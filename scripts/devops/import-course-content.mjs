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

import { randomUUID } from 'node:crypto';
import { appendFileSync, existsSync, mkdirSync, readFileSync } from 'node:fs';
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
// Ported from SnapshotCourseSeeder.cs (ParseCourseDefinition :240-278, ParseContentDefinition
// :429-476, ResolveContentBody :456-476, ResolveMarkdownPath :503-509, extractors :539-602,
// regexes :966-973). ONE deviation from the C#, documented at the object regexes: `export` is
// optional because intro2gpro/game-publishing declare contents as plain `const`.

// C# MarkdownImportRegex (:966). 'g' because .Matches() scans all; C# default has no
// Singleline/Multiline, and the pattern needs neither.
const MarkdownImportRegex = /import\s+(?<name>[A-Za-z0-9_]+)\s+from\s+['"](?<path>\.[^'"]+\.md)['"];/g;

// C# :969/:972 use Singleline|Multiline → JS 'gms' (s = dot-matches-newline for C# Singleline,
// m = ^ per-line for C# Multiline). Deviation: C# anchors on `export const`; the regexes here
// accept `(?:export\s+)?const` so plain-`const` courses (intro2gpro) also parse. Type
// annotations still gate strictly (`: Program = {` / `: ProgramContent = {`), so Product and
// ProductProgram objects and the `*.programContents = [...]` wiring blocks never match.
const ProgramObjectRegex = /(?:export\s+)?const\s+\w+Program:\s*Program\s*=\s*\{(?<body>.*?)^\};/gms;
const ProgramContentObjectRegex = /(?:export\s+)?const\s+\w+Content:\s*ProgramContent\s*=\s*\{(?<body>.*?)^\};/gms;

