# Guideline Service

:::info

Make sure to install the Docker Desktop App.

:::

Clone the Guideline Service repository from the dev branch and start the docker-compose.yml in it.
Make sure to lookup the container version from 
[gitea](https://gitea.ekkodale.biz/ekkodaleData/Guideline/packages)
 and put it into the .env before building the container.

```tsx title=".env"
COMPOSE_PROJECT_NAME=ekkodale-guideline

GUIDELINE_SERVICE_CONTAINERNAME=guideline-service
GUIDELINE_SERVICE_PORT=5008
GUIDELINE_SERVICE_TAG=1.20240429.11-dev

NETWORK_NAME=guideline-network
```

```tsx
docker-compose up -D
```