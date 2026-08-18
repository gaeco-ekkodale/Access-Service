# Concepts

This document describes the main concepts used in the AccessRight Service.

## Micro-Frontends

The Client of the Service is designed as a micro-frontend. To be able to use it, it can be uploaded into the `PluginHost` using the `PluginManager`. It can also be started locally if your Keycloak Client is configured correctly.

## AccessRight Management

The `AccessRight Service` is responsible for managing AccessRights by applying `CRUD` operations. This includes:

- **Creating AccessRights**
- **Fetch AccessRights**
- **Update AccessRights**
- **Delete AccessRights**

### AccessRights

AccessRights represent the level of access a keycloak user has. AccessRights are used for managing Classifications and Classification Properties.

- **Read** - The User can only read data.
- **Write** - The User can read and also write data.
- **None** - The User cannot see the data.

Getting a single unique AccessRight requires filtering using 4 attributes:

- The Id of the underlying classification
- The Property Id of the property inside the underlying classification
- The Id of the respective UseCase
- The Id of the Keycloak Usergroup

## Authentication and Authorization

### Inside Backend

Authentication and authorization are handled by Keycloak. Before requesting data from the `AccessRight Service`, a client must authenticate. Authentication can be enabled by setting the `ASPNETCORE_ENVIRONMENT` to any value other than `Development`.

### Inside Client

The `PluginHost` authenticates the user and then requests an access token specifically for the `access-client` Plugin by making a token exchange with the user token. The plugins can then use this token to authorize the user within the `AccessRight Api`.

## Event Driven Design with Kafka

The `AccessRight Service` uses an event-driven architecture to communicate changes in AccessRights across the system. This is implemented using [Apache Kafka](https://kafka.apache.org/) as the message broker.

### Kafka Events

Whenever a CRUD operation is applied to an AccessRight a corresponding event is published to Kafka. This event allows other services to subscribe to AccessRight changes, promoting loose coupling and enabling real-time reactions elsewhere in the platform.