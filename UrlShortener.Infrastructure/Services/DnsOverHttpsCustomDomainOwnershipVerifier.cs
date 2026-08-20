using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using UrlShortener.Application.Interfaces;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Infrastructure.Services;

public sealed class DnsOverHttpsCustomDomainOwnershipVerifier : ICustomDomainOwnershipVerifier
{
    private readonly HttpClient _httpClient;
    private readonly CustomDomainOptions _options;

    public DnsOverHttpsCustomDomainOwnershipVerifier(
        HttpClient httpClient,
        IOptions<CustomDomainOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<CustomDomainVerificationEvidence> VerifyTxtRecordAsync(
        string recordName,
        string expectedValue,
        CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.LookupTimeoutSeconds));

        try
        {
            var separator = _options.DnsOverHttpsEndpoint.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            var requestUri = $"{_options.DnsOverHttpsEndpoint}{separator}name={Uri.EscapeDataString(recordName)}&type=TXT";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/dns-json");
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                return LookupUnavailable();
            }

            const int maximumResponseBytes = 64 * 1024;
            if (response.Content.Headers.ContentLength > maximumResponseBytes)
            {
                return LookupUnavailable();
            }

            await response.Content.LoadIntoBufferAsync(maximumResponseBytes, timeout.Token);
            var payload = await response.Content.ReadFromJsonAsync<DnsJsonResponse>(cancellationToken: timeout.Token);
            if (payload == null || payload.Status is not (0 or 3))
            {
                return LookupUnavailable();
            }

            var values = payload.Answer?
                .Where(answer => answer.Type == 16 && !string.IsNullOrWhiteSpace(answer.Data))
                .Select(answer => UnquoteTxtValue(answer.Data!))
                .ToList() ?? [];

            if (values.Any(value => value.Equals(expectedValue, StringComparison.Ordinal)))
            {
                return CustomDomainVerificationEvidence.Verified;
            }

            return values.Count == 0
                ? new CustomDomainVerificationEvidence(
                    CustomDomainVerificationEvidenceStatus.Failed,
                    "DNS_TXT_RECORD_NOT_FOUND",
                    "Publish the expected TXT record and try again after DNS propagation.")
                : new CustomDomainVerificationEvidence(
                    CustomDomainVerificationEvidenceStatus.Failed,
                    "DNS_TXT_RECORD_MISMATCH",
                    "The TXT record does not contain the current verification value. Copy the value exactly and try again.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return LookupUnavailable();
        }
        catch (HttpRequestException)
        {
            return LookupUnavailable();
        }
        catch (System.Text.Json.JsonException)
        {
            return LookupUnavailable();
        }
    }

    private static string UnquoteTxtValue(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"'
            ? trimmed[1..^1].Replace("\\\"", "\"").Replace("\\\\", "\\")
            : trimmed;
    }

    private static CustomDomainVerificationEvidence LookupUnavailable() => new(
        CustomDomainVerificationEvidenceStatus.Failed,
        "DNS_LOOKUP_UNAVAILABLE",
        "DNS verification is temporarily unavailable. Try again later.");

    private sealed class DnsJsonResponse
    {
        [JsonPropertyName("Status")]
        public int Status { get; init; }

        [JsonPropertyName("Answer")]
        public IReadOnlyList<DnsJsonAnswer>? Answer { get; init; }
    }

    private sealed class DnsJsonAnswer
    {
        [JsonPropertyName("type")]
        public int Type { get; init; }

        [JsonPropertyName("data")]
        public string? Data { get; init; }
    }
}
