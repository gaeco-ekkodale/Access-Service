# Postgres

:::info

Make sure to install the Docker Desktop App.

:::

We need to get the Postgres Database up and running. It starts with the Access Service.
Just head to AccessService\System.Tools\Docker\Local and execute the docker compose file.

```tsx
docker-compose up -D
```

The connection string is saved in the ASP.NET project under appsettings.Development.json

```tsx title="appsettings.Development.json"
"Postgres": {
  "Host": "localhost",
  "Port": "5432",
  "User": "user",
  "Password": "123",
  "Database": "postgres"
}
```