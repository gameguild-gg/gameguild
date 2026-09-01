#!/usr/bin/env node

import { readFileSync, writeFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const fullShaPattern = /^[0-9a-f]{40}$/u;
const digestPattern = /^sha256:[0-9a-f]{64}$/u;
const supportedServices = new Set(['api', 'web', 'learning']);

function requireText(value, name) {
  if (typeof value !== 'string' || value.trim() === '') {
    throw new TypeError(`${name} is required`);
  }
  return value.trim();
}

function requireSha(value, name) {
  const sha = requireText(value, name);
  if (!fullShaPattern.test(sha)) throw new TypeError(`${name} must be a full Git SHA`);
  return sha;
}

function normalizeService(service, activeReleaseSha, activeTreeSha) {
  if (!service || typeof service !== 'object' || Array.isArray(service)) {
    throw new TypeError('each release service must be an object');
  }

  const name = requireText(service.service, 'service');
  if (!supportedServices.has(name)) throw new TypeError(`unsupported service ${name}`);

  const imageDigest = requireText(service.imageDigest, `${name} imageDigest`);
  if (!digestPattern.test(imageDigest)) {
    throw new TypeError(`${name} imageDigest must be an immutable sha256 digest`);
  }

  return {
    service: name,
    image: requireText(service.image, `${name} image`),
    imageDigest,
    sourceSha: requireSha(service.sourceSha, `${name} sourceSha`),
    releaseSha: requireSha(service.releaseSha ?? activeReleaseSha, `${name} releaseSha`),
    treeSha: requireSha(service.treeSha ?? activeTreeSha, `${name} treeSha`),
  };
}

export function createReleaseManifest(input) {
  if (!input || typeof input !== 'object' || Array.isArray(input)) {
    throw new TypeError('release manifest input must be an object');
  }

  const releaseSha = requireSha(input.releaseSha, 'releaseSha');
  const treeSha = requireSha(input.treeSha, 'treeSha');
  const services = (input.services ?? []).map((service) => normalizeService(service, releaseSha, treeSha));
  if (services.length === 0) throw new TypeError('at least one release service is required');

  const seen = new Set();
  for (const { service } of services) {
    if (seen.has(service)) throw new TypeError(`duplicate service ${service}`);
    seen.add(service);
  }

  const releasedAt = requireText(input.releasedAt, 'releasedAt');
  if (Number.isNaN(Date.parse(releasedAt))) throw new TypeError('releasedAt must be an ISO timestamp');

  return {
    schemaVersion: 1,
    releaseSha,
    treeSha,
    releasedAt,
    migrationRequired: input.migrationRequired === true,
    verificationRunIds: [...new Set((input.verificationRunIds ?? []).map((id) => requireText(String(id), 'verificationRunId')))],
    services,
  };
}

function parseArguments(argv) {
  const options = {};
  for (let index = 0; index < argv.length; index += 2) {
    const name = argv[index];
    const value = argv[index + 1];
    if (!name?.startsWith('--') || value === undefined) {
      throw new TypeError(`Unknown or incomplete argument: ${name ?? ''}`);
    }
    options[name.slice(2)] = value;
  }
  return options;
}

function main() {
  const options = parseArguments(process.argv.slice(2));
  const services = JSON.parse(readFileSync(requireText(options['services-file'], 'services-file'), 'utf8'));
  const manifest = createReleaseManifest({
    releaseSha: options['release-sha'],
    treeSha: options['tree-sha'],
    releasedAt: options['released-at'],
    migrationRequired: options['migration-required'] === 'true',
    verificationRunIds: (options['verification-run-ids'] ?? '').split(',').filter(Boolean),
    services,
  });
  const output = `${JSON.stringify(manifest, null, 2)}\n`;
  if (options.output) writeFileSync(options.output, output, 'utf8');
  else process.stdout.write(output);
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) main();
