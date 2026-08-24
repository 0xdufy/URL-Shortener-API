# TASK-047 — Abuse Controls and Moderation Hooks

**Status:** Completed
**Phase:** 12 — Security, Privacy & Abuse Hardening

## Goal

Add bounded, auditable controls for obvious abuse cases without claiming the service can perfectly detect phishing or malicious destinations.

## Dependencies

- TASK-046 completed.

## Scope

- Define blocked destination domains/hosts and reserved patterns through operator-controlled configuration or persistence.
- Add an optional authenticated/public report-link workflow if it can be moderated meaningfully.
- Add link moderation state capable of disabling redirect without deleting owner data.
- Define owner/account suspension interaction with existing links if account status supports it.
- Log moderation actions without logging sensitive credentials.
- Keep external reputation/scanning integrations out of scope unless separately approved with SSRF, privacy, cost, and reliability considerations.

## Acceptance Criteria

- [x] Blocked destination policy has one normalized matching strategy and cannot be bypassed trivially through hostname case/trailing-dot normalization.
- [x] Moderation can disable a link's redirect and invalidates distributed cache state.
- [x] Moderation status/reason exposure to owners follows a documented policy and does not leak internal security details unnecessarily.
- [x] Report endpoint, if implemented, is rate limited and cannot create unbounded spam records. (Not implemented; see decision below.)
- [x] Operator/moderation actions are auditable.
- [x] Existing ownership rules remain intact; moderation is not implemented by transferring ownership.
- [x] The documentation explicitly distinguishes preventive controls from guaranteed malicious-link detection.
- [x] Backend/Angular behavior for a moderated link is clear and manually verified.
- [x] No automated test files are added.

## Implemented policy

`AbuseControls:BlockedDestinationHosts` is the operator-controlled preventive blocklist. Each configured host and each submitted destination host is converted to ASCII IDN form, lowercased, and stripped of a terminal DNS dot. A configured entry blocks that exact host and all of its subdomains. Creation and destination updates use the same policy. For example, `Example.COM.` blocks `example.com`, `EXAMPLE.COM.`, and `a.example.com`, but not `notexample.com`.

This is a deterministic preventive control, not a reputation scanner and not a guarantee that permitted destinations are safe. DNS resolution, redirects at the destination, external reputation services, content fetching, and phishing classification remain out of scope. Adding any network scanner requires separate SSRF, privacy, reliability, and cost review.

## Moderation and owner exposure

SQL-backed operators receive moderation access through the ASP.NET Identity `Moderator` role. Role claims are copied into newly issued short-lived access tokens. `PUT /api/v1/moderation/short-urls/{shortUrlId}` accepts a blocked/cleared decision, an allowlisted owner-visible reason code, and a required internal reason. API keys cannot call this endpoint.

Blocking sets a moderation state on the existing link; it never changes `OwnerId`, deletes the link, or overwrites the owner's active setting. Redirect resolution checks the moderation state on persistence reads and cache revalidation, and the moderation operation removes the route's distributed cache key after the database commit. Consequently, a stale cache entry still cannot bypass a block if cache removal fails.

Owners see only `moderationStatus` and one coarse reason code: `policy_violation`, `unsafe_destination`, or `abuse`. Internal moderator notes, actor identity, and action history are never returned by owner APIs. The Angular list labels blocked links **Restricted**. Details explain the coarse policy category, suppress the open-link and QR actions, and make clear that changing the owner's active state does not lift moderation. Editing, deletion, restoration, analytics ownership, and all other ownership checks remain owner-scoped.

Every decision appends a `ShortUrlModerationActions` row with the link ID, moderator user ID, action, coarse reason code, internal reason, and UTC timestamp. Authentication headers, tokens, cookies, API-key credentials, and request headers are not included. The moderation controller does not log request bodies. Operators must not place credentials in the free-text internal reason.

## Account state interaction

For SQL-backed links, redirect persistence lookup and cache revalidation require an existing owner whose account status is `Active`. `Suspended` and `Disabled` owners retain their links and ownership data, but their links resolve as unavailable. Existing sign-in, refresh-session, and API-key checks already reject non-active accounts. Restoring an account to `Active` makes otherwise eligible links resolvable again; moderation, deletion, activation, expiry, and custom-domain rules still apply independently.

## Public reporting decision

A public report endpoint was deliberately not added. The service has no staffed queue, deduplication/retention policy, or bounded case-management workflow, so accepting reports would create an unbounded spam sink without meaningful moderation. A future reporting task must define those controls and apply a dedicated distributed rate-limit policy before exposing ingestion.

## Migration and operator setup

Apply migration `20260824095324_AddAbuseControlModeration`. Assign the exact `Moderator` Identity role to approved operator accounts through a controlled administrative process, then require the operator to sign in or refresh so a new access token contains the role. Do not allow self-service role assignment.

## Manual verification record

- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and zero errors.
- `npm.cmd run build` completed; only the repository's existing component-style budget warnings were emitted.
- A development API smoke run served OpenAPI successfully, rejected an unauthenticated moderation request with `401 AUTHENTICATION_REQUIRED`, and returned the normal `404 NOT_FOUND` contract for a missing redirect.
- Angular template compilation verified the restricted notice, label, and suppression of open/QR actions.
- Backend inspection verified that blocked, suspended, and disabled state participates in both persistence resolution and cached-route revalidation, while owner lookup and mutation methods remain owner-scoped.
- No automated test files were created, as required by this task.

## Phase 12 Completion Gate

Phase 12 is complete when TASK-044 through TASK-047 are completed and authentication/HTTP controls, input boundaries, analytics privacy, and abuse/moderation behavior are explicitly documented and enforced.
