using System.Net;

namespace UrlShortener.Api.Networking;

public static class ClientIpAddress
{
    public const string Unknown = "unknown";

    public static string Normalize(IPAddress? address)
    {
        if (address is null)
        {
            return Unknown;
        }

        return address.IsIPv4MappedToIPv6
            ? address.MapToIPv4().ToString()
            : address.ToString();
    }
}
