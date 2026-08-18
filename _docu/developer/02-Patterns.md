# Patterns

This document describes the design patterns used in the AccessRight Service.

## Repository Pattern

The repository pattern is used in the backend to abstract the data access layer. The `AccessService.Domain/Repositories/IAccessRightRepository` interface defines the methods for accessing accessRight data, and the `AccessService.Infrastructure/Repositories/AccessRightRepository` class provides the implementation. This pattern allows to easily switch the database implementation without changing the business logic.

There is also a Repository for Usergroups and OutboxEvents.

## Options Pattern

The options pattern is used to configure the application. The `KeycloakOptions` class defines the configuration options, and the `appsettings.json` file provides the values. This pattern allows to change the configuration without recompiling the application.

## Outbox Pattern

The outbox pattern is implemented to ensure reliable event publishing when a new Guideline is uploaded. This implementation guarantees reliable delivery and error handling by supporting automatic retries.

## Mediator Pattern

The mediator pattern is implemented using [MassTransit](https://masstransit-project.com/). In our service, requests are sent via MassTransit’s mediator component (`IMediator`). Handlers for each query are implemented separately, encapsulating business logic for each operation. The mediator receives these requests and dispatches appropriate responses.