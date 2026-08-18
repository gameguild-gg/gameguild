#!/usr/bin/env node
// Dev orchestrator — cross-platform (Windows/macOS/Linux).
//
//   1. docker compose up -d --wait   (postgres/redis/garage/mailhog)
//   2. API via `dotnet watch`        (hot reload; Database/Migrations excluded
//                                     in GameGuild.API.csproj — editing a
//                                     migration never restarts the API, EF
//                                     migrations only run on full restart)
//   3. poll the API health endpoint  (fail fast if the API process dies)
//   4. in parallel:
//        - client generate:watch  (polls swagger.json, regenerates ONLY when
//                                  the spec hash changes)
//        - client build:watch     (tsup rebuilds dist so the running web picks
//                                  up regenerated client code)
//        - web dev                (Next.js watch mode)
//
// Ctrl+C tears down all children (taskkill /T on Windows, SIGTERM elsewhere).
import { spawn } from 'node:child_process';

const API_HEALTH_URL = process.env.DEV_API_HEALTH_URL ?? 'http://localhost:8080/health';
const API_TIMEOUT_MS = Number(process.env.DEV_API_TIMEOUT_MS ?? 180_000);

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

async function waitForApi(api) {
  const deadline = Date.now() + API_TIMEOUT_MS;
  while (Date.now() < deadline) {
    if (api.exitCode !== null) {
      throw new Error(`API process exited early (code ${api.exitCode})`);
    }
    try {
      const res = await fetch(API_HEALTH_URL);
      if (res.ok) return;
    } catch {
      /* not up yet */
    }
    await new Promise((r) => setTimeout(r, 2000));
  }
  throw new Error(`Timed out waiting for ${API_HEALTH_URL}`);
}

// ── 1. docker compose ────────────────────────────────────────────────────────
const compose = run('docker', ['compose', '-f', 'compose.yaml', 'up', '-d', '--wait']);
const composeCode = await new Promise((resolve) => compose.on('exit', resolve));
if (composeCode !== 0) process.exit(composeCode ?? 1);

// ── 2. API in background (dotnet watch) ──────────────────────────────────────
console.log('[dev] starting API (dotnet watch)...');
const api = run('pnpm', ['dev:api']);

// ── 3. Wait for health ───────────────────────────────────────────────────────
console.log(`[dev] waiting for API (${API_HEALTH_URL})...`);
try {
  await waitForApi(api);
} catch (err) {
  console.error(`[dev] ${err.message}`);
  killTree(api);
  process.exit(1);
}

// ── 4. Web + client regeneration in parallel ─────────────────────────────────
console.log('[dev] API up — starting web, client generate:watch, client build:watch');
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

// When web exits (Ctrl+C on its foreground output), tear everything down.
web.on('exit', (code) => {
  shutdown();
});
