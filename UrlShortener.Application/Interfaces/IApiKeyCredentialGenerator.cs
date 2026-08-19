using UrlShortener.Application.ApiKeys;

namespace UrlShortener.Application.Interfaces;

public interface IApiKeyCredentialGenerator
{
    GeneratedApiKeyCredential Generate();
}
