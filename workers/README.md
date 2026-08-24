# Worker Application Location

This directory contains independently hosted background processes, beginning with the asynchronous click analytics worker. The same worker host also runs centrally registered maintenance jobs; it does not run them in the API process or in the click-consumer loop.

Worker hosts are composition roots. Shared business workflows and ports belong in `UrlShortener.Application`; external adapters belong in `UrlShortener.Infrastructure`.

See [Background job scheduling](../docs/background-jobs.md) for registration, configuration, distributed ownership, failure semantics, and the manual verification procedure.
