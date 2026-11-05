import { once } from 'node:events';
import { createServer } from 'node:http';
import { describe, expect, it } from 'vitest';
import { createClient } from '../../src/client.js';
import { AuthServiceAccountsModule } from '../../src/generated/modules/auth-service-accounts.gen.js';

describe('Generated service-account certificate removal', () => {
  it.each([204, 403, 404])('uses the authenticated DELETE contract and preserves HTTP %i', async (status) => {
    const id = '7d74384b-804b-493a-a3d3-282d57e150d2';
    const observed: Array<{ method?: string; path?: string; authorization?: string; body: string }> = [];
    const server = createServer((request, response) => {
      let body = '';
      request.on('data', (chunk: Buffer) => {
        body += chunk.toString();
      });
      request.on('end', () => {
        observed.push({ method: request.method, path: request.url, authorization: request.headers.authorization, body });
        if (status === 204) response.writeHead(204).end();
        else
          response
            .writeHead(status, { 'Content-Type': 'application/problem+json' })
            .end(JSON.stringify({ status, title: status === 403 ? 'Forbidden' : 'Not found' }));
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
        auth: { getAccessToken: () => Promise.resolve('test-only') },
        devtools: { enabled: false },
      });

      const result = await new AuthServiceAccountsModule(client).deleteAuthServiceAccountsClientCertificate(id);

      expect(observed).toEqual([
        {
          method: 'DELETE',
          path: `/v1/auth/service-accounts/${id}/client-certificate`,
          authorization: 'Bearer test-only',
          body: '',
        },
      ]);
      expect(result.ok).toBe(status === 204);
      if (!result.ok) expect(result.error.status).toBe(status);
    } finally {
      await new Promise<void>((resolve, reject) => server.close((error) => (error ? reject(error) : resolve())));
    }
  });
});
