<div align="center">
  <img src="https://raw.githubusercontent.com/gaeco-ekkodale/.github/main/assets/gaeco_logo_horizontal_color.png" width="200" alt="gaeco logo">

  # AccessService

  <em>Fine-grained access rights for classifications and their properties, resolved per use case and Keycloak user group.</em>

  [![License](https://img.shields.io/badge/license-fair--code-blue.svg)](LICENSE.md)
  [![Version](https://img.shields.io/github/v/release/gaeco-ekkodale/Access-Service)](../../releases)

  [gaeco-ekkodale Organization](https://github.com/gaeco-ekkodale) · [All Repos](https://github.com/orgs/gaeco-ekkodale/repositories)
</div>

---

gaeco (Graphs for Architecture, Engineering, Construction, Operations) is an event-driven microservice platform for BIM data management. It translates external building-industry standards (IFC, IBPDI, Brick Schema, ASHRAE 223 and others) into a shared, versioned classification and relationship model (Guideline + Ontology) and exposes consistent, graph-based building data (Instance) across use cases and departments — without forcing every consumer onto one rigid schema. Built for organizations managing building/portfolio data across disconnected departmental systems (construction, facilities management, leasing, accounting) that need automatic, reliable data propagation instead of manual, error-prone hand-offs.

> This project is licensed under the [Source Available](LICENSE.md). Source code is viewable and usable; commercial use is restricted.

---

## What this service does

The AccessService owns the platform's authorization model on top of the Guideline. It manages access rights via CRUD operations and answers the question "may this user group read or write this property, in this use case?".

An access right is uniquely identified by four attributes:

- the id of the underlying classification
- the property id within that classification
- the id of the use case
- the id of the Keycloak user group

Each combination carries one of three levels:

| Level     | Meaning                              |
| --------- | ------------------------------------ |
| **Read**  | The user can only read the data.     |
| **Write** | The user can read and write.         |
| **None**  | The user cannot see the data.        |

Other services — most notably the [InstanceService](https://github.com/gaeco-ekkodale/InstanceService) — evaluate these rights before creating instances, setting properties, or creating relationships.

## Repository Structure

- `Server/Api/`: ASP.NET Core Web API
- `Server/Domain/`: domain models and contracts
- `Server/Infrastructure/`: EF Core data access, MinIO and Kafka integration
- `Server/Events/`: Kafka event contracts
- `Server/Api.Tests/`, `Server/Infrastructure.Tests/`: unit tests
- `Client/`: React micro-frontend, exposed via Module Federation
- `_docker/`: Compose definition, env schemas and the App Registry package manifest
- `_docu/`: developer and user documentation
- `_pipeline/`: Azure DevOps CI/CD pipeline definitions
- `build/`: NUKE build scripts

## Tech Stack

- **Backend**: .NET 8, ASP.NET Core, Entity Framework Core, AutoMapper, Swagger/Swashbuckle, OpenTelemetry
- **Frontend**: React, TypeScript, Vite, Material UI, Tailwind CSS, React Query, Module Federation
- **Infrastructure**: PostgreSQL, MinIO, Apache Kafka, Keycloak, Docker
- **Build**: NUKE

## Local Development

### Prerequisites

- Docker Desktop
- .NET 8 SDK
- Node.js 20+
- The shared platform infrastructure (Keycloak, MinIO, Kafka) plus GuidelineService, UseCaseService, PluginHost and AppOrchestrator — see [`_docu/user/01-Installation.md`](_docu/user/01-Installation.md)

### Start with Docker Compose

```bash
cd _docker
docker compose -p access-service -f docker-compose.yml -f docker-compose-override.yml up -d
```

Ports are driven by the `ACCESS_*_OUTERPORT` variables in the environment files; the API exposes Swagger at `/swagger`.

### Run the client locally

```bash
cd Client
npm ci
npm run dev
```

The client is a micro-frontend. In an integrated setup the `access-client` container publishes its micro-frontend metadata, which the AppOrchestrator discovers and binds into the PluginHost automatically.

## Build and Test

```bash
./build.sh     # Linux/macOS
.\build.ps1    # Windows
```

- Backend tests: `dotnet test` from the repository root
- Frontend lint: `npm run lint` in `Client/`
- Frontend build: `npm run build` in `Client/`

## Integration

- **Authentication**: Keycloak (OIDC/JWT). The PluginHost authenticates the user and performs a token exchange to obtain a token scoped to the `access-client` plugin. Authentication is active whenever `ASPNETCORE_ENVIRONMENT` is not `Development`.
- **Events**: access right changes are published to Apache Kafka so that consuming services stay in sync without synchronous calls.
- **Guideline data**: classifications and properties are read from the guideline stored in MinIO and cached locally.

## Documentation

- [Concepts](_docu/developer/01-Concepts.md)
- [Patterns](_docu/developer/02-Patterns.md)
- [Used Technologies](_docu/developer/03-Used-Technologies.md)
- [Data Model](_docu/developer/04-Data-Model.md)
- [Software Architecture](_docu/developer/05-Software-Architecture.md)
- [Installation](_docu/user/01-Installation.md) · [User Manual](_docu/user/02-User-Manual.md)
