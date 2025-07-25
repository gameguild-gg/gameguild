FROM node:lts-alpine AS base
# Update system image.
RUN apk update
# Install system image dependencies.
RUN apk add --no-cache libc6-compat curl

# Build Stage.
FROM base AS builder
# Create app directory.
WORKDIR /app

# Accept build arguments for Next.js public environment variables
ARG NEXT_PUBLIC_API_BASE_URL
ARG NEXT_PUBLIC_API_URL  
ARG NEXT_PUBLIC_WEB_URL

# Set environment variables from build args
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_WEB_URL=$NEXT_PUBLIC_WEB_URL

# Copy package files
COPY package*.json ./
# Generate package-lock.json.
RUN npm install --package-lock-only
# Install app dependencies.
RUN npm ci 
# Clean npm cache.
RUN npm cache clean --force
# Copy source code (excluding large assets via .dockerignore)
COPY . ./
# Build app.
RUN npm run build 
# Prune dev dependencies.
RUN npm prune --production

# Production Stage.
FROM base AS runner
# Set environment variables.
ENV NODE_ENV production
# Create app directory.
WORKDIR /app
# Change ownership to node user.
RUN chown -R node:node /app
# create a non-root user.
USER node
# Copy application build files to the container image.
COPY --from=builder --chown=node:node /app/.next/standalone ./
# Copy static files generated during the build to the .next/static directory in the container image.
COPY --from=builder --chown=node:node /app/.next/static ./.next/static
# Copy files from the "public" directory (excluding assets which are handled separately)
COPY --from=builder --chown=node:node /app/public ./public
# Define an argument for the port with a default value of 3000.
ARG PORT=3000
# Set an environment variable for the port using the ARG value.
ENV PORT=${PORT}
# Set hostname to bind to all interfaces
ENV HOSTNAME=0.0.0.0
# Expose the port to inform Docker that the container will listen on the specified port.
EXPOSE ${PORT}
# Run the web service on container startup.
CMD [ "node", "server.js" ]