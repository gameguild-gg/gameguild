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

# Production image, copy all files and run next
FROM base AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

COPY --from=builder /app/apps/web/public ./public

# Automatically leverage output traces to reduce image size
# https://nextjs.org/docs/advanced-features/output-file-tracing
COPY --from=builder --chown=nextjs:nodejs /app/apps/web/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/web/.next/static ./.next/static

USER nextjs

EXPOSE 3000

ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]
