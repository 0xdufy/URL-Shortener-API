# Client IP and Reverse-Proxy Trust

## Trust Model

The API has two supported client-IP topologies:

1. **Direct development:** the client connects to Kestrel and `ProxyTrust:Enabled` is `false`.
   `HttpContext.Connection.RemoteIpAddress` is the socket peer, and every forwarding header is
   ignored.
2. **Proxied deployment:** one or more explicitly configured reverse proxies or load balancers sit
   between the client and Kestrel. `ProxyTrust:Enabled` is `true`, every proxy hop is represented by
   `KnownProxies` or `KnownNetworks`, and `ForwardLimit` is no greater than the intended proxy-chain
   depth.

Forwarded-header processing runs before request logging, authentication, rate limiting, and
controllers. This task enables only `X-Forwarded-For`; `X-Forwarded-Host` and
`X-Forwarded-Proto` do not influence the request. Canonical public short URLs continue to come
from `PublicUrls:BaseUrl` rather than any request header.

TASK-041 custom-domain redirects use the actual `Host` field received by Kestrel, not
`X-Forwarded-Host`. A reverse proxy serving branded links must preserve the original public
hostname in `Host` while routing `/r/{shortCode}` to the API. Branded `shortUrl` values are
generated from the verified persisted hostname and `PublicUrls:CustomDomainScheme`; platform URLs
continue to use `PublicUrls:BaseUrl`. Neither path trusts forwarding headers for URL generation.

ASP.NET Core evaluates `X-Forwarded-For` from right to left, beginning with the direct socket peer.
It advances only while the current proxy address is trusted and stops after `ForwardLimit`
entries. An untrusted direct peer therefore cannot replace the effective IP by sending its own
header. The edge proxy must remove any incoming `X-Forwarded-For` value supplied by the public
client before setting or appending the verified connection address according to the selected
proxy product's guidance.

## Configuration

Forwarded IPs are disabled safely in committed configuration:

```json
{
  "ProxyTrust": {
    "Enabled": false,
    "ForwardLimit": 1,
    "KnownProxies": [],
    "KnownNetworks": []
  }
}
```

For one reverse proxy whose connection to Kestrel originates from `10.20.0.10`:

```powershell
$env:ProxyTrust__Enabled = "true"
$env:ProxyTrust__ForwardLimit = "1"
$env:ProxyTrust__KnownProxies__0 = "10.20.0.10"
```

For a tightly controlled proxy subnet, use CIDR notation instead:

```powershell
$env:ProxyTrust__Enabled = "true"
$env:ProxyTrust__ForwardLimit = "1"
$env:ProxyTrust__KnownNetworks__0 = "10.20.0.0/28"
```

Array indexes must be contiguous (`__0`, `__1`, and so on). Prefer individual proxy addresses
when stable. A network is appropriate for an orchestrator or load-balancer subnet only when that
subnet cannot contain untrusted workloads. Do not copy example addresses into a deployment; use
the source addresses actually observed on the API-side connection.

Startup validation enforces a hop limit from 1 through 10, requires at least one trust entry when
processing is enabled, rejects invalid/unspecified proxy addresses, and rejects `/0` or malformed
networks. ASP.NET Core's implicit loopback defaults are cleared, so even a loopback proxy must be
listed deliberately. Startup logs report whether processing is enabled plus counts and the hop
limit without logging the configured addresses.

## Changing the Proxy Topology

Before adding or replacing a proxy/load balancer:

1. Determine the source IP or smallest dedicated CIDR seen by Kestrel for every hop.
2. Configure each hop as a known proxy/network and set `ForwardLimit` to the intended chain depth.
3. Configure the public edge to sanitize client-supplied `X-Forwarded-For` and pass one consistent
   header through the internal chain.
4. Restart the API so startup validation applies the complete configuration atomically.
5. Repeat direct, trusted-proxy, and untrusted-header checks. Confirm rate-limit partitions and
   analytics records see the same effective client identity.

Never set a catch-all network or clear the framework's hop limit. If topology discovery is
incomplete, leave processing disabled; rate limiting will conservatively group traffic by the
direct proxy address instead of trusting attacker-controlled data.

## IP Normalization

Rate limiting, URL-creation metadata, and redirect analytics all consume the effective
`RemoteIpAddress` after the trust middleware. IPv4-mapped IPv6 values such as
`::ffff:192.0.2.10` are converted to `192.0.2.10`; other IPv4 and IPv6 values use .NET's canonical
text representation. A missing address becomes the shared fail-safe identity `unknown`.

This prevents mapped and native IPv4 forms from producing separate limiter keys. Rate-limit keys
continue to contain a SHA-256 partition digest rather than the raw normalized address. Raw
analytics retention and privacy policy remain owned by the later privacy/data-lifecycle tasks.

## Manual Verification

Use the anonymous bootstrap endpoint with a temporary low limit and an isolated Redis key prefix:

- **Direct:** keep `Enabled=false`; send one request with a fabricated `X-Forwarded-For` and one
  without it. They must share the direct-peer partition.
- **Trusted proxy:** enable processing, explicitly trust the socket peer, and send requests with
  two different forwarded client addresses. They must use different partitions.
- **Untrusted path:** enable processing but trust a different address/network; send requests with
  two different forwarded addresses from the actual peer. They must still share the actual
  direct-peer partition.

Also start once with `Enabled=true` and empty trust lists. Startup must fail with the
`ProxyTrust` validation message. Restore normal limits and remove the isolated Redis keys after
verification.
