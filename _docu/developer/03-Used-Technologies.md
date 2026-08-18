# Used Technologies

This document lists the technologies used in the AccessRight Service.

## Backend

- **.NET 8**: The backend is built with .NET framework.
- **ASP.NET Core**: The backend uses ASP.NET Core for building the web API.
- **Entity Framework Core**: The backend uses Entity Framework Core for data access.
- **Keycloak**: The backend uses Keycloak for authentication and authorization.
- **NUKE**: The build process is automated with NUKE.
- **Docker**: The Api is containerized with Docker.
- **MinIO**: The backend uses MinIO for caching the guideline stored in MinIO with its classification data.
- **Kafka**: The backend uses Kafka to publish AccessRight update messages.
- **Swagger / Swashbuckle**: Used for API documentation and interactive Swagger UI.
- **OpenTelemetry**: Used for distributed tracing and monitoring.
- **PostgreSQL**: Serves as the main relational database.
- **AutoMapper**: The backend uses AutoMapper for object-to-object mapping.

## Frontend

### Core Frameworks & Libraries

- **React**: The main library for building user interfaces.
- **React DOM**: DOM bindings for React.
- **TypeScript**: The frontend is written in TypeScript, a strongly-typed superset of JavaScript for safer and more maintainable code.

### State Management & Data Fetching

- **@tanstack/react-query**: For efficient data synchronization and server state management.

### Routing & Authentication

- **react-router-dom**: For client-side routing.
- **react-oidc-context**: Handles OpenID Connect (OIDC) based authentication and user session management.

### UI Component Libraries & Styling

- **@mui/material, @mui/icons-material**: Modern UI component library (Material UI) for building feature-rich interfaces, with icon support.
- **@emotion/react, @emotion/styled**: CSS-in-JS libraries for styling React components.
- **tailwindcss**: Utility-first CSS framework for rapidly building custom UIs.
- **sonner**: For notifications and toasts.
- **react-window**: Library for efficiently rendering large lists and grids by only rendering the items visible in the viewport.

### Tooling & Developer Experience

- **vite**: Build tool and development server.
- **@vitejs/plugin-react**: React integration for Vite.
- **eslint, @typescript-eslint/eslint-plugin, eslint-plugin-react-hooks, eslint-plugin-react-refresh**: Code linting tools to enforce code quality and best practices.
- **openapi-typescript-codegen**: Generates TypeScript client code from OpenAPI specifications.
- **autoprefixer, postcss**: Post-processing tools for enhanced browser CSS compatibility.
- **@originjs/vite-plugin-federation**: Enables module federation/micro-frontend setup with Vite.