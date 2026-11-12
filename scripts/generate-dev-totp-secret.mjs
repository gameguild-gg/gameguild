import { randomBytes } from 'node:crypto';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const BASE32_ALPHABET = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';

export function encodeBase32(bytes) {
  let bits = 0;
  let value = 0;
  let output = '';

  for (const byte of bytes) {
    value = (value << 8) | byte;
    bits += 8;

    while (bits >= 5) {
      output += BASE32_ALPHABET[(value >>> (bits - 5)) & 31];
      bits -= 5;
    }
  }

  if (bits > 0) {
    output += BASE32_ALPHABET[(value << (5 - bits)) & 31];
  }

  return output;
}

export function createTotpSetup({ secret = encodeBase32(randomBytes(20)), issuer = 'GameGuild', account = 'admin@game-guild.com' } = {}) {
  const label = `${encodeURIComponent(issuer)}:${encodeURIComponent(account)}`;
  const parameters = new URLSearchParams({
    secret,
    issuer,
    algorithm: 'SHA1',
    digits: '6',
    period: '30',
  });

  return {
    secret,
    environmentAssignment: `Seed__AdminTotpSecret=${secret}`,
    uri: `otpauth://totp/${label}?${parameters}`,
  };
}

export function main() {
  const setup = createTotpSetup();
  process.stdout.write(
    [
      'Generated a unique local TOTP factor for admin@game-guild.com.',
      'Add this line to the ignored root .env file:',
      '',
      setup.environmentAssignment,
      '',
      'Import this URI into your authenticator:',
      '',
      setup.uri,
      '',
      'Restart the API after updating .env. Never commit this value.',
      '',
    ].join('\n'),
  );
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main();
}
