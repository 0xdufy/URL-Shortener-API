# Custom-Domain Ownership Verification

Custom domains are owner-scoped management resources. Registration records a claim but never
establishes ownership by itself. The only supported verification method is an exact DNS TXT value
observed through the configured DNS-over-HTTPS resolver; request headers, account data, A/AAAA
records, and CNAME records are not ownership evidence.

## Host and claim policy

Input is a hostname, not a URL. Normalization removes one terminal DNS root dot, converts Unicode
labels to IDNA ASCII, lowercases the result, and validates DNS label and total-length limits. Ports,
schemes, paths, queries, fragments, wildcards, IP literals, empty labels, and single-label names are
rejected. The normalized ASCII host is stored in a binary-collated `varchar(253)` column with the
global unique index `UX_CustomDomains_NormalizedHost`.

Global uniqueness intentionally prevents two accounts from simultaneously claiming the same host.
Claims are retained when disabled, so a hostname cannot be taken over through a normal management
operation. `OwnerId` and `NormalizedHost` have private setters and no update endpoint; ownership and
claim identity are immutable after creation.

The canonical `PublicUrls:BaseUrl` host and every `CustomDomains:ReservedHosts` entry are protected.
The policy rejects the exact host, its children, and its parent namespace. This keeps both a platform
host and its surrounding namespace out of user-controlled routing. Reserved entries must already be
normalized ASCII DNS names.

## Verification states and evidence

Registration creates a 256-bit random base64url token and returns this instruction:

```text
Type:  TXT
Name:  _urlshortener-verification.<normalized-host>
Value: urlshortener-verification=<43-character-token>
```

The token is scoped to the custom-domain row and is compared exactly and case-sensitively. Requesting
verification rotates it, invalidates any previous TXT value, clears failure/verification state, and
returns the domain to `pending`.

| State | Meaning | `canServeBrandedLinks` |
|---|---|---|
| `pending` | A current token exists but has not been checked successfully. | `false` |
| `failed` | The last external lookup did not prove ownership. | `false` |
| `verified` | The configured resolver returned the exact current TXT value. | `true` |
| `disabled` | The owner disabled the retained claim. Request verification to re-enable it. | `false` |

`verifiedAtUtc` is retained as audit metadata after disable. A disabled domain cannot be checked;
the owner must request a new token and prove control again. Branded-link creation and routing
consume `CanServeBrandedLinks`: pending, failed, and disabled claims cannot be selected or served,
and leaving `verified` invalidates assigned redirect caches.

Failed checks return the updated resource with one stable failure code and a safe action:

- `DNS_TXT_RECORD_NOT_FOUND`: publish the returned record and allow for DNS propagation.
- `DNS_TXT_RECORD_MISMATCH`: replace stale/incorrect content with the current exact value.
- `DNS_LOOKUP_UNAVAILABLE`: retry later; upstream status, response bodies, addresses, and resolver
  exception details are not exposed.

## Management API

All routes require the owner's Bearer session, use owner-filtered repository queries, and return
`Cache-Control: no-store` because the verification value appears in the representation.

| Method and route | Behavior |
|---|---|
| `POST /api/v1/custom-domains` | Register `{ "host": "links.example.com" }`; returns `201`. |
| `GET /api/v1/custom-domains` | List the current owner's claims and safe verification metadata. |
| `POST /api/v1/custom-domains/{id}/verification/request` | Rotate the token and enter `pending`. |
| `POST /api/v1/custom-domains/{id}/verification/check` | Query external DNS and enter `verified` or `failed`. |
| `POST /api/v1/custom-domains/{id}/disable` | Enter `disabled` without releasing the global claim. |

A missing or cross-owner ID is the same `404 NOT_FOUND`. A duplicate normalized claim returns
`409 CUSTOM_DOMAIN_ALREADY_CLAIMED`; a protected host returns `400 CUSTOM_DOMAIN_RESERVED`; and a
concurrent token/status change returns `409 CUSTOM_DOMAIN_STATE_CONFLICT`.

## Resolver configuration and controlled verification

Production must use an HTTPS DNS-over-HTTPS JSON endpoint. The default is Cloudflare's documented
`/dns-query` endpoint and requests use `Accept: application/dns-json`. The response body is bounded
by the configured HTTP client buffer and lookup time is bounded by
`CustomDomains:LookupTimeoutSeconds` (1-30 seconds). Only Development permits a loopback HTTP
endpoint, specifically to support a deterministic local stub; non-loopback plaintext endpoints are
rejected at startup.

To exercise the workflow without controlling public DNS:

1. Run a loopback HTTP stub at a path such as `http://127.0.0.1:8053/resolve` and start the API with
   `CustomDomains__DnsOverHttpsEndpoint` set to that URL.
2. Register `verify.example.test` and confirm the response is `pending` and
   `canServeBrandedLinks` is `false`.
3. Have the stub return `{"Status":0}` for the queried name. Check verification and confirm
   `failed` with `DNS_TXT_RECORD_NOT_FOUND` and no resolver diagnostics.
4. Copy the current response's verification value into a TXT answer:
   `{"Status":0,"Answer":[{"type":16,"data":"\"urlshortener-verification=<token>\""}]}`.
5. Check again and confirm `verified` with `canServeBrandedLinks: true`; then disable it and confirm
   `canServeBrandedLinks: false`.
6. Request verification once more and confirm a different 43-character token, `pending` state, and
   that the old TXT response now produces `DNS_TXT_RECORD_MISMATCH`.

The loopback exception is for controlled Development verification only. Deployment evidence must
come from externally resolvable DNS and TLS/custom-host routing remains separate TASK-041 work.
