FROM node:18-alpine AS base
# Update system image.
RUN apk update
# Install system image dependencies.
RUN apk add --no-cache libc6-compat curl

# Build Stage.
FROM base AS builder
# Create app directory.
WORKDIR /app
# Copy root package.json and package-lock.json first for better caching
COPY package*.json ./
# Copy packages directory (contains local workspace dependencies)
COPY packages/ ./packages/
# Copy web app package.json
COPY apps/web/package*.json ./apps/web/
# Generate package-lock.json from root
RUN npm install --package-lock-only
# Install all dependencies (including workspace dependencies)
RUN npm ci
# Clean npm cache.
RUN npm cache clean --force
# Copy web app source code
COPY apps/web/ ./apps/web/
# Set working directory to web app
WORKDIR /app/apps/web
# Build app.
RUN npm run build 
# Prune dev dependencies from the web app.
RUN npm prune --production

# Production Stage.
FROM base AS runner
# Create app directory.
WORKDIR /app
# Set environment variables.
ENV NODE_ENV production
# Disable telemetry to prevent Next.js from collecting anonymous usage data.
ENV NEXT_TELEMETRY_DISABLED=1
# Create a non-root group for running the application.
# The group is created with a specific GID (1001) to ensure consistent permissions.
# This is important for security and to avoid running the application as root.
RUN addgroup --system --gid 1001 nodejs
# Create a non-root user for running the application.
# The user is created with a specific UID (1001) to ensure consistent permissions.
# This is important for security and to avoid running the application as root.
RUN adduser --system --uid 1001 nextjs
# Change ownership to node user.
RUN chown -R node:node /usr/src/app

COPY --from=builder /app/public ./public

# Automatically leverage output traces to reduce image size.
# https://nextjs.org/docs/advanced-features/output-file-tracing.
# Copy application dependency manifests to the container image.
COPY --from=builder --chown=node:node /app/apps/web/package*.json ./
# Copy application dependencies to the container image.
COPY --from=builder --chown=node:node /app/apps/web/node_modules ./node_modules
# Copy application build files to the container image.
COPY --from=builder --chown=node:node /app/apps/web/.next/standalone ./
# Copy static files generated during the build to the .next/static directory in the container image.
COPY --from=builder --chown=node:node /app/apps/web/.next/static ./.next/static
# Copy files from the "public" directory to the corresponding "public" directory in the container image.
COPY --from=builder --chown=node:node /app/apps/web/public ./public
# Define an argument for the port with a default value of 3000.
ARG PORT=3000
# Expose the port to inform Docker that the container will listen on the specified port.
EXPOSE ${PORT}
# Set an environment variable for the port using the ARG value.
ENV PORT=${PORT}
# server.js is created by next build from the standalone output
# https://nextjs.org/docs/pages/api-reference/config/next-config-js/output
ENV HOSTNAME="0.0.0.0"
# Run the web service on container startup.
CMD ["node", "server.js"]
