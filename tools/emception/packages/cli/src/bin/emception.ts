#!/usr/bin/env node
// @emception/cli bin entry. Phase 9 implementation.

const cmd = process.argv[2] ?? 'help';

if (cmd === 'doctor' || cmd === 'help' || cmd === '--help' || cmd === '-h') {
  console.log('emception CLI — Phase 9 pending.');
  console.log('Commands: doctor, cdn-export, run, test');
  process.exit(0);
}

console.error(`Unknown command: ${cmd}`);
process.exit(1);
