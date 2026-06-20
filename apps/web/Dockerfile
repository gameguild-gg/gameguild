# syntax=docker/dockerfile:1.6

FROM node:20-alpine AS deps
RUN corepack enable && corepack prepare pnpm@10.0.0 --activate
WORKDIR /repo

COPY pnpm-workspace.yaml pnpm-lock.yaml package.json .npmrc* ./
COPY apps/web/package.json ./apps/web/
COPY packages ./packages

RUN pnpm install --frozen-lockfile --prefer-offline

FROM deps AS builder
ARG GIT_HASH
ARG AUTH_SECRET
ARG API_INTERNAL_URL
ARG API_PUBLIC_URL
ARG WEB_PUBLIC_URL
ARG LEARNING_PUBLIC_URL

ENV GIT_HASH=${GIT_HASH}
ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_OPTIONS=--max-old-space-size=1536
ENV AUTH_SECRET=${AUTH_SECRET}
ENV NEXTAUTH_URL=${WEB_PUBLIC_URL}
ENV AUTH_TRUST_HOST=true
ENV API_URL=${API_INTERNAL_URL:-http://api:8080}
ENV NEXT_PUBLIC_API_URL=${API_PUBLIC_URL}
ENV NEXT_PUBLIC_APP_URL=${WEB_PUBLIC_URL}
ENV NEXT_PUBLIC_LEARNING_APP_URL=${LEARNING_PUBLIC_URL}

COPY . .

RUN if [ -z "$AUTH_SECRET" ]; then echo "AUTH_SECRET build arg is required" >&2; exit 1; fi \
    && pnpm --filter @game-guild/client build \
    && pnpm --filter @game-guild/web build

FROM node:20-alpine AS runner

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

WORKDIR /app

RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/static ./apps/web/.next/static
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/public ./apps/web/public

USER nextjs
EXPOSE 3000

CMD ["node", "apps/web/server.js"]
