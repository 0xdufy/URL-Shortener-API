# URL, Alias, and Redirect-Metadata Security

This document defines the canonical input policy for destination URLs, short codes, reserved
routes, and redirect analytics metadata. The API applies these rules before persistence, cache
population, or queue publication. The Angular form mirrors the user-facing subset; the API remains
authoritative.

## Destination URLs

Destinations are limited to 2,048 characters both before and after normalization. They must parse
as absolute HTTP or HTTPS URIs through .NET `Uri`; scheme allow-listing is an additional check, not
a regular-expression parser. Empty hosts, unknown/basic host forms, control characters,
surrounding whitespace, and embedded user information (`user:password@host`) are rejected.
Consequently `javascript:`, `data:`, `file:`, script-like, relative, and malformed inputs are not
accepted. Paths, queries, fragments, explicit non-default ports, DNS hosts, IPv4, and IPv6 remain
supported when the complete URI is valid and within the bound.

Before hashing an idempotent create request or storing an accepted destination, the API uses
`UriBuilder`/`Uri.AbsoluteUri` to canonicalize it. Scheme and DNS host casing, default ports,
escaping, and IDN host representation therefore have one stable form. A normalized value that
expands past 2,048 characters is rejected rather than reaching the database.

Unicode DNS names are converted through `Uri.IdnHost` and stored and returned in lower-case ASCII
Punycode form. For example, `https://例え.テスト/a` is returned as
`https://xn--r8jz45g.xn--zckzah/a`. The owner UI therefore displays the canonical ASCII host rather
than presenting a visually confusable Unicode spelling as authoritative. No Unicode-display
reconstruction is performed. Operators and users must still treat unfamiliar Punycode hosts as
untrusted destinations.

The service does not resolve, connect to, fetch, preview, scan, or otherwise dereference a
destination. Adding server-side fetching requires a separate safe-fetch/SSRF design and ADR that
covers address resolution, redirect handling, network egress, time/size limits, and DNS rebinding.

## Short codes and reserved routes

Generated codes and custom aliases contain 4–20 ASCII letters, digits, hyphens, or underscores.
Generated values remain eight characters. Short-code lookup rejects values outside this shape
before cache or database access. Alias case is preserved and comparisons remain ordinal/case-
sensitive, matching the SQL Server `Latin1_General_CS_AS` unique column and the in-memory
repository. Thus `Launch` and `launch` are distinct claims.

Route reservation is deliberately case-insensitive. A custom alias is rejected when it equals a
reserved root or starts with that root followed by `-` or `_`. This prevents case variants and
namespaced aliases from being mistaken for present or future platform endpoints. The roots are:

`api`, `auth`, `health`, `healthz`, `live`, `livez`, `ready`, `readyz`, `metrics`, `swagger`,
`openapi`, `docs`, `app`, `r`, `dashboard`, `links`, `analytics`, `api-keys`, `domains`, `account`,
`sign-in`, and `register`.

For example, `Docs`, `API_v1`, and `app-settings` are reserved, while `application` and `rapid` are
not. The separator rule avoids reserving every ordinary word that happens to begin with a short
route root. Generated candidates are checked against the same reservation policy and retried.

## Request headers and analytics metadata

Kestrel rejects request headers above the configured 16,384-byte aggregate limit or 64-header
count before controller processing. For a successful redirect, the analytics producer applies
stricter field policy before queue publication:

- User agent is trimmed and truncated to 256 characters. A blank value becomes absent.
- A raw `Referer` above 2,048 characters or containing control characters is classified as
  `unknown` without parsing or retaining it.
- A valid absolute HTTP(S) referrer is reduced to its lower-case ASCII IDN host, capped at 253
  characters. Path, query, fragment, credentials, and port are never queued or persisted.
- Missing/blank referrer is `direct`; malformed, unsupported, oversized, or unsafe referrer is
  `unknown`.

The worker independently rejects queue fields above their contract bounds. SQL Server also caps
stored user agent at 256, normalized referrer host at 512 (the active producer/consumer contract
uses at most 253), and pseudonymous visitor identifiers at 64 characters.

## Error behavior

Rejected create/update destinations and aliases return the existing `400 VALIDATION_ERROR`
envelope with `OriginalUrl` or `CustomAlias` details. Angular maps those names to the matching
field, mirrors URL/alias constraints locally, and still renders backend detail when the API rejects
a value. Alias uniqueness remains a separate `409 ALIAS_CONFLICT` condition.

