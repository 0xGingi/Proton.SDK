﻿using System.Text.Json.Serialization;

namespace Proton.Sdk.Authentication;

internal sealed class AuthenticationResponse : ApiResponse
{
    [JsonPropertyName("UID")]
    public required string SessionId { get; init; }

    [JsonPropertyName("UserID")]
    public required string UserId { get; init; }

    [JsonPropertyName("EventID")]
    public string? EventId { get; init; }

    public required ReadOnlyMemory<byte> ServerProof { get; init; }

    public required string TokenType { get; init; }

    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }

    public required IReadOnlyList<string> Scopes { get; init; }

    public required PasswordMode PasswordMode { get; init; }

    [JsonPropertyName("2FA")]
    public SecondFactorParameters? SecondFactorParameters { get; init; }
}
