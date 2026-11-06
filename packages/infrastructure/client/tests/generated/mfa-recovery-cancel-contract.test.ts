import { once } from 'node:events';
import { createServer } from 'node:http';
import { describe, expect, it } from 'vitest';
import { createClient } from '../../src/client.js';
import { AuthMultiFactorModule } from '../../src/generated/modules/auth-multi-factor.gen.js';

// This exercises the generated method and real HTTP transport. Backend ownership,
// cancellation races and transaction rollback have separate PostgreSQL HTTP tests.
describe('generated owner MFA recovery cancellation', () => {
  it.each([
    { status: 200, cancelled: true, token: 'test-only-owner' },
    { status: 200, cancelled: false, token: 'test-only-owner' },
    { status: 401, cancelled: false, token: 'test-only-expired' },
    { status: 403, cancelled: false, token: 'test-only-service' },
    { status: 401, cancelled: false, token: undefined },
  ])('preserves HTTP $status and cancellation $cancelled for $token without an account selector', async ({ status, cancelled, token }) => {
    const observed: Array<{ method?: string; path?: string; authorization?: string; body: string }> = [];
    const responseBody = { success: true, cancelled, message: 'Recovery cancellation processed.' };
    const server = createServer((request, response) => {
      let body = '';
      request.on('data', (chunk: Buffer) => {
        body += chunk.toString();
      });
      request.on('end', () => {
        observed.push({ method: request.method, path: request.url, authorization: request.headers.authorization, body });
        response.writeHead(status, { 'Content-Type': status === 200 ? 'application/json' : 'application/problem+json' });
        response.end(JSON.stringify(status === 200 ? responseBody : { status, title: status === 401 ? 'Unauthorized' : 'Forbidden' }));
      });
    });
    const listening = once(server, 'listening');
    server.listen(0, '127.0.0.1');
    await listening;
    try {
      const address = server.address();
      if (!address || typeof address === 'string') throw new Error('Missing test port');
      const client = createClient({
        baseUrl: `http://127.0.0.1:${address.port}`,
        auth: { getAccessToken: () => Promise.resolve(token ?? null) },
        devtools: { enabled: false },
      });
      const result = await new AuthMultiFactorModule(client).postAuthMfaRecoveryCancel();
      // Missing owner credentials must be rejected locally by the generated auth contract.
      expect(observed).toEqual(token ? [{ method: 'POST', path: '/v1/auth/mfa/recovery:cancel', authorization: `Bearer ${token}`, body: '' }] : []);
      expect(result.ok).toBe(status === 200);
      if (result.ok) expect(result.data).toEqual(responseBody);
      else expect(result.error.status).toBe(status);
    } finally {
      await new Promise<void>((resolve, reject) => server.close((error) => (error ? reject(error) : resolve())));
    }
  });
});
