# Worker Application Location

This directory is reserved for independently hosted background processes required by later roadmap phases, beginning with the asynchronous click pipeline where justified.

Worker hosts are composition roots. Shared business workflows and ports belong in `UrlShortener.Application`; external adapters belong in `UrlShortener.Infrastructure`. TASK-005 does not create a worker project or runtime behavior.
