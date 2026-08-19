namespace UrlShortener.Application.ApiKeys;

public sealed class GeneratedApiKeyCredential
{
    public GeneratedApiKeyCredential(string keyPrefix, byte[] secretHash, string plaintextKey)
    {
        KeyPrefix = keyPrefix;
        SecretHash = secretHash;
        PlaintextKey = plaintextKey;
    }

    public string KeyPrefix { get; }
    public byte[] SecretHash { get; }
    public string PlaintextKey { get; }

    public override string ToString() => "API-key credential [REDACTED]";
}
