# Keycloak

:::info

Make sure to install the Docker Desktop App.

:::

## Docker setup

First, we need to create an external Docker network for the Access Service container to communicate with the Keycloak container.
```tsx
docker network create main_network
```

Then, we have to get the Keycloak Docker up and running.
From the root folder, navigate to the Keycloak folder, where you will find docker-compose.yml.

```tsx
docker-compose up -D
```

:::note

The Keycloak Service also starts with its own Postgres database.

:::

## Keycloak setup

Now we have to access the Keycloak interface. 
Open the site under the port you are running it and go to the admin console.
You get all the credentials from the .env in the Keycloak folder.

Navigate to the Clients tab in the sidebar and create a new client.

###Important settings

```
Client ID: access-service-be

Name: AccessService Backend

Valid redirect URIs: http://localhost:9345/*

Web origins: *

Client authentication: ON

Authentication flow: Standard flow, Direct access grants, Service accounts roles
```

After you have created the client, select it and go to the Service accounts roles tab. 
There, go to Assign roles, select the "Filter by clients" filter and add the query-groups, manage-clients, manage-users and view-clients roles to it.

Then go to the credentials tab, select Client Id and Secret under Client Authentication, 
copy the Client Secret and paste it into the Access Service Solution appsettings.Development.json.
