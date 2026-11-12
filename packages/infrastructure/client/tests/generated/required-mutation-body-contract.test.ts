import { once } from 'node:events';
import { createServer } from 'node:http';
import { describe, expect, it } from 'vitest';
import { createClient } from '../../src/client.js';
import { CommercePaymentsModule } from '../../src/generated/modules/commerce-payments.gen.js';
import { CommercePaymentsWalletsModule } from '../../src/generated/modules/commerce-payments-wallets.gen.js';
import { UsersModule } from '../../src/generated/modules/users.gen.js';
import { AuthTrustedDevicesModule } from '../../src/generated/modules/auth-trusted-devices.gen.js';

describe('generated mandatory mutation bodies', () => {
  it.each(['payment', 'wallet-transfer', 'user', 'trusted-device'] as const)(
    'rejects a missing %s payload before HTTP dispatch', async operation => {
      let requests = 0;
      const server = createServer((_request, response) => {
        requests++;
        response.writeHead(400, { 'Content-Type': 'application/json' });
        response.end(JSON.stringify({ title: 'Synthetic remote rejection', status: 400 }));
      });
      const listening = once(server, 'listening');
      server.listen(0, '127.0.0.1');
      await listening;
      try {
        const address = server.address();
        if (!address || typeof address === 'string') throw new Error('Missing local test port');
        const client = createClient({ baseUrl: `http://127.0.0.1:${address.port}`,
          auth: { getAccessToken: async () => 'test-only-required-body' }, devtools: { enabled: false } });
        // Deliberately bypass TypeScript to cover JavaScript callers at runtime.
        const calls = {
          payment: () => new CommercePaymentsModule(client).postPayments(undefined as never),
          'wallet-transfer': () => new CommercePaymentsWalletsModule(client).postWalletsTransfer(undefined as never),
          user: () => new UsersModule(client).postUsers(undefined as never),
          'trusted-device': () => new AuthTrustedDevicesModule(client).postAuthTrustedDevices(undefined as never),
        };
        await expect(calls[operation]()).rejects.toMatchObject({ code: 'VALIDATION_ERROR' });
        expect(requests).toBe(0);
      } finally {
        await new Promise<void>((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
      }
    },
  );
});
