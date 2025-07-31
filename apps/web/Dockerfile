FROM node:lts-alpine AS base
# Update system image and install dependencies in a single layer
RUN apk update && apk add --no-cache \
    libc6-compat \
    curl \
    && rm -rf /var/cache/apk/*

# Build Stage
FROM base AS builder
WORKDIR /app

# Accept build arguments for Next.js public environment variables
ARG NEXT_PUBLIC_API_URL  
ARG NEXT_PUBLIC_WEB_URL

# Set environment variables from build args
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_WEB_URL=$NEXT_PUBLIC_WEB_URL
ENV NODE_ENV=production

# Copy package files first for better layer caching
COPY package*.json ./

# Install ALL dependencies including dev dependencies needed for build
RUN npm install --include=dev && \
    npm cache clean --force

# Copy source code
COPY . .

# Build the application
RUN npm run build:prod

# Production Stage
FROM base AS runner
WORKDIR /app

# Create non-root user for security
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# Copy built application from builder stage
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
COPY --from=builder --chown=nextjs:nodejs /app/public ./public

# Set environment variables for Next.js to listen on all addresses
ENV NODE_ENV=production
ENV HOSTNAME=0.0.0.0
ENV PORT=3000

# New Relic environment variables
ENV NEW_RELIC_NO_CONFIG_FILE=true
ENV NEW_RELIC_DISTRIBUTED_TRACING_ENABLED=true
ENV NEW_RELIC_LOG=stdout
ENV NEW_RELIC_AI_MONITORING_ENABLED=true
ENV NEW_RELIC_CUSTOM_INSIGHTS_EVENTS_MAX_SAMPLES_STORED=100k
ENV NEW_RELIC_SPAN_EVENTS_MAX_SAMPLES_STORED=10k

# Switch to non-root user
USER nextjs

# Expose port
EXPOSE 3000

# Health check - use 0.0.0.0 to listen on all addresses
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://0.0.0.0:3000/ || exit 1

# Start the application
CMD ["node", "server.js"]