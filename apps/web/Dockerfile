FROM node:18-alpine AS base
# Update system image.
RUN apk update
# Install system image dependencies.
RUN apk add --no-cache libc6-compat

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
# Prune dev dependencies from the web app
RUN npm prune --production

# Production Stage.
FROM base AS runner
# Set environment variables.
ENV NODE_ENV production
# Create app directory.
WORKDIR /usr/src/app
# Change ownership to node user.
RUN chown -R node:node /usr/src/app
# create a non-root user.
USER node
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
# Set an environment variable for the port using the ARG value.
ENV PORT=${PORT}
# Expose the port to inform Docker that the container will listen on the specified port.
EXPOSE ${PORT}
# Run the web service on container startup.
CMD [ "npm", "run", "start" ]
