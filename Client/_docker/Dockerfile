# Build stage
FROM node:25-alpine AS builder

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build the application
RUN npm run build

# Production stage
FROM nginx:alpine

# Copy built assets from builder
COPY --from=builder /app/dist /usr/share/nginx/html

# Copy nginx configuration
COPY _docker/nginx.conf /etc/nginx/conf.d/default.conf

# Copy the env substitution script and schema
COPY _docker/docker-entrypoint.sh /docker-entrypoint.sh
COPY env.d.ts /app/env.d.ts
RUN chmod +x /docker-entrypoint.sh

# Expose port
EXPOSE 80

# Use the entrypoint script to inject environment variables at runtime
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
