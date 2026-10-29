# Architecture Plan: C# API on Hetzner

Status: **Draft v0.1** (proposal, no code yet)
Target: .NET 10 (current LTS, supported until Nov 2028) on Hetzner Cloud, EU region.

---

## 1. Goals and constraints

| Goal | What it means in practice |
|---|---|
| Low, predictable cost | Hetzner Cloud VMs, no hyperscaler managed services |
| EU data residency (GDPR) | Falkenstein (FSN1), Nuremberg (NBG1) or Helsinki (HEL1). Sign Hetzner's DPA |
| Reusable template | Clean layering, conventions and CI that new APIs can copy |
| Simple ops for a small team | Docker Compose first, Kubernetes only if a real need appears |
| Good frontend DX | OpenAPI spec that generates a typed TS client for React + TanStack Query |

**Key constraint:** Hetzner offers compute, networking, volumes, load balancers and S3-compatible Object Storage, but **no managed database, queue or secrets service**. Everything stateful is ours to run, back up and patch. This is the main trade-off of choosing Hetzner and is addressed in sections 5 and 8.

---

## 2. High-level topology

### Phase 1: single node (MVP, ~€10-20/month)

```
Internet
   │  443
   ▼
Hetzner Cloud Firewall (80/443 open, 22 from admin IPs/VPN only)
   │
   ▼
┌───────────────── VM (CAX21 ARM or CX32 x86) ─────────────────┐
│  Docker Compose                                              │
│   ┌────────┐     ┌──────────┐     ┌─────────────┐            │
│   │ Caddy  │ ──▶ │ api (x2) │ ──▶ │ PostgreSQL  │            │
│   │ TLS    │     │ .NET 10  │     │ + volume    │            │
│   └────────┘     └──────────┘     └─────────────┘            │
│                        │                 │                   │
│                  OTel collector     pgBackRest/WAL-G         │
└────────────────────────┼─────────────────┼───────────────────┘
                         ▼                 ▼
               Grafana Cloud (free)   Hetzner Object Storage (backups)
```

### Phase 2: split tiers (when load or uptime needs grow)

```
Hetzner Load Balancer (TLS, health checks)
   ├── app-1 (VM, Compose)  ┐
   └── app-2 (VM, Compose)  ┘── private network 10.0.0.0/16
                               └── db-1 (dedicated VM, Hetzner Volume, no public IP)
```

**Why not Kubernetes from day one?** k3s on Hetzner is viable, but it adds a control plane, ingress, storage drivers and upgrades to own. For one or a few APIs, Compose plus a load balancer covers zero-downtime deploys and horizontal scaling at a fraction of the complexity. Revisit if we pass ~5 services or need autoscaling.

**ARM (CAX) vs x86 (CX/CPX):** .NET runs well on linux-arm64 and CAX is cheaper per vCPU. The cost is a multi-arch Docker build and making sure every native dependency has an arm64 build. Recommendation: ARM, unless a dependency blocks it.

---

## 3. Application architecture

### Style: Clean Architecture with vertical feature slices

- **Layers** keep dependencies pointing inward (Api → Application → Domain; Infrastructure implements Application interfaces).
- **Feature folders** inside each layer keep a feature's endpoint, handler, validator and DTOs together, so changes stay local.
- **Minimal APIs** with endpoint groups (`MapGroup`) instead of controllers. Less ceremony, better AOT/trimming support, first-class in .NET 10.
- **No MediatR by default.** Plain handler classes registered in DI are enough and avoid a licensing question (MediatR went commercial in 2025). Add a pipeline library only if cross-cutting behaviors grow.

### Proposed file tree

```
c-sharp-api-template/
├── .github/
│   └── workflows/
│       ├── ci.yml                    # build, test, lint, image build on PR
│       └── deploy.yml                # push image to GHCR, deploy on main/tag
├── deploy/
│   ├── compose/
│   │   ├── compose.yml               # api, caddy, postgres, otel-collector
│   │   ├── compose.override.yml      # local dev overrides
│   │   └── Caddyfile
│   ├── infra/                        # OpenTofu (Terraform) for Hetzner
│   │   ├── main.tf                   # servers, network, firewall, LB
│   │   ├── variables.tf
│   │   ├── outputs.tf
│   │   └── cloud-init.yaml           # docker, users, ssh hardening, unattended-upgrades
│   └── scripts/
│       ├── deploy.sh                 # pull image, migrate, rolling restart
│       └── restore-db.sh             # documented and tested restore
├── docs/
│   ├── ARCHITECTURE.md               # this file
│   └── adr/                          # architecture decision records
│       └── 0001-hosting-on-hetzner.md
├── src/
│   ├── Template.Api/                 # HTTP edge: composition root only
│   │   ├── Program.cs
│   │   ├── Endpoints/
│   │   │   └── Orders/
│   │   │       └── OrderEndpoints.cs # MapGroup("/orders") + route handlers
│   │   ├── Infrastructure/           # Api-only concerns
│   │   │   ├── ExceptionHandling/    # IExceptionHandler → ProblemDetails
│   │   │   ├── Auth/                 # JWT bearer setup, policies
│   │   │   ├── RateLimiting/
│   │   │   └── OpenApi/              # doc transformers, Scalar UI
│   │   ├── appsettings.json
│   │   └── Dockerfile
│   ├── Template.Application/         # use cases, no HTTP or EF types
│   │   ├── Abstractions/             # IAppDbContext, IClock, ICurrentUser
│   │   ├── Common/                   # Result<T>, errors, paging
│   │   └── Orders/
│   │       ├── CreateOrder/
│   │       │   ├── CreateOrderCommand.cs
│   │       │   ├── CreateOrderHandler.cs
│   │       │   └── CreateOrderValidator.cs   # FluentValidation
│   │       └── GetOrder/
│   │           ├── GetOrderQuery.cs
│   │           ├── GetOrderHandler.cs
│   │           └── OrderResponse.cs
│   ├── Template.Domain/              # entities, value objects, domain rules
│   │   ├── Common/                   # Entity, IAuditable, DomainEvent
│   │   └── Orders/
│   │       ├── Order.cs
│   │       └── OrderStatus.cs
│   └── Template.Infrastructure/      # EF Core, external services
│       ├── Persistence/
│       │   ├── AppDbContext.cs
│       │   ├── Configurations/       # IEntityTypeConfiguration<T> per entity
│       │   └── Migrations/
│       ├── Services/                 # e.g. email, blob storage (S3 client)
│       └── DependencyInjection.cs    # AddInfrastructure(...)
├── tests/
│   ├── Template.Domain.Tests/        # pure unit tests
│   ├── Template.Application.Tests/   # handler tests with fakes
│   ├── Template.Api.IntegrationTests/# WebApplicationFactory + Testcontainers Postgres
│   └── Template.ArchitectureTests/   # NetArchTest: enforce layer rules
├── Directory.Build.props             # nullable, warnings as errors, analyzers
├── Directory.Packages.props          # central package versions
├── global.json                       # pin .NET SDK
├── .editorconfig
├── Template.slnx                     # new XML solution format (.NET 9+)
└── README.md
```

Rename `Template` to the product name when the template is used.

### Cross-cutting conventions

| Concern | Choice |
|---|---|
| Errors | RFC 9457 ProblemDetails everywhere, via `IExceptionHandler` + `Result<T>` mapping. No exceptions for expected failures |
| Validation | FluentValidation in Application, run by an endpoint filter |
| API docs | Built-in `Microsoft.AspNetCore.OpenApi` (Swashbuckle is no longer in the templates) + Scalar UI in non-prod |
| Versioning | URL segment `/v1/...` via `Asp.Versioning.Http`. Simple and cache-friendly |
| Auth | JWT bearer from an external IdP. Do not build our own user store |
| Rate limiting | Built-in `Microsoft.AspNetCore.RateLimiting`, per user/IP partition |
| CORS | Explicit allow-list of frontend origins per environment |
| Config | `appsettings.json` + environment variables. Options pattern with `ValidateOnStart()` |
| Time | Inject `TimeProvider`, never `DateTime.UtcNow` directly |
| Logging | Serilog or built-in logging, structured JSON to stdout, exported via OpenTelemetry |
| Health | `/health/live` (process up) and `/health/ready` (DB reachable). Used by Caddy/LB |

---

## 4. Data layer

- **PostgreSQL 17/18** with **EF Core 10 + Npgsql**.
- Migrations run as a **separate step in the deploy pipeline** (EF migration bundle), not on app startup. Startup migrations race when running two replicas.
- Use `IDbContextFactory` or scoped contexts. Enable connection pooling (Npgsql default) and set sane `Max Pool Size`.
- Write migrations to be **backwards compatible for one release** (expand/contract), so rolling deploys never break the old instance still running.

---

## 5. Hetzner infrastructure

| Resource | Use |
|---|---|
| Cloud Server (CAX/CX) | App and DB hosts |
| Cloud Firewall | Allow 80/443 public. SSH only from admin IPs or via Tailscale/WireGuard. DB port never public |
| Private Network | App ↔ DB traffic (Phase 2) |
| Volume | Postgres data dir, so the DB survives server rebuilds |
| Load Balancer (LB11) | Phase 2: TLS termination, health checks, two app nodes |
| Primary IP | Stable public IP for DNS in Phase 1 |
| Object Storage | S3-compatible bucket for DB backups and app file uploads |
| Snapshots/Backups | Server-level, nice to have. **Not** a DB backup strategy (crash-consistent only) |

**Provisioning:** OpenTofu with the `hetznercloud/hcloud` provider. State stored in Hetzner Object Storage (S3 backend). `cloud-init` installs Docker, creates a non-root deploy user, disables password SSH and enables `unattended-upgrades`.

**DNS:** Hetzner DNS or Cloudflare. Cloudflare in front (proxied) adds DDoS protection and WAF, at the cost of another vendor and seeing traffic. Hetzner already includes basic DDoS protection.

---

## 6. CI/CD

