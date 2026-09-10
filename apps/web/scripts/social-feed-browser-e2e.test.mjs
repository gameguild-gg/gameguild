import assert from 'node:assert/strict';
import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

import {
  assertNonAdminActors,
  assertSocialEvidence,
  buildSocialRunMetadata,
  DEFAULT_SOCIAL_EVIDENCE_PATH,
  getAccessTokenRoles,
  missingSocialEvidence,
  readRequiredActorCredentials,
  REQUIRED_SOCIAL_EVIDENCE,
} from './social-feed-browser-e2e.mjs';

const socialScript = fileURLToPath(new URL('./social-feed-browser-e2e.mjs', import.meta.url));
const expectedEvidence = [
  'nonAdminActors',
  'crossActorMutationForbidden',
  'published',
  'mediaPublished',
  'postEdited',
  'trendingTagVisible',
  'followed',
  'profileMetrics',
  'followingVisible',
  'reacted',
  'commented',
  'replyCreated',
  'commentEdited',
  'commentDeleted',
  'reposted',
  'saved',
  'shared',
  'permalinkOpened',
  'savedVisible',
  'storyPublished',
  'storyMediaDelivered',
  'storyViewed',
  'storyDeleted',
  'persistedAfterReload',
  'streamsSeparated',
  'unfollowed',
  'postDeleted',
];

function jwt(payload) {
  return `ignored.${Buffer.from(JSON.stringify(payload)).toString('base64url')}.ignored`;
}

test('social feed browser gate locks every production interaction independently', () => {
  assert.deepEqual(REQUIRED_SOCIAL_EVIDENCE, expectedEvidence);
  assert.deepEqual(missingSocialEvidence({}), expectedEvidence);

  const evidence = Object.fromEntries(expectedEvidence.map((key) => [key, true]));
  assert.doesNotThrow(() => assertSocialEvidence(evidence));
});

test('social feed browser gate rejects incomplete evidence', () => {
  const evidence = Object.fromEntries(expectedEvidence.map((key) => [key, true]));
  evidence.persistedAfterReload = false;
  assert.throws(() => assertSocialEvidence(evidence), /persistedAfterReload/);
});

test('requires both existing actor credentials and lists every missing environment variable', () => {
  assert.deepEqual(readRequiredActorCredentials({
    SOCIAL_FEED_E2E_USER_A_EMAIL: 'a@example.test',
    SOCIAL_FEED_E2E_USER_A_PASSWORD: 'a-secret',
    SOCIAL_FEED_E2E_USER_B_EMAIL: 'b@example.test',
    SOCIAL_FEED_E2E_USER_B_PASSWORD: 'b-secret',
  }), {
    A: { email: 'a@example.test', password: 'a-secret' },
    B: { email: 'b@example.test', password: 'b-secret' },
  });

  assert.throws(
    () => readRequiredActorCredentials({ SOCIAL_FEED_E2E_USER_A_EMAIL: 'a@example.test' }),
    /SOCIAL_FEED_E2E_USER_A_PASSWORD, SOCIAL_FEED_E2E_USER_B_EMAIL, SOCIAL_FEED_E2E_USER_B_PASSWORD/,
  );
});

test('parses canonical, plural, and standard URI access-token role claims', () => {
  assert.deepEqual(getAccessTokenRoles(jwt({ role: 'User' })), ['User']);
  assert.deepEqual(getAccessTokenRoles(jwt({ roles: ['Member', 'Contributor'] })), ['Member', 'Contributor']);
  assert.deepEqual(
    getAccessTokenRoles(jwt({ 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Viewer' })),
    ['Viewer'],
  );
});

test('non-admin guard rejects every authoritative administrator-equivalent role', () => {
  for (const role of ['SystemAdmin', 'TenantAdmin', 'Admin', 'Owner']) {
    assert.throws(
      () => assertNonAdminActors(
        { id: 'actor-a', email: 'a@example.test', accessToken: jwt({ role }) },
        { id: 'actor-b', email: 'b@example.test', accessToken: jwt({ role: 'User' }) },
      ),
      new RegExp(`actor A.*${role}`, 'i'),
    );
  }
});

test('non-admin guard requires distinct actors with explicit non-admin role claims', () => {
  const actorA = { id: 'actor-a', email: 'a@example.test', accessToken: jwt({ role: 'User' }) };
  const actorB = { id: 'actor-b', email: 'b@example.test', accessToken: jwt({ roles: ['Member'] }) };
  assert.deepEqual(assertNonAdminActors(actorA, actorB), {
    actorARoles: ['User'],
    actorBRoles: ['Member'],
  });
  assert.throws(() => assertNonAdminActors(actorA, { ...actorB, id: actorA.id }), /distinct user IDs/i);
  assert.throws(
    () => assertNonAdminActors(actorA, { ...actorB, accessToken: jwt({ sub: 'actor-b' }) }),
    /actor B.*role claim/i,
  );
});

test('CLI fails closed before network access and writes failure evidence when credentials are absent', async () => {
  const directory = await mkdtemp(join(tmpdir(), 'social-feed-contract-'));
  const evidencePath = join(directory, 'evidence.json');
  const env = { ...process.env, SOCIAL_FEED_E2E_EVIDENCE_PATH: evidencePath };
  for (const name of [
    'SOCIAL_FEED_E2E_USER_A_EMAIL',
    'SOCIAL_FEED_E2E_USER_A_PASSWORD',
    'SOCIAL_FEED_E2E_USER_B_EMAIL',
    'SOCIAL_FEED_E2E_USER_B_PASSWORD',
  ]) delete env[name];

  try {
    const child = spawn(process.execPath, [socialScript], { env, stdio: ['ignore', 'pipe', 'pipe'] });
    let stderr = '';
    child.stderr.setEncoding('utf8');
    child.stderr.on('data', (chunk) => { stderr += chunk; });
    const [exitCode] = await once(child, 'exit');
    assert.equal(exitCode, 1);
    assert.match(stderr, /Missing required social feed E2E credentials/);
    const artifact = JSON.parse(await readFile(evidencePath, 'utf8'));
    assert.equal(artifact.status, 'failed');
    assert.equal(artifact.aggregate.passed, false);
    assert.equal(artifact.capabilities.nonAdminActors, false);
    assert.ok(artifact.errors.some((message) => /USER_A_EMAIL/.test(message)));
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
});

test('default evidence path is deterministic and ignored by the repository', () => {
  assert.match(DEFAULT_SOCIAL_EVIDENCE_PATH.replaceAll('\\', '/'), /\/\.tmp\/social-feed-browser-e2e\/evidence\.json$/);
});

test('run metadata records the real elapsed interval and target context', () => {
  assert.deepEqual(buildSocialRunMetadata(1_000, {
    tenantId: 'tenant-id',
    actorIds: ['actor-a', 'actor-b'],
    runTag: 'run-tag',
  }, 4_250), {
    journey: 'social-feed-browser-e2e',
    startedAt: '1970-01-01T00:00:01.000Z',
    completedAt: '1970-01-01T00:00:04.250Z',
    durationMs: 3250,
    apiBaseUrl: 'http://localhost:8080',
    webBaseUrl: 'http://localhost:3004',
    tenantId: 'tenant-id',
    actorIds: ['actor-a', 'actor-b'],
    runTag: 'run-tag',
  });
});
