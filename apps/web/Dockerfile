# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS pruner
ENV PNPM_HOME=/pnpm
ENV PATH=$PNPM_HOME:$PATH
RUN corepack enable \
    && corepack prepare pnpm@10.0.0 --activate \
    && pnpm add --global turbo@2.10.11
WORKDIR /repo
COPY . .
RUN turbo prune @game-guild/web --docker

FROM node:22-alpine AS toolchain
WORKDIR /repo
COPY tools/emception/packages/toolchain/package.json ./tools/emception/packages/toolchain/package.json
RUN --mount=type=cache,id=emception-npm,target=/root/.npm \
    TOOLCHAIN_VERSION="$(node -p 'require("./tools/emception/packages/toolchain/package.json").version')" \
    && mkdir -p /repo/tools/emception/artifacts/toolchain/release/cdn /tmp/emception-extract \
    && cd /tmp/emception-extract \
    && npm pack "emception@${TOOLCHAIN_VERSION}" \
    && tar xzf ./*.tgz \
    && cp -r package/cdn/. /repo/tools/emception/artifacts/toolchain/release/cdn/

FROM node:22-alpine AS deps
ENV PNPM_HOME=/pnpm
ENV PATH=$PNPM_HOME:$PATH
RUN corepack enable && corepack prepare pnpm@10.0.0 --activate
WORKDIR /repo
COPY --from=pruner /repo/out/json/ ./
COPY --from=pruner /repo/out/pnpm-lock.yaml ./pnpm-lock.yaml
RUN --mount=type=cache,id=pnpm-web,target=/pnpm/store \
    pnpm install --frozen-lockfile --prefer-offline --store-dir=/pnpm/store

FROM deps AS builder
ARG GIT_HASH
ARG AUTH_SECRET
ARG API_INTERNAL_URL
ARG API_PUBLIC_URL
ARG WEB_PUBLIC_URL
ARG AUTH_COOKIE_DOMAIN
ARG NEXT_PUBLIC_GOOGLE_CLIENT_ID
ARG SOURCE_SHA=unknown
ARG SOURCE_TREE=unknown
ARG RELEASE_SHA=unknown
ARG VERSION=unknown
ARG BUILD_TIMESTAMP=unknown

ENV GIT_HASH=${GIT_HASH}
ENV NEXT_TELEMETRY_DISABLED=1
ENV NEXT_PRIVATE_BUILD_WORKER_COUNT=1
# Devtron's GameGuild build profile allows 6 GiB for this container, leaving
# native/SWC headroom around the 4 GiB V8 heap required by generated client DTS.
ENV NODE_OPTIONS=--max-old-space-size=4096
ENV GAMEGUILD_DISABLE_WEBPACK_CACHE=1
ENV NEXTAUTH_URL=${WEB_PUBLIC_URL}
ENV AUTH_TRUST_HOST=true
ENV API_URL=${API_INTERNAL_URL:-http://gameguild-api-internal:3000}
ENV NEXT_PUBLIC_API_URL=${API_PUBLIC_URL}
ENV NEXT_PUBLIC_APP_URL=${WEB_PUBLIC_URL}
ENV NEXT_PUBLIC_GOOGLE_CLIENT_ID=${NEXT_PUBLIC_GOOGLE_CLIENT_ID}
ENV WEB_PUBLIC_URL=${WEB_PUBLIC_URL}
ENV AUTH_COOKIE_DOMAIN=${AUTH_COOKIE_DOMAIN}
ENV GAMEGUILD_VERSION=${VERSION}
ENV RELEASE_SHA=${RELEASE_SHA}
ENV SOURCE_TREE=${SOURCE_TREE}
ENV BUILT_AT=${BUILD_TIMESTAMP}

COPY --from=pruner /repo/out/full/ ./
# turbo prune only emits package directories; the emception sub-workspace root
# files below are required at build time (tsconfig "extends" chain and the
# coi-serviceworker.js copied by the browser package build).
COPY --from=pruner /repo/tools/emception/tsconfig.base.json ./tools/emception/tsconfig.base.json
COPY --from=pruner /repo/tools/emception/public/coi-serviceworker.js ./tools/emception/public/coi-serviceworker.js
COPY --from=pruner /repo/tools/emception/scripts/ ./tools/emception/scripts/
COPY --from=pruner /repo/tools/emception/packages/toolchain/package.json ./tools/emception/packages/toolchain/package.json
COPY --from=toolchain /repo/tools/emception/artifacts/toolchain/release/cdn/ ./tools/emception/artifacts/toolchain/release/cdn/

RUN --mount=type=secret,id=auth_secret,required=false \
    mounted_secret="$(cat /run/secrets/auth_secret 2>/dev/null || true)" \
    && export AUTH_SECRET="${mounted_secret:-$AUTH_SECRET}" \
    && if [ -z "$AUTH_SECRET" ]; then echo "AUTH_SECRET build secret is required" >&2; exit 1; fi \
    && pnpm --filter @game-guild/web build

# Next.js output tracing includes optional native packages for every supported
# platform. The runtime image is Alpine amd64, so retaining the other targets
# creates oversized registry layers without adding executable functionality.
RUN pnpm_root="apps/web/.next/standalone/node_modules/.pnpm" \
    && for directory in \
      "$pnpm_root"/@swc+core-* \
      "$pnpm_root"/@img+* \
      "$pnpm_root"/@esbuild+*; do \
        [ -d "$directory" ] || continue; \
        case "$directory" in \
          *@swc+core-linux-x64-musl@*|*@img+sharp-linuxmusl-x64@*|*@img+sharp-libvips-linuxmusl-x64@*|*@esbuild+linux-x64@*) ;; \
          *) rm -rf "$directory" ;; \
        esac; \
      done

# Cloudflare fronts the public registry and rejects individual upload blobs
# larger than its request limit. Preserve the exact standalone filesystem while
# distributing pnpm package directories across deterministic Docker layers.
RUN standalone_root="/repo/apps/web/.next/standalone" \
    && runtime_root="/repo/apps/web/.next/runtime-sharded" \
    && rm -rf "$runtime_root" \
    && mkdir -p "$runtime_root/root" \
    && for shard in 0 1 2 3 4 5 6 7; do mkdir -p "$runtime_root/shards/$shard"; done \
    && cp -a "$standalone_root"/. "$runtime_root/root"/ \
    && pnpm_root="$runtime_root/root/node_modules/.pnpm" \
    && shard=0 \
    && for directory in "$pnpm_root"/*; do \
         [ -d "$directory" ] || continue; \
         mv "$directory" "$runtime_root/shards/$shard"/; \
         shard=$(( (shard + 1) % 8 )); \
       done

FROM node:22-alpine AS runner
ARG SOURCE_SHA=unknown
ARG SOURCE_TREE=unknown
ARG RELEASE_SHA=unknown
ARG VERSION=unknown
ARG BUILD_TIMESTAMP=unknown

LABEL org.opencontainers.image.source="https://github.com/gameguild-gg/gameguild" \
      org.opencontainers.image.revision="${SOURCE_SHA}" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.created="${BUILD_TIMESTAMP}" \
      io.gameguild.source-tree="${SOURCE_TREE}"

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
ENV GAMEGUILD_VERSION=${VERSION}
ENV RELEASE_SHA=${RELEASE_SHA}
ENV SOURCE_TREE=${SOURCE_TREE}
ENV BUILT_AT=${BUILD_TIMESTAMP}
ENV IMAGE_DIGEST=Unknown
ENV DEPLOYED_AT=Unknown

WORKDIR /app
RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/runtime-sharded/root ./
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/runtime-sharded/shards/0 ./node_modules/.pnpm/
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/runtime-sharded/shards/1 ./node_modules/.pnpm/
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/runtime-sharded/shards/2 ./node_modules/.pnpm/
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/runtime-sharded/shards/3 ./node_modules/.pnpm/
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/runtime-sharded/shards/4 ./node_modules/.pnpm/
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/runtime-sharded/shards/5 ./node_modules/.pnpm/
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/runtime-sharded/shards/6 ./node_modules/.pnpm/
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/runtime-sharded/shards/7 ./node_modules/.pnpm/
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/static ./apps/web/.next/static
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/public ./apps/web/public
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/static-cache-guard.cjs ./static-cache-guard.cjs

USER nextjs
EXPOSE 3000
CMD ["node", "static-cache-guard.cjs"]
