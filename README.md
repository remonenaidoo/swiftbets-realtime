# swiftbets-realtime

[![ci](https://github.com/remonenaidoo/swiftbets-realtime/actions/workflows/ci.yml/badge.svg)](https://github.com/remonenaidoo/swiftbets-realtime/actions/workflows/ci.yml)

Real-time push for SwiftBets: bridges Kafka to SignalR with typed, sequence-numbered deltas per group (`fixture:{id}`, `coupon:{id}`, `ops`, `risk`) so clients can detect gaps and refetch. Scales out on a Redis backplane.

## Hosts

- `SwiftBets.Realtime.Api`: the SignalR hub and Kafka bridge.

## Data and events

- **Owns:** Redis (SignalR backplane).
- **Events:** Consumes the platform event stream.

## Layout

Clean Architecture, enforced by project references and `*.ArchitectureTests`:

```
src/*.Domain          pure domain, no references
src/*.Application     use cases and ports; depends on Domain and contracts only
src/*.Infrastructure  adapters (Dapper + embedded .sql, Kafka, Redis); implements Application ports
src/*.Api | *.Worker  composition root: observability, error envelope, health, metrics
src/*.Migrator        DbUp scripts under Migrations/, run once before the host starts
```

Every host exposes `/health/live`, `/health/ready` (checks its real dependencies), `/metrics` (Prometheus), logs compact JSON with correlation ids, and exports traces over OTLP.

## Build and test

```bash
../swiftbets-platform/scripts/fetch-shared-packages.sh .   # or pack-local.sh for unreleased shared changes
dotnet test SwiftBets.Realtime.slnx
```

Integration tests use Testcontainers and need Docker. The whole platform runs from `swiftbets-platform` with `make up`.

## Images

Multi-arch (amd64 + arm64), non-root, chiseled runtime:

- `ghcr.io/remonenaidoo/swiftbets-realtime`

## License

MIT
