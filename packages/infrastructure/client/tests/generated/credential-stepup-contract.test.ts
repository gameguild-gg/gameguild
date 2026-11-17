import { once } from 'node:events';
import { createServer } from 'node:http';
import { describe, expect, it } from 'vitest';
import { createClient } from '../../src/client.js';
import { AuthApiKeysModule } from '../../src/generated/modules/auth-api-keys.gen.js';
import { AuthServiceAccountsModule } from '../../src/generated/modules/auth-service-accounts.gen.js';
import { AuthStepUpModule } from '../../src/generated/modules/auth-step-up.gen.js';
import { IdentityAuthenticationStepUpAuthenticationRequiredOutputSchema } from '../../src/generated/types.gen.js';

// Real generated methods, validation and HTTP transport; backend authorization,
// factor verification and credential/receipt transactions have separate PostgreSQL coverage.
describe('generated credential step-up contract', () => {
  it.each(['api-key', 'service-secret'] as const)('explicitly verifies and retries the same %s intent', async kind => {
    const id = 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee';
    const receipt = 'test-only-credential-receipt';
    const keyIntent = { name: 'Synthetic key', scopes: ['reports:read'] };
    const initial = kind === 'api-key' ? keyIntent : undefined;
    const path = kind === 'api-key' ? '/v1/auth/api-keys' : `/v1/auth/service-accounts/${id}:rotate-secret`;
    const expiresAt = new Date(Date.now() + 300_000).toISOString();
    const demand = {
      status: 403, title: 'Fresh verification is required.', stepUpRequired: true,
      challengeId: id, stepUpToken: id, expiresAt,
      operation: { operationType: kind === 'api-key' ? 'create-api-key' : 'rotate-service-account-secret',
        targetReference: kind === 'api-key' ? 'current-user' : `service-account/${id}`, payloadHash: 'a'.repeat(64) },
      availableMethods: ['BackupCode'], riskLevel: 'High', riskFactors: ['SensitiveOperation'],
    };
    const observed: Array<{ path?: string; method?: string; authorization?: string; body: unknown }> = [];
    const server = createServer((request, response) => {
      let raw = '';
      request.on('data', (chunk: Buffer) => { raw += chunk.toString(); });
      request.on('end', () => {
        const body = raw ? JSON.parse(raw) as Record<string, unknown> : undefined;
        observed.push({ path: request.url, method: request.method, authorization: request.headers.authorization, body });
        response.setHeader('Content-Type', 'application/json');
        if (request.url === `/v1/auth/step-up/challenges/${id}:verify`) {
          response.end(JSON.stringify({ receipt, expiresAt }));
        } else if (body?.stepUpReceipt === receipt) {
          response.end(JSON.stringify(kind === 'api-key'
            ? { id, name: 'Synthetic key', apiKey: 'test-only-issued-key', keyPrefix: 'test', scopes: ['reports:read'],
              expiresAt: null, createdAt: '2026-11-09T12:00:00Z' }
            : { clientSecret: 'test-only-rotated-secret', warning: 'Synthetic response' }));
        } else {
          response.writeHead(403);
          response.end(JSON.stringify(demand));
        }
      });
    });
    const listening = once(server, 'listening'); server.listen(0, '127.0.0.1'); await listening;
    try {
      const address = server.address();
      if (!address || typeof address === 'string') throw new Error('Missing test port');
      const client = createClient({ baseUrl: `http://127.0.0.1:${address.port}`,
        auth: { getAccessToken: async () => 'test-only-credential-owner' }, devtools: { enabled: false } });
      const keys = new AuthApiKeysModule(client);
      const accounts = new AuthServiceAccountsModule(client);
      const required = kind === 'api-key'
        ? await keys.postAuthApiKeys(keyIntent)
        : await accounts.postAuthServiceAccountsRotateSecret(id);
      expect(required.ok).toBe(false);
      if (required.ok) throw new Error('Expected verification demand');
      expect(required.error.code).toBe('FORBIDDEN');
      expect(observed).toHaveLength(1);
      const parsed = IdentityAuthenticationStepUpAuthenticationRequiredOutputSchema.safeParse(required.error.metadata?.stepUp);
      expect(parsed.success).toBe(true);
      if (!parsed.success || !parsed.data.challengeId) throw new Error('Missing typed challenge');
      expect(parsed.data).toEqual(demand);
      const verified = await new AuthStepUpModule(client).postAuthStepUpChallengesVerify(parsed.data.challengeId,
        { method: 'BackupCode', evidence: 'test-only-factor' });
      expect(verified.ok).toBe(true);
      if (!verified.ok || !verified.data.receipt) throw new Error('Missing verification receipt');
      const finalBody = { ...initial, stepUpReceipt: verified.data.receipt };
      const result = kind === 'api-key'
        ? await keys.postAuthApiKeys({ ...keyIntent, stepUpReceipt: verified.data.receipt })
        : await accounts.postAuthServiceAccountsRotateSecret(id, finalBody);
      expect(result.ok).toBe(true);
      expect(observed).toEqual([
        { method: 'POST', path, authorization: 'Bearer test-only-credential-owner', body: initial },
        { method: 'POST', path: `/v1/auth/step-up/challenges/${id}:verify`, authorization: 'Bearer test-only-credential-owner',
          body: { method: 'BackupCode', evidence: 'test-only-factor' } },
        { method: 'POST', path, authorization: 'Bearer test-only-credential-owner', body: finalBody },
      ]);
    } finally {
      await new Promise<void>((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
    }
  });
});
