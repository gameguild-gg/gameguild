# syntax=docker/dockerfile:1.6

FROM node:20-alpine AS deps
RUN corepack enable && corepack prepare pnpm@10.0.0 --activate
WORKDIR /repo

COPY pnpm-workspace.yaml pnpm-lock.yaml package.json .npmrc* ./
COPY apps/learning/package.json ./apps/learning/
COPY packages ./packages

RUN pnpm install --frozen-lockfile --prefer-offline

FROM deps AS builder
ARG GIT_HASH
ARG AUTH_SECRET
ARG API_INTERNAL_URL
ARG API_PUBLIC_URL
ARG LEARNING_PUBLIC_URL
ARG WEB_PUBLIC_URL

ENV GIT_HASH=${GIT_HASH}
ENV NEXT_TELEMETRY_DISABLED=1
ENV NEXT_PRIVATE_BUILD_WORKER_COUNT=1
ENV NODE_OPTIONS=--max-old-space-size=1024
ENV NEXTAUTH_URL=${LEARNING_PUBLIC_URL}
ENV AUTH_TRUST_HOST=true
ENV API_URL=${API_INTERNAL_URL:-http://api:8080}
ENV NEXT_PUBLIC_API_URL=${API_PUBLIC_URL}
ENV NEXT_PUBLIC_WEB_URL=${WEB_PUBLIC_URL}

COPY . .

RUN if [ -z "$AUTH_SECRET" ]; then echo "AUTH_SECRET build arg is required" >&2; exit 1; fi \
    && pnpm --filter @game-guild/client build \
    && pnpm --filter @game-guild/learning build

FROM node:20-alpine AS runner

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3002
ENV HOSTNAME=0.0.0.0

WORKDIR /app

RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

COPY --from=builder --chown=nextjs:nodejs /repo/apps/learning/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /repo/apps/learning/.next/static ./apps/learning/.next/static

USER nextjs
EXPOSE 3002

CMD ["node", "apps/learning/server.js"]
