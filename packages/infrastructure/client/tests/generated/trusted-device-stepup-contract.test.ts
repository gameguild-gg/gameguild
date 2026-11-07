import { once } from 'node:events';
import { createServer } from 'node:http';
import { describe, expect, it } from 'vitest';
import { createClient } from '../../src/client.js';
import { AuthTrustedDevicesModule } from '../../src/generated/modules/auth-trusted-devices.gen.js';
import { AuthStepUpModule } from '../../src/generated/modules/auth-step-up.gen.js';
import { IdentityAuthenticationStepUpAuthenticationRequiredOutputSchema } from '../../src/generated/types.gen.js';

// The generated methods and real HTTP transport are exercised here. Backend subject binding,
// factor verification, receipt consumption and rollback have separate PostgreSQL HTTP coverage.
describe('generated trusted-device step-up contract', () => {
  it('preserves the RFC demand and explicitly verifies before retrying the same operation', async () => {
    const id = 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee';
    const receipt = 'test-only-opaque-receipt';
    const deviceName = 'Synthetic step-up device';
    const expiresAt = new Date(Date.now() + 300_000).toISOString();
    const demand = {
      status: 403, title: 'Fresh verification is required.', traceId: 'synthetic-trace',
      stepUpRequired: true, challengeId: id, stepUpToken: id, expiresAt,
      operation: { operationType: 'trust-device', targetReference: 'current-session', payloadHash: 'a'.repeat(64) },
      availableMethods: ['BackupCode'], riskLevel: 'High', riskFactors: ['SensitiveOperation'],
    };
    const observed: Array<{ path?: string; method?: string; authorization?: string; body: unknown }> = [];
    const server = createServer((request, response) => {
      let raw = '';
      request.on('data', (chunk: Buffer) => { raw += chunk.toString(); });
      request.on('end', () => {
        const body = JSON.parse(raw) as Record<string, unknown>;
        observed.push({ path: request.url, method: request.method, authorization: request.headers.authorization, body });
        response.setHeader('Content-Type', 'application/json');
        if (request.url === `/v1/auth/step-up/challenges/${id}:verify`) {
          response.end(JSON.stringify({ receipt, expiresAt }));
        } else if (body.stepUpReceipt === receipt) {
          response.end(JSON.stringify({ message: 'Device trusted successfully' }));
        } else {
          response.writeHead(403);
          response.end(JSON.stringify(demand));
        }
      });
    });
    const listening = once(server, 'listening');
    server.listen(0, '127.0.0.1'); await listening;
    try {
      const address = server.address();
      if (!address || typeof address === 'string') throw new Error('Missing test port');
      const client = createClient({ baseUrl: `http://127.0.0.1:${address.port}`,
        auth: { getAccessToken: async () => 'test-only-owner' }, devtools: { enabled: false } });
      const devices = new AuthTrustedDevicesModule(client);
      const required = await devices.postAuthTrustedDevices({ deviceName });
      expect(required.ok).toBe(false);
      if (required.ok) throw new Error('Expected a verification demand');
      expect(required.error.status).toBe(403);
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
      const granted = await devices.postAuthTrustedDevices({ deviceName, stepUpReceipt: verified.data.receipt });
      expect(granted.ok).toBe(true);
      expect(observed).toEqual([
        { method: 'POST', path: '/v1/auth/trusted-devices', authorization: 'Bearer test-only-owner', body: { deviceName } },
        { method: 'POST', path: `/v1/auth/step-up/challenges/${id}:verify`, authorization: 'Bearer test-only-owner', body: { method: 'BackupCode', evidence: 'test-only-factor' } },
        { method: 'POST', path: '/v1/auth/trusted-devices', authorization: 'Bearer test-only-owner', body: { deviceName, stepUpReceipt: receipt } },
      ]);
    } finally {
      await new Promise<void>((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
    }
  });

  it.each([400, 401, 403])('keeps ordinary HTTP %i errors separate from a verification demand', async status => {
    const server = createServer((request, response) => {
      request.resume();
      request.on('end', () => {
        response.writeHead(status, { 'Content-Type': 'application/problem+json' });
        response.end(JSON.stringify({ status, title: 'Synthetic denial' }));
      });
    });
    const listening = once(server, 'listening'); server.listen(0, '127.0.0.1'); await listening;
    try {
      const address = server.address();
      if (!address || typeof address === 'string') throw new Error('Missing test port');
      const client = createClient({ baseUrl: `http://127.0.0.1:${address.port}`,
        auth: { getAccessToken: async () => 'test-only-owner' }, devtools: { enabled: false } });
      const result = await new AuthTrustedDevicesModule(client).postAuthTrustedDevices({ deviceName: 'Synthetic device' });
      expect(result.ok).toBe(false);
      if (result.ok) throw new Error('Expected an error');
      expect(result.error.status).toBe(status);
      expect(result.error.metadata?.stepUp).toBeUndefined();
    } finally {
      await new Promise<void>((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
    }
  });
});