```
PR  ──▶ ci.yml: restore → build (warnings as errors) → unit + integration tests
                → dotnet format --verify-no-changes → docker buildx (amd64+arm64)

main ─▶ deploy.yml: build & push ghcr.io/<org>/<api>:<sha>
                  → run EF migration bundle against prod DB
                  → ssh deploy@host ./deploy.sh <sha>
                        - docker compose pull
                        - restart replica 1, wait for /health/ready
                        - restart replica 2, wait for /health/ready
                  → smoke test public /health/ready
```

- Images are tagged by commit SHA. Rollback = redeploy the previous SHA.
- Use GitHub Environments (`staging`, `production`) with required reviewers on production.
- Dockerfile: multi-stage, `mcr.microsoft.com/dotnet/aspnet:10.0` (or chiseled image) runtime, non-root user, `HEALTHCHECK`.

---

## 7. Secrets

Hetzner has no secrets manager, so:

1. **Source of truth:** GitHub Environment secrets (or 1Password/Bitwarden Secrets Manager if the org already uses one).
2. **On the host:** the deploy job writes a `.env` file owned by the deploy user, mode `600`. Compose injects it as environment variables.
3. Never bake secrets into images or commit them. Consider **SOPS + age** if we want encrypted secrets in Git.

---

## 8. Backups and recovery

Because the database is self-managed, this section is non-negotiable.

- **pgBackRest or WAL-G** shipping base backups + WAL to Hetzner Object Storage. Gives point-in-time recovery.
- Retention: e.g. 7 daily, 4 weekly, 3 monthly.
- Store backups in a **different Hetzner location** from the DB (e.g. DB in FSN1, bucket in HEL1).
- **Monthly automated restore test** into a throwaway server. An untested backup is not a backup.
- Targets to agree on: **RPO** (max data loss) ≤ 5 min, **RTO** (time to restore) ≤ 1 hour.

---

## 9. Observability

- **OpenTelemetry** SDK in the API (traces, metrics, logs) → OTel Collector container → **Grafana Cloud** free tier (Tempo/Prometheus/Loki).
  Alternative: self-host Grafana + Prometheus + Loki on a small VM. Cheaper at scale, more to maintain.
- Uptime check from outside Hetzner (Grafana synthetic, UptimeRobot or Better Stack) hitting `/health/ready`.
- Alerts: 5xx rate, p95 latency, DB connections, disk usage on the DB volume, backup job failures.

---

## 10. Security checklist

- [ ] SSH keys only, root login disabled, fail2ban or SSH behind VPN
- [ ] Cloud Firewall default deny, DB port closed publicly
- [ ] TLS everywhere (Caddy auto certs or LB managed certs), HSTS
- [ ] Containers run as non-root, read-only filesystem where possible
- [ ] Dependabot/Renovate for NuGet, Docker base images and GitHub Actions
- [ ] `dotnet list package --vulnerable` in CI
- [ ] Rate limiting and request size limits on public endpoints
- [ ] Security headers (Caddy or middleware)
- [ ] Hetzner DPA signed, data processing documented for GDPR

---

## 11. Frontend integration (React + TanStack Query)

- API publishes `/openapi/v1.json`.
- Frontend generates a typed client with **orval** or **openapi-typescript** + `openapi-fetch`. Orval can emit TanStack Query v5 hooks directly.
- Optional CI check: fail the PR if the committed OpenAPI spec drifts from the generated one, so breaking changes are visible in review.

---

## 12. Rough cost (verify on hetzner.com, prices changed in 2025)

| Phase | Items | Approx. monthly |
|---|---|---|
| 1 | 1× CAX21, 1× Volume 20 GB, Object Storage base, Primary IP | ~€10-20 |
| 2 | 2× app VMs, 1× DB VM, Volume, LB11, Object Storage | ~€40-70 |

Observability on Grafana Cloud free tier: €0 until limits are reached.

---

## 13. Open questions (please challenge these)

1. **What does the API do, and what load do we expect?** Requests/sec and data size decide whether Phase 1 is enough for a long time.
2. **Is self-managing Postgres acceptable?** If nobody wants to own backups, patching and failover, a managed EU Postgres (e.g. Scaleway, Aiven, Neon EU) next to Hetzner compute is a valid hybrid. Expect a few ms of extra latency and egress cost.
3. **Which identity provider?** Entra ID (if the org is Microsoft-based), Auth0/Clerk, or self-hosted Keycloak/Zitadel (another stateful service to run).
4. **Uptime target?** A single VM means planned downtime during host maintenance. 99.9% realistically needs Phase 2 plus a Postgres replica.
5. **Background jobs?** If yes, decide between `BackgroundService` + Postgres-backed queue (simple) or Hangfire/Quartz. Avoid adding Redis/RabbitMQ until needed.
6. **Is this a template for many APIs or one product?** A template justifies more structure (architecture tests, `dotnet new` template packaging). One product could start leaner, e.g. a single project with feature folders.

---

## 14. Suggested next steps

1. Agree on the open questions above and record them as ADRs in `docs/adr/`.
2. Scaffold the solution skeleton and one sample feature (`Orders`) end to end.
3. Add CI (build + tests + image) before any deployment work.
4. Provision Phase 1 with OpenTofu, deploy, and run the first restore test.
