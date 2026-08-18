#!/usr/bin/env node
// Dev orchestrator — cross-platform (Windows/macOS/Linux).
//
// 1. docker compose up -d --wait   (postgres/redis/garage/mailhog)
// 2. start api/web/client-watchers in parallel (see below)
//
// Ctrl+C tears down all children (taskkill /T on Windows, SIGTERM elsewhere).
import { spawn } from 'node:child_process';

// Windows: pnpm/docker are .cmd shims — need shell to resolve.
const shell = process.platform === 'win32';

function run(cmd, args) {
  return spawn(cmd, args, { shell, stdio: 'inherit' });
}

function killTree(child) {
  if (child.exitCode !== null) return;
  if (process.platform === 'win32') {
    spawn('taskkill', ['/pid', String(child.pid), '/T', '/F'], { shell: true });
  } else {
    child.kill('SIGTERM');
  }
}

// ── 1. docker compose ────────────────────────────────────────────────────────
const compose = run('docker', ['compose', '-f', 'compose.yaml', 'up', '-d', '--wait']);
const composeCode = await new Promise((resolve) => compose.on('exit', resolve));
if (composeCode !== 0) process.exit(composeCode ?? 1);

// ── 2. Everything in parallel ────────────────────────────────────────────────
// API, web, client generate:watch and build:watch all start immediately.
// generate:watch polls the spec every 10s and retries until the API is up,
// build:watch waits for regen output, web hot-reloads on client changes.
// (dev:web builds the client once first, so web never boots against a
// missing dist.) dev:api uses a polling file watcher — FSEvents-backed
// watchers crash dotnet watch on macOS 26 (see the dev:api script).
console.log('[dev] starting API, web, client generate:watch, client build:watch');
const api = run('pnpm', ['dev:api']);
const children = [
  run('pnpm', ['--filter', '@game-guild/client', 'run', 'generate:watch']),
  run('pnpm', ['--filter', '@game-guild/client', 'run', 'build:watch']),
  run('pnpm', ['dev:web']),
];
const [clientGen, clientBuild, web] = children;

const shutdown = () => {
  for (const child of [api, ...children]) killTree(child);
  process.exit(0);
};
process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);

// Watcher processes failing must not take down dev — log and continue.
for (const [name, child] of [
  ['client generate:watch', clientGen],
  ['client build:watch', clientBuild],
]) {
  child.on('exit', (code) => {
    if (code !== 0 && code !== null) console.error(`[dev] ${name} exited (code ${code}) — continuing without it`);
  });
}

// API dying means nothing else is useful — tear everything down.
api.on('exit', (code) => {
  if (code !== 0 && code !== null) console.error(`[dev] API exited (code ${code})`);
  shutdown();
});

// When web exits (Ctrl+C on its foreground output), tear everything down.
web.on('exit', (code) => {
  shutdown();
});
