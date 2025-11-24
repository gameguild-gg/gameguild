ARG NODE_VERSION=lts
#
FROM node:${NODE_VERSION}-alpine AS builder
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