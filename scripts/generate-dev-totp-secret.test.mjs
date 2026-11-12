import assert from 'node:assert/strict';
import test from 'node:test';

import { createTotpSetup, encodeBase32 } from './generate-dev-totp-secret.mjs';

test('encodeBase32 emits RFC 4648 output without padding', () => {
  assert.equal(encodeBase32(Buffer.from('foo', 'ascii')), 'MZXW6');
});

test('createTotpSetup emits the local seed assignment and authenticator URI', () => {
  const setup = createTotpSetup({ secret: 'MZXW6' });

  assert.equal(setup.environmentAssignment, 'Seed__AdminTotpSecret=MZXW6');
  assert.equal(setup.uri, 'otpauth://totp/GameGuild:admin%40game-guild.com?secret=MZXW6&issuer=GameGuild&algorithm=SHA1&digits=6&period=30');
});
