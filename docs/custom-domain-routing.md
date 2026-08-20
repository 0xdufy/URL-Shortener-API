# Custom-Domain Routing

## Identity and uniqueness decision

Short codes remain globally unique and case-sensitive. The existing unique SQL Server index
`IX_ShortUrls_ShortCode` is unchanged: a code claimed on the platform host cannot be reused on a
custom host, and two custom hosts cannot share a code. TASK-041 deliberately does not migrate to
per-host code reuse.

Routing still uses the pair `(normalized effective host, short code)`. This prevents a branded
link from resolving on the platform host and prevents a platform link from resolving on a custom
host. `ShortUrls.CustomDomainId` is nullable: `null` means the configured platform host, while a
value assigns the link to one custom-domain claim. A composite foreign key from
`(CustomDomainId, OwnerId)` to `(CustomDomains.Id, OwnerId)` prevents cross-owner assignments at
the database boundary.

## Create, update, and response contract

`POST /api/v1/short-urls` and the full-replacement
`PUT /api/v1/short-urls/{shortCode}` accept `customDomainId`. Omit it or send `null` for the
platform host; send a domain ID for a branded link. Selection succeeds only when the current owner
owns that exact claim and its state is currently `verified`. A missing, cross-owner, pending,
failed, disabled, or empty ID returns `409 CUSTOM_DOMAIN_UNAVAILABLE` without revealing which
eligibility check failed.

The repository rechecks owner and verified state before SQL creation. The response and list item
include `customDomainId` and `customDomainHost`; both are `null` for platform links. `shortUrl` is
constructed from trusted canonical data:

- platform link: `PublicUrls:BaseUrl` plus `/r/{code}`;
- branded link: `PublicUrls:CustomDomainScheme`, the normalized persisted domain host, and
  `/r/{code}`.

Request `Host`, `X-Forwarded-Host`, and `X-Forwarded-Proto` never construct or persist the public
URL. Outside Development, startup requires `PublicUrls:CustomDomainScheme=https`.

## Redirect and cache policy

`GET /r/{shortCode}` passes `Request.Host.Host` as routing input. The configured
`PublicUrls:BaseUrl` host is the only platform-host identity. Any other input must normalize as a
valid custom DNS host and match the assigned domain while that domain remains verified. Unknown
hosts, unverified/disabled domains, and wrong host/code combinations return the concealed
`404 NOT_FOUND`. Link expiry still returns `410 EXPIRED` only after host/domain routing succeeds.

Redis cache identity is version 2:

```text
redirect:v2:<normalized-routing-host>:<case-sensitive-short-code>
```

The version 2 payload also contains `routingHost`. Cache hits remain guarded by persistence; the
guard checks exact destination/expiry, active/deleted/expiry state, link-to-domain assignment,
normalized host, and current verified domain state. Stale version 1 keys are never read.

Disabling a domain or requesting a new verification token retains every link assignment but makes
the domain immediately ineligible. The operation removes all known cache entries for that domain;
the persisted guard also fails closed if invalidation races or Redis is unavailable. Assigned
links return `404` until the same claim is verified again. Re-verification restores routing for
links that are independently active, non-deleted, and unexpired. A link may instead be updated
with `customDomainId: null` to move it to the platform host.

## DNS, TLS, and reverse proxy requirements

Ownership verification does not configure traffic or certificates. A production operator must:

1. Keep the verification TXT record required by the ownership workflow and publish an A/AAAA,
   CNAME, or provider-specific ALIAS/ANAME record that sends the branded host to the public edge.
2. Provision and renew a certificate covering the exact branded hostname at the load balancer,
   ingress, CDN, or other TLS terminator. The API does not issue certificates and does not claim
   that successful TXT verification makes HTTPS ready.
3. Route `/r/{shortCode}` for the platform and approved custom hosts to the API, preserving the
   original host in the actual HTTP `Host` field. This application intentionally ignores
   `X-Forwarded-Host`; do not depend on it for routing.
4. Remove or overwrite client-supplied forwarding headers at the public edge. If ASP.NET Core
   `AllowedHosts` or equivalent edge allowlists are enabled, configure a safe dynamic/custom-host
   policy so verified hosts are admitted without turning forwarded-host data into authority.
5. Confirm DNS propagation, certificate readiness, SNI behavior, and proxy routing before telling
   an owner that the branded URL is publicly reachable.

The application may receive a caller-supplied `Host` header on direct connections. That header can
only select an already verified persisted route; it cannot register a domain, change ownership, or
alter generated public URLs.

## Controlled host-routing verification

For local verification, use the TASK-040 DNS stub workflow to create one verified host such as
`go.example.test`, then map the platform and custom hosts to the local proxy/API with a hosts-file
entry or `curl --resolve`. Configure the API's `PublicUrls:BaseUrl` to the platform origin and use
`PublicUrls:CustomDomainScheme=http` only in Development if local TLS is unavailable.

Verify these cases:

1. A platform link resolves only with the platform `Host`.
2. A link assigned to the verified domain resolves only with that custom `Host`.
3. An unregistered/unverified host and both wrong host/code combinations return `404`.
4. Cache the branded redirect, disable the domain, and confirm the cache key is removed and the
   next branded request returns `404`.
5. Re-request verification and confirm the link stays unavailable; complete verification again
   and confirm the assigned link resumes when its own lifecycle state permits.

No certificate behavior is implied by a successful HTTP-only local check.
