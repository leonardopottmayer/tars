using FluentAssertions;
using Moq;
using Pottmayer.Tars.Core.Localization.Abstractions;
using Pottmayer.Tars.Core.Primitives.Outcomes;
using Pottmayer.Tars.Web.Http;

namespace Pottmayer.Tars.Web.Tests.Unit;

public class DefaultHttpErrorMapperTests
{
    private static DefaultHttpErrorMapper Create(string providerMessage = "localized")
    {
        var messages = new Mock<IMessageProvider>();
        messages.Setup(m => m.Get(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<object[]>()))
            .Returns(providerMessage);
        return new DefaultHttpErrorMapper(messages.Object);
    }

    [Theory]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Validation, 422)]
    [InlineData(ErrorType.Business, 400)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.Unexpected, 500)]
    public void MapToStatusCode_maps_each_error_type(ErrorType type, int expected)
    {
        Create().MapToStatusCode(type).Should().Be(expected);
    }

    [Fact]
    public void Map_uses_error_message_when_present()
    {
        var response = Create().Map(Error.NotFound("USER_NOT_FOUND", "User does not exist"));

        response.Success.Should().BeFalse();
        response.ErrorCode.Should().Be("USER_NOT_FOUND");
        response.ErrorMessage.Should().Be("User does not exist");
    }

    [Fact]
    public void Map_falls_back_to_message_provider_when_message_blank()
    {
        var response = Create("fallback-msg").Map(new Error("CODE", "", ErrorType.Business));

        response.ErrorMessage.Should().Be("fallback-msg");
    }

    [Fact]
    public void Map_validation_error_with_metadata_produces_field_errors()
    {
        var metadata = new Dictionary<string, object?> { ["email"] = "invalid format" };
        var response = Create().Map(Error.Validation("VALIDATION", "bad", metadata));

        response.FieldErrors.Should().NotBeNull();
        response.FieldErrors!.Should().ContainSingle();
        response.FieldErrors![0].Field.Should().Be("email");
        response.FieldErrors![0].Message.Should().Be("invalid format");
    }

    [Fact]
    public void Map_non_validation_error_has_no_field_errors()
    {
        var response = Create().Map(Error.Business("B", "x"));

        response.FieldErrors.Should().BeNull();
    }

    [Fact]
    public void Map_exception_returns_internal_server_error()
    {
        var response = Create().Map(new InvalidOperationException("boom"));

        response.Success.Should().BeFalse();
        response.ErrorCode.Should().Be("INTERNAL_SERVER_ERROR");
    }
}
