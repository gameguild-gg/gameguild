#!/usr/bin/env node
// Import course content from a local course folder into the GameGuild API.
//
// Usage:
//   node scripts/devops/import-course-content.mjs --slug <folder> [flags]
//
// Flags:
//   --slug <folder>         course folder under apps/web/src/data/courses (required)
//   --dry-run               default mode: plan only, no writes
//   --execute               write mode: upsert metadata + content via REST
//   --prune-stale           delete content whose slug is not in the imported set
//                           (only meaningful with --execute; dry-run lists candidates)
//   --skip-source-ids <csv> comma-separated sourceIds to exclude from the import
//   --self-check            offline parse-only assertions; no creds, no HTTP
//
// Env:
//   GG_API_URL      API base (default https://api.gameguild.gg)
//   GG_API_EMAIL    admin email (fallback GG_USER_EMAIL)
//   GG_API_PASSWORD admin password (fallback GG_USER_PASSWORD) — never printed
//
// Evidence: every HTTP call is appended as one JSONL line
// {ts,method,url,status,ok} to .omo/evidence/import-course-content/<mode>-<runTs>.jsonl

import { appendFileSync, existsSync, mkdirSync } from 'node:fs';
import path from 'node:path';

const REPO_ROOT = path.resolve(import.meta.dirname, '..', '..');
const COURSES_ROOT = path.join(REPO_ROOT, 'apps/web/src/data/courses');
const EVIDENCE_DIR = path.join(REPO_ROOT, '.omo/evidence/import-course-content');
const DEFAULT_API_URL = 'https://api.gameguild.gg';

function fail(message) {
  console.error(`[error] ${message}`);
  process.exit(1);
}

// ===== cli =====

function parseArgs(argv) {
  const args = {
    slug: null,
    dryRun: false,
    execute: false,
    pruneStale: false,
    skipSourceIds: [],
    selfCheck: false,
  };

  for (let i = 0; i < argv.length; i += 1) {
    const flag = argv[i];
    switch (flag) {
      case '--slug': {
        const value = argv[i + 1];
        if (!value || value.startsWith('--')) fail('--slug requires a value');
        args.slug = value;
        i += 1;
        break;
      }
      case '--dry-run':
        args.dryRun = true;
        break;
      case '--execute':
        args.execute = true;
        break;
      case '--prune-stale':
        args.pruneStale = true;
        break;
      case '--skip-source-ids': {
        const value = argv[i + 1];
        if (!value || value.startsWith('--')) fail('--skip-source-ids requires a csv value');
        const ids = value.split(',').map((id) => id.trim()).filter(Boolean);
        for (const id of ids) {
          if (!/^[A-Za-z0-9][A-Za-z0-9_-]*$/.test(id)) {
            fail(`--skip-source-ids: invalid identifier "${id}"`);
          }
        }
        args.skipSourceIds = ids;
        i += 1;
        break;
      }
      case '--self-check':
        args.selfCheck = true;
        break;
      default:
        fail(`unknown flag: ${flag}`);
    }
  }

  if (!args.slug) fail('slug required');
  if (args.execute && args.selfCheck) fail('--execute and --self-check are mutually exclusive');

  if (args.selfCheck) args.mode = 'selfcheck';
  else if (args.execute) args.mode = 'execute';
  else args.mode = 'dryrun';

  return args;
}

// ===== env =====

function resolveEnv() {
  return {
    apiUrl: process.env.GG_API_URL || DEFAULT_API_URL,
    email: process.env.GG_API_EMAIL || process.env.GG_USER_EMAIL || null,
    password: process.env.GG_API_PASSWORD || process.env.GG_USER_PASSWORD || null,
  };
}

function validateEnv(env, mode) {
  if (mode === 'selfcheck') return;
  const missing = [];
  if (!env.email) missing.push('GG_API_EMAIL (or GG_USER_EMAIL)');
  if (!env.password) missing.push('GG_API_PASSWORD (or GG_USER_PASSWORD)');
  if (missing.length > 0) fail(`missing env: ${missing.join(', ')}`);
}

function maskSecret(value) {
  return value ? '***' : '(unset)';
}

// ===== evidence logger =====

const runTs = new Date().toISOString().replace(/[:.-]/g, '');
let evidenceFile = null;

function initEvidence(mode) {
  mkdirSync(EVIDENCE_DIR, { recursive: true });
  evidenceFile = path.join(EVIDENCE_DIR, `${mode}-${runTs}.jsonl`);
}

function appendEvidenceLine(obj) {
  if (!evidenceFile) return;
  appendFileSync(evidenceFile, `${JSON.stringify(obj)}\n`);
}

export function logHttp({ method, url, status, ok }) {
  appendEvidenceLine({ ts: new Date().toISOString(), method, url, status, ok });
}

export function logEvent(obj) {
  appendEvidenceLine({ ts: new Date().toISOString(), event: obj });
}

// ===== parser (task 2-3) =====

// ===== api client (task 4) =====

// ===== upsert (task 5-7) =====

// ===== orchestration (task 8) =====

function main() {
  const args = parseArgs(process.argv.slice(2));

  const courseDir = path.join(COURSES_ROOT, args.slug);
  const indexPath = path.join(courseDir, 'index.ts');
  if (!existsSync(indexPath)) fail(`index.ts not found: ${indexPath}`);

  const env = resolveEnv();
  validateEnv(env, args.mode);

  initEvidence(args.mode);

  console.log(`[boot] mode=${args.mode} slug=${args.slug} courseDir=${courseDir} api=${env.apiUrl}`);
  logEvent({ type: 'boot', mode: args.mode, slug: args.slug, args: { ...args, mode: undefined } });

  if (args.mode === 'selfcheck') {
    console.log('[self-check] scaffold-only (assertions arrive with parser tasks)');
    process.exit(0);
  }

  // Creds are read but only consumed by the API client (task 4); masked here
  // so the value never reaches stdout or evidence logs.
  logEvent({ type: 'creds', email: env.email, password: maskSecret(env.password) });

  console.log('[todo] scaffold complete — parser/API/import stages land in tasks 2-8');
  process.exit(0);
}

main();
