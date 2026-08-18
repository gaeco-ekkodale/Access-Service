# Software Architecture

This document describes the software architecture of the AccessRight Service.

## Overview

The AccessRight Service consists of a frontend client and a backend service that provides a REST API for managing access rights and user groups. The backend is implemented with .NET and is organized into several distinct projects representing different layers of a clean architecture.

## Backend Architecture

The backend is a modular, multi-project solution and consists of the following layers:

- **API Layer (`AccessService.Api`)**:  
  This layer is responsible for handling incoming HTTP requests and sending responses. It contains controllers to process API calls, middleware, validators, and utilizes Data Transfer Objects (DTOs) for communication. The API layer is the main entry point for client interactions.
- **Domain Layer (`AccessService.Domain`)**:  
  The domain layer encapsulates models and Repository Interfaces of the application.
- **Infrastructure Layer (`AccessService.Infrastructure`)**:  
  This layer contains the implementation of repositories for data access, the Entity Framework `DbContext`, and database migrations.
- **Events Layer (`AccessService.Events`)**:  
  This layer is responsible for the definition of application events, such as `CreatedUserGroup` or events related to access rights.
- **Test Projects (`AccessService.Api.Tests`, `AccessService.Infrastructure.Tests`)**:  
  These separate projects contain unit and integration tests for their respective layers to ensure code quality and correctness.

## Frontend Architecture

The frontend is a single-page application (SPA) that is built with React. It uses the following components:

- **App**: The root component of the application.
- **StandaloneApp**: The root component of the application for working locally without pluginhost.
- **Features**: Components that define client logic.
- **Components**: The reusable components of the application.
- **API Clients**: The API clients that communicate with the backend.
- **Hooks**: Reusable hooks for reading the jwt token by decoding it.
- **Models**: Data models intended for use across the client.

## Communication

The frontend communicates with the backend via a REST API.