#!/usr/bin/env node
// Sequential dev orchestrator — cross-platform (Windows/macOS/Linux).
//
// Replaces the shell one-liner that raced dev:web against a 40s API boot:
//   1. docker compose up -d --wait
//   2. dev:api in background
//   3. poll the API health endpoint (fail fast if the API process dies)
//   4. only then start dev:web
//
// Ctrl+C tears down both children (taskkill /T on Windows, SIGTERM elsewhere).
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

// ── 2. API in background ─────────────────────────────────────────────────────
console.log('[dev] starting API...');
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

// ── 4. Web in foreground ─────────────────────────────────────────────────────
console.log('[dev] API up — starting web');
const web = run('pnpm', ['dev:web']);

const shutdown = () => {
  killTree(api);
  killTree(web);
  process.exit(0);
};
process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);

// When web exits (Ctrl+C on its foreground output), tear the API down too.
web.on('exit', (code) => {
  killTree(api);
  process.exit(code ?? 0);
});
