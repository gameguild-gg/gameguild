#!/usr/bin/env node
// @emception/cli bin entry.

import { runDoctor, formatReport } from '../doctor.js';

const cmd = process.argv[2] ?? 'help';

async function main(): Promise<number> {
  if (cmd === 'doctor') {
    const report = await runDoctor();
    console.log(formatReport(report));
    return report.ok ? 0 : 1;
  }
  if (cmd === 'help' || cmd === '--help' || cmd === '-h') {
    console.log('emception CLI');
    console.log('Commands:');
    console.log('  doctor       — check environment for emception prerequisites');
    console.log('  cdn-export   — Phase 9 (pending)');
    console.log('  run          — Phase 9 (pending)');
    console.log('  test         — Phase 9 (pending)');
    return 0;
  }
  console.error(`Unknown command: ${cmd}`);
  console.error(`Run 'emception help' for available commands.`);
  return 1;
}

main().then(
  (code) => process.exit(code),
  (err) => {
    console.error('emception: unhandled error:', err);
    process.exit(2);
  },
);
