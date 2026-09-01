#!/usr/bin/env node

import { writeFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const fullShaPattern = /^[0-9a-f]{40}$/u;
const digestPattern = /^sha256:[0-9a-f]{64}$/u;

function requireText(value, name) {
  if (typeof value !== 'string' || value.trim() === '') throw new TypeError(`${name} is required`);
  return value.trim();
}

export function createDevtronExternalCiPayload(input) {
  if (!input || typeof input !== 'object' || Array.isArray(input)) {
    throw new TypeError('Devtron payload input must be an object');
  }

  const digest = requireText(input.digest, 'digest');
  if (!digestPattern.test(digest)) throw new TypeError('digest must be an immutable sha256 digest');

  const releaseSha = requireText(input.releaseSha, 'releaseSha');
  if (!fullShaPattern.test(releaseSha)) throw new TypeError('releaseSha must be a full Git SHA');

  const commitTime = requireText(input.commitTime, 'commitTime');
  if (Number.isNaN(Date.parse(commitTime))) throw new TypeError('commitTime must be an ISO timestamp');

  const image = requireText(input.image, 'image');
  const tag = requireText(input.tag, 'tag');
  const branch = input.branch?.trim() || 'main';

  return {
    dockerImage: `${image}:${tag}`,
    digest,
    ciProjectDetails: [
      {
        gitRepository: requireText(input.repository, 'repository'),
        checkoutPath: input.checkoutPath?.trim() || './',
        commitHash: releaseSha,
        commitTime,
        branch,
        sourceValue: branch,
        message: input.message?.trim() || `release ${releaseSha}`,
        author: input.author?.trim() || 'GitHub Actions',
      },
    ],
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
  const payload = createDevtronExternalCiPayload({
    image: options.image,
    tag: options.tag,
    digest: options.digest,
    releaseSha: options['release-sha'],
    repository: options.repository,
    commitTime: options['commit-time'],
    branch: options.branch,
    checkoutPath: options['checkout-path'],
    message: options.message,
    author: options.author,
  });
  const output = `${JSON.stringify(payload, null, 2)}\n`;
  if (options.output) writeFileSync(options.output, output, 'utf8');
  else process.stdout.write(output);
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) main();
