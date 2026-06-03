using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.Web.Http;
using Pottmayer.Tars.Web.Http.Abstractions;
using Pottmayer.Tars.Web.Http.AspNetCore.Extensions;

namespace Pottmayer.Tars.Web.Tests.Unit;

public class ResultHttpExtensionsTests
{
    private static readonly FakeHttpErrorMapper Mapper = new();

    [Fact]
    public void Success_generic_returns_ok_envelope_with_data()
    {
        var result = Result<string>.Success("payload").ToActionResult(Mapper);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<HttpResponse<string>>().Subject;
        body.Success.Should().BeTrue();
        body.Data.Should().Be("payload");
    }

    [Fact]
    public void Success_non_generic_returns_ok_envelope_with_null_data()
    {
        var result = Result.Success().ToActionResult(Mapper);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<HttpResponse<object?>>().Subject;
        body.Success.Should().BeTrue();
        body.Data.Should().BeNull();
    }

    [Fact]
    public void Success_injects_trace_id_from_current_activity()
    {
        using var activity = StartActivity();

        var ok = (OkObjectResult)Result<string>.Success("x").ToActionResult(Mapper);
        var body = (HttpResponse<string>)ok.Value!;

        body.TraceId.Should().Be(activity.TraceId.ToHexString());
    }

    // The headline behavior change: 401/403 used to return a bodiless
    // UnauthorizedResult/ForbidResult; now they carry the error envelope.
    [Theory]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Validation, 422)]
    [InlineData(ErrorType.Business, 400)]
    [InlineData(ErrorType.Conflict, 409)]
    public void Failure_returns_envelope_object_result_with_mapped_status(ErrorType type, int expectedStatus)
    {
        var error = new Error("Some.Code", "Some message", type);

        var result = Result<string>.Failure(error).ToActionResult(Mapper);

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(expectedStatus);

        var body = obj.Value.Should().BeOfType<HttpErrorResponse>().Subject;
        body.Success.Should().BeFalse();
        body.ErrorCode.Should().Be("Some.Code");
        body.ErrorMessage.Should().Be("Some message");
    }

    [Fact]
    public void Failure_validation_propagates_field_errors()
    {
        var metadata = new Dictionary<string, object?> { ["email"] = "invalid" };
        var error = Error.Validation("Users.InvalidEmail", "bad email", metadata);

        var obj = (ObjectResult)Result<string>.Failure(error).ToActionResult(Mapper);
        var body = (HttpErrorResponse)obj.Value!;

        body.FieldErrors.Should().ContainSingle();
        body.FieldErrors![0].Field.Should().Be("email");
        body.FieldErrors![0].Message.Should().Be("invalid");
    }

    [Fact]
    public void Failure_injects_trace_id_into_envelope()
    {
        using var activity = StartActivity();

        var obj = (ObjectResult)Result<string>.Failure(Error.Unauthorized("X", "y")).ToActionResult(Mapper);
        var body = (HttpErrorResponse)obj.Value!;

        body.TraceId.Should().Be(activity.TraceId.ToHexString());
    }

    private static Activity StartActivity()
    {
        var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        return activity;
    }

    private sealed class FakeHttpErrorMapper : IHttpErrorMapper
    {
        public int MapToStatusCode(ErrorType errorType) => errorType switch
        {
            ErrorType.NotFound     => 404,
            ErrorType.Validation   => 422,
            ErrorType.Business     => 400,
            ErrorType.Conflict     => 409,
            ErrorType.Unauthorized => 401,
            ErrorType.Forbidden    => 403,
            _                      => 500
        };

        public IHttpErrorResponse Map(Error error) => new HttpErrorResponse
        {
            Success      = false,
            ErrorCode    = error.Code,
            ErrorMessage = error.Message,
            FieldErrors  = error.Type == ErrorType.Validation && error.Metadata is not null
                ? error.Metadata
                    .Select(kv => (IHttpFieldError)new HttpFieldError(kv.Key, kv.Value?.ToString() ?? string.Empty))
                    .ToList()
                : null
        };

        public IHttpErrorResponse Map(Exception exception) => new HttpErrorResponse
        {
            Success   = false,
            ErrorCode = "INTERNAL_SERVER_ERROR"
        };
    }
}