function escapeRegex(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// C# Regex.Unescape → JSON string unescape; on malformed input fall back to the raw text.
function unescapeString(raw) {
  try {
    return JSON.parse(`"${raw}"`);
  } catch {
    return raw;
  }
}

// C# extractors use RegexOptions.Singleline → JS 's' flag (dot matches newline). Field names
// are anchored with \b exactly as in the C# (`^\s*fieldName`-style anchors are NOT used).
function extractString(objectBody, fieldName) {
  const match = new RegExp(
    `\\b${escapeRegex(fieldName)}\\s*:\\s*(?<quote>['"])(?<value>(?:\\\\.|(?!\\k<quote>).)*)\\k<quote>`,
    's',
  ).exec(objectBody);
  return match ? unescapeString(match.groups.value) : null;
}

function extractNullableString(objectBody, fieldName) {
  const nullMatch = new RegExp(`\\b${escapeRegex(fieldName)}\\s*:\\s*null\\b`, 's').exec(objectBody);
  return nullMatch ? null : extractString(objectBody, fieldName);
}

function extractNullableInt(objectBody, fieldName) {
  const match = new RegExp(
    `\\b${escapeRegex(fieldName)}\\s*:\\s*(?<value>-?\\d+|null)\\b`,
    's',
  ).exec(objectBody);
  if (!match || match.groups.value === 'null') return null;
  return Number.parseInt(match.groups.value, 10);
}

function extractBoolean(objectBody, fieldName) {
  // C# adds IgnoreCase → 'i'.
  const match = new RegExp(
    `\\b${escapeRegex(fieldName)}\\s*:\\s*(?<value>true|false)\\b`,
    'si',
  ).exec(objectBody);
  return match ? match.groups.value.toLowerCase() === 'true' : null;
}

function extractIdentifier(objectBody, fieldName) {
  const match = new RegExp(
    `\\b${escapeRegex(fieldName)}\\s*:\\s*(?<value>[A-Za-z_][A-Za-z0-9_]*)\\b`,
    's',
  ).exec(objectBody);
  return match ? match.groups.value : null;
}

function extractTrailingComment(objectBody, fieldName) {
  const match = new RegExp(
    `\\b${escapeRegex(fieldName)}\\s*:\\s*[^\\r\\n,]+,\\s*//\\s*(?<comment>[^\\r\\n]+)`,
    's',
  ).exec(objectBody);
  return match ? match.groups.comment.trim() : null;
}

// C# NormalizeLabel (:956-964). Exported — task 3 (inference) consumes it.
export function normalizeLabel(value) {
  if (value === null || value === undefined || value.trim() === '') return '';
  return value.replace(/[^A-Za-z0-9]+/g, '').toLowerCase();
}

// C# ResolveMarkdownPath (:503-509): strip every "./" occurrence, swap '/' for the platform
// separator, then combine against the course dir.
function resolveMarkdownPath(courseDirectory, relativeMarkdownPath) {
  const normalized = relativeMarkdownPath.split('./').join('').split('/').join(path.sep);
  return path.resolve(courseDirectory, normalized);
}

function parseMarkdownImports(fileText) {
  const imports = new Map();
  for (const match of fileText.matchAll(MarkdownImportRegex)) {
    imports.set(match.groups.name, match.groups.path);
  }
  return imports;
}

function extractFirstProgramObject(fileText) {
  const match = fileText.matchAll(ProgramObjectRegex).next().value;
  if (!match) {
    throw new Error('Unable to locate the top-level Program export in the snapshot course definition.');
  }
  return match.groups.body;
}

function extractProgramContentObjects(fileText) {
  return [...fileText.matchAll(ProgramContentObjectRegex)].map((match) => match.groups.body);
}

function resolveContentBody(courseDirectory, markdownImports, contentBody, sourceId, bodyImportName) {
  if (bodyImportName !== null && bodyImportName.trim() !== '' && markdownImports.has(bodyImportName)) {
    return readFileSync(resolveMarkdownPath(courseDirectory, markdownImports.get(bodyImportName)), 'utf8');
  }

  const inlineBody = extractString(contentBody, 'body');
  if (inlineBody !== null) {
    return inlineBody;
  }

  throw new Error(`ProgramContent '${sourceId}' does not declare a markdown import body or inline string body.`);
}

function parseContentDefinition(courseDirectory, markdownImports, contentBody) {
  const sourceId = extractString(contentBody, 'id') ?? randomUUID().replaceAll('-', '');
  const title = extractString(contentBody, 'title') ?? sourceId;
  const description = extractString(contentBody, 'description') ?? '';
  const bodyImportName = extractIdentifier(contentBody, 'body');
  const body = resolveContentBody(courseDirectory, markdownImports, contentBody, sourceId, bodyImportName);
  const typeComment = extractTrailingComment(contentBody, 'type');
  // Only quoted parentId values match; `parentId: undefined` yields null.
  const parentSourceId = extractString(contentBody, 'parentId');

  return {
    sourceId,
    parentSourceId,
    title,
    description,
    rawType: extractNullableInt(contentBody, 'type'),
    typeComment,
    bodyImportName,
    bodyResolvedFromFile:
      bodyImportName !== null && bodyImportName.trim() !== '' && markdownImports.has(bodyImportName),
    body,
    sortOrder: extractNullableInt(contentBody, 'sortOrder') ?? 0,
    isRequired: extractBoolean(contentBody, 'isRequired') ?? true,
    estimatedMinutes: extractNullableInt(contentBody, 'estimatedMinutes'),
  };
}

function parseCourse(courseDirectory) {
  const indexFilePath = path.join(courseDirectory, 'index.ts');
  if (!existsSync(indexFilePath)) {
    throw new Error(`Course definition file not found: ${indexFilePath}`);
  }

  const fileText = readFileSync(indexFilePath, 'utf8');
  const markdownImports = parseMarkdownImports(fileText);
  const programBody = extractFirstProgramObject(fileText);

  const slug = extractString(programBody, 'slug') ?? path.basename(courseDirectory);
  const program = {
    slug,
    title: extractString(programBody, 'title') ?? slug,
    description: extractString(programBody, 'description') ?? '',
    thumbnail: extractNullableString(programBody, 'thumbnail'),
    estimatedHours: extractNullableInt(programBody, 'estimatedHours'),
    rawCategory: extractNullableInt(programBody, 'category'),
    rawDifficulty: extractNullableInt(programBody, 'difficulty'),
    rawEnrollmentStatus: extractNullableInt(programBody, 'enrollmentStatus'),
  };

  const contents = extractProgramContentObjects(fileText).map((contentBody) =>
    parseContentDefinition(courseDirectory, markdownImports, contentBody),
  );

  return { program, contents };
}

// Test-only: GG_COURSES_ROOT lets --self-check (and negative tests) point at a scratch courses
// root (e.g. /tmp) without touching the repo default. Read at call time; not part of the
// import flow contract.
function parseCourseDir(slug) {
  const root = process.env.GG_COURSES_ROOT || COURSES_ROOT;
  return parseCourse(path.join(root, slug));
}

// ===== self-check (tasks 2-3) =====

function assertSelfCheck(name, ok, detail) {
  if (ok) {
    console.log(`[self-check] ok ${name}`);
  } else {
    console.log(`[self-check] FAIL ${name}: ${detail}`);
    process.exit(1);
  }
}

function runSelfCheck(args) {
  let parsed;
  try {
    parsed = parseCourseDir(args.slug);
  } catch (err) {
    console.log(`[self-check] FAIL parse: ${err.message}`);
    process.exit(1);
  }
  const { program, contents } = parsed;

  const fromFile = contents.filter((content) => content.bodyResolvedFromFile);
  const inline = contents.filter((content) => !content.bodyResolvedFromFile);

  for (const content of contents) {
    console.log(
      `[self-check] parsed ${content.sourceId} body=${content.bodyResolvedFromFile ? 'file' : 'inline'}` +
        ` type=${content.rawType} parent=${content.parentSourceId ?? 'null'}`,
    );
  }

  assertSelfCheck(
    'program',
    program.slug === 'intro2gpro' && program.title === 'Introduction to Game Programming',
    `slug=${program.slug} title=${JSON.stringify(program.title)}`,
  );
  assertSelfCheck('contents-16', contents.length === 16, `contents.length=${contents.length}`);
  assertSelfCheck(
    'body-kinds',
    fromFile.length === 13 && inline.length === 3,
    `file=${fromFile.length} inline=${inline.length}`,
  );
  assertSelfCheck(
    'md-bodies-nonempty',
    fromFile.every((content) => content.body.length > 0),
    `empty=${fromFile.filter((content) => content.body.length === 0).map((content) => content.sourceId).join(',') || 'none'}`,
  );
}

// ===== api client (task 4) =====

// ===== upsert (task 5-7) =====

// ===== orchestration (task 8) =====

function main() {
  const args = parseArgs(process.argv.slice(2));

  const courseDir = path.join(COURSES_ROOT, args.slug);
  const indexPath = path.join(courseDir, 'index.ts');
  // Self-check resolves the course dir itself (parseCourseDir, GG_COURSES_ROOT-aware) and
  // reports a missing index.ts as a self-check failure instead of an early exit.
  if (args.mode !== 'selfcheck' && !existsSync(indexPath)) fail(`index.ts not found: ${indexPath}`);

  const env = resolveEnv();
  validateEnv(env, args.mode);

  initEvidence(args.mode);

  console.log(`[boot] mode=${args.mode} slug=${args.slug} courseDir=${courseDir} api=${env.apiUrl}`);
  logEvent({ type: 'boot', mode: args.mode, slug: args.slug, args: { ...args, mode: undefined } });

  if (args.mode === 'selfcheck') {
    runSelfCheck(args);
    process.exit(0);
  }

  // Creds are read but only consumed by the API client (task 4); masked here
  // so the value never reaches stdout or evidence logs.
  logEvent({ type: 'creds', email: env.email, password: maskSecret(env.password) });

  console.log('[todo] scaffold complete — parser/API/import stages land in tasks 2-8');
  process.exit(0);
}

main();
