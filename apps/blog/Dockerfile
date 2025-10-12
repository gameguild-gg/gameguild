FROM node:lts-alpine AS base
# Update system image.
RUN apk update
# Install system image dependencies.
RUN apk add --no-cache libc6-compat curl
# Delete APK cache to reduce image size.
RUN rm -rf /var/cache/apk/*
# Install latest NPN version globally.
RUN npm install -g npm@latest
# Clean NPN cache.
RUN npm cache clean --force

# Build Stage.
FROM base AS builder
# Create app directory.
WORKDIR /app
# Copy root package.json and package-lock.json first for better caching
COPY package*.json ./
# Copy packages directory (contains local workspace dependencies)
COPY packages/ ./packages/
# Copy blog app package.json
COPY apps/blog/package*.json ./apps/blog/
# Generate package-lock.json from root
RUN npm install --package-lock-only
# Install all dependencies (including workspace dependencies)
RUN npm ci
# Clean NPN cache.
RUN npm cache clean --force
# Copy blog app source code
COPY apps/blog/ ./apps/blog/
# Set working directory to blog app
WORKDIR /app/apps/blog
# Build app.
RUN npm run build 
# Prune dev dependencies from the blog app.
RUN npm prune --omit=dev

# Production Stage.
FROM base AS runner
# Define an argument for the hostname with a default value
ARG HOSTNAME="0.0.0.0"
# Define an argument for the port with a default value of 3000.
ARG PORT=3000
# Define an argument for the environment with a default value of "production".
ARG NODE_ENV=production
# Define an argument to disable telemetry with a default value of 1.
# This is useful for production environments where you want to minimize data collection.
ARG NEXT_TELEMETRY_DISABLED=1
# server.js is created by next build from the standalone output
# https://nextjs.org/docs/pages/api-reference/config/next-config-js/output
ENV HOSTNAME=${HOSTNAME}
# Set an environment variable for the port using the ARG value.
ENV PORT=${PORT}
# Set environment variables.
ENV NODE_ENV=${NODE_ENV}
# Disable telemetry to prevent Next.js from collecting anonymous usage data.
ENV NEXT_TELEMETRY_DISABLED=${NEXT_TELEMETRY_DISABLED}
# Create app directory.
WORKDIR /app
# Change ownership to node user.
RUN chown -R node:node /app
# Automatically leverage output traces to reduce image size.
# https://nextjs.org/docs/advanced-features/output-file-tracing.
# Copy the standalone node.js modules output runner container image.
COPY --from=builder --chown=node:node /app/apps/blog/.next/standalone/node_modules ./node_modules
# Copy application build files to the container image.
COPY --from=builder --chown=node:node /app/apps/blog/.next/standalone/apps/blog ./
# Copy static files generated during the build to the .next/static directory in the container image.
COPY --from=builder --chown=node:node /app/apps/blog/.next/static ./.next/static
# Copy files from the "public" directory to the corresponding "public" directory in the container image.
COPY --from=builder --chown=node:node /app/apps/blog/public ./public
# Set the user to run the application.
# This ensures that the application runs with non-root privileges, enhancing security.
USER node
# Expose the port to inform Docker that the container will listen on the specified port.
EXPOSE ${PORT}
# server.js is created by next build from the standalone output
# https://nextjs.org/docs/pages/api-reference/config/next-config-js/output
ENV HOSTNAME="0.0.0.0"
# Add a health check to ensure the service is running.
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 CMD curl -f http://localhost:${PORT}/ || exit 1
# Run the blog service on container startup.
CMD ["node", "server.js"]
