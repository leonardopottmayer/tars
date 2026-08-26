using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.DataProtection;
using Pottmayer.Tars.Security.DataProtection.Abstractions;
using Pottmayer.Tars.Security.DataProtection.Options;

namespace Pottmayer.Tars.Security.Tests.Unit;

public sealed class AesGcmSecretProtectorTests
{
    private static string Key() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static AesGcmSecretProtector Protector(DataProtectionOptions options) =>
        new(Microsoft.Extensions.Options.Options.Create(options));

    private static DataProtectionOptions SingleKey(string version = "v1")
    {
        var options = new DataProtectionOptions { ActiveKeyVersion = version };
        options.Keys[version] = Key();
        return options;
    }

    [Fact]
    public void Protect_then_Unprotect_roundtrips()
    {
        var protector = Protector(SingleKey());

        var plaintext = "1//0abcRefreshToken-çãö-🔐";
        var protectedValue = protector.Protect(plaintext);

        protectedValue.Should().NotBe(plaintext);
        protector.Unprotect(protectedValue).Should().Be(plaintext);
    }

    [Fact]
    public void Protect_produces_distinct_ciphertext_each_call()
    {
        var protector = Protector(SingleKey());

        var first = protector.Protect("same");
        var second = protector.Protect("same");

        first.Should().NotBe(second, "a fresh random nonce is used per call");
        protector.Unprotect(first).Should().Be("same");
        protector.Unprotect(second).Should().Be("same");
    }

    [Fact]
    public void Protect_stamps_the_active_key_version()
    {
        var protector = Protector(SingleKey("v2"));

        protector.Protect("x").Should().StartWith("v2.");
    }

    [Fact]
    public void Unprotect_decrypts_a_value_written_under_a_now_inactive_key()
    {
        var shared = Key();

        var v1Only = new DataProtectionOptions { ActiveKeyVersion = "v1" };
        v1Only.Keys["v1"] = shared;
        var oldValue = Protector(v1Only).Protect("legacy");

        // Key rotated: v2 is active now, but v1 is still available to decrypt.
        var rotated = new DataProtectionOptions { ActiveKeyVersion = "v2" };
        rotated.Keys["v1"] = shared;
        rotated.Keys["v2"] = Key();

        Protector(rotated).Unprotect(oldValue).Should().Be("legacy");
    }

    [Fact]
    public void Unprotect_rejects_a_value_whose_key_version_is_not_configured()
    {
        var value = Protector(SingleKey("v1")).Protect("x");

        var other = new DataProtectionOptions { ActiveKeyVersion = "v9" };
        other.Keys["v9"] = Key();

        var act = () => Protector(other).Unprotect(value);

        act.Should().Throw<SecretProtectionException>().WithMessage("*key version*");
    }

    [Fact]
    public void Unprotect_rejects_a_tampered_payload()
    {
        var options = SingleKey();
        var protector = Protector(options);
        var value = protector.Protect("x");

        // Flip a character in the base64 payload (after the "v1." prefix).
        var chars = value.ToCharArray();
        var last = chars.Length - 1;
        chars[last] = chars[last] == 'A' ? 'B' : 'A';
        var tampered = new string(chars);

        var act = () => protector.Unprotect(tampered);

        act.Should().Throw<SecretProtectionException>();
    }

    [Theory]
    [InlineData("no-separator")]
    [InlineData(".missing-version")]
    [InlineData("v1.not-base64!!")]
    public void Unprotect_rejects_a_malformed_value(string malformed)
    {
        var act = () => Protector(SingleKey()).Unprotect(malformed);

        act.Should().Throw<SecretProtectionException>();
    }

    [Fact]
    public void Constructor_rejects_a_key_of_the_wrong_length()
    {
        var options = new DataProtectionOptions { ActiveKeyVersion = "v1" };
        options.Keys["v1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)); // 128-bit

        var act = () => Protector(options);

        act.Should().Throw<InvalidOperationException>().WithMessage("*32 bytes*");
    }

    [Fact]
    public void Constructor_rejects_an_active_version_absent_from_keys()
    {
        var options = new DataProtectionOptions { ActiveKeyVersion = "missing" };
        options.Keys["v1"] = Key();

        var act = () => Protector(options);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ActiveKeyVersion*");
    }

    [Fact]
    public void Constructor_rejects_an_empty_key_set()
    {
        var act = () => Protector(new DataProtectionOptions { ActiveKeyVersion = "v1" });

        act.Should().Throw<InvalidOperationException>().WithMessage("*at least one key*");
    }
}
