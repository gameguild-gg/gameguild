#!/usr/bin/env node
/**
 * emception meta-package bin shim.
 * Delegates to the full @emception/cli entry point so that both
 *   npx @emception/cli doctor
 *   npx emception doctor
 * work identically.
 */
import { createRequire } from 'node:module';

const req = createRequire(import.meta.url);
let binPath;
try {
  binPath = req.resolve('@emception/cli/dist/bin/emception.js');
} catch {
  console.error(
    'emception: @emception/cli is not installed.\n' +
      'Install it: npm install @emception/cli\n' +
      'or run the CLI directly: npx @emception/cli ' +
      (process.argv[2] ?? 'help'),
  );
  process.exit(1);
}

await import(binPath);
