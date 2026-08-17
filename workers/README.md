# Worker Application Location

This directory contains independently hosted background processes, beginning with the asynchronous click analytics worker.

Worker hosts are composition roots. Shared business workflows and ports belong in `UrlShortener.Application`; external adapters belong in `UrlShortener.Infrastructure`.
