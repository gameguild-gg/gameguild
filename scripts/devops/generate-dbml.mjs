#!/usr/bin/env node

import { spawnSync } from 'node:child_process';
import { existsSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

export function parseArgs(argv) {
  if (argv.includes('--help')) {
    return { mode: 'help' };
  }
  if (argv.includes('--verify-db')) {
    return { mode: 'verify-db' };
  }
  if (argv.includes('--check')) {
    return { mode: 'check' };
  }
  return { mode: 'generate' };
}

// `schemas` narrows live introspection (db2dbml `?schemas=a,b,c`). The model
// physically spans 7 schemas (public + gameguild.authentication, assets,
// gameguild.resources, resources, gameguild.sla, auth — 32 of 320 tables),
// so --verify-db passes the model-derived schema set; every other caller
// keeps the public-only default.
export function envToDsn(env, schemas = ['public']) {
  const database = env.POSTGRES_DB;
  if (!database) {
    throw new Error('POSTGRES_DB is required to build the database DSN (see .env.example)');
  }
  const schemaList = schemas.map((schema) => encodeURIComponent(schema)).join(',');
  return `postgresql://${env.POSTGRES_USER}:${encodeURIComponent(env.POSTGRES_PASSWORD)}@${
    env.POSTGRES_HOST ?? 'localhost'
  }:${env.POSTGRES_PORT ?? '5432'}/${database}?schemas=${schemaList}`;
}

// Dollar-quote tags per PostgreSQL rules: `$$`, `$roles$`, `$func$`, `$tag_name$`.
// Masking must run BEFORE the `;` split so semicolons inside routine bodies never
// split a statement (this repo's migrations embed DO $roles$ and CREATE FUNCTION
// ... AS $$ blocks).
const DOLLAR_TAG = /\$\w*\$/g;
const MASKED_BODY = '/* dollar-quoted body masked */';

function maskDollarQuotedBodies(sql) {
  let masked = '';
  let cursor = 0;
  for (let match = DOLLAR_TAG.exec(sql); match !== null; match = DOLLAR_TAG.exec(sql)) {
    const closing = sql.indexOf(match[0], match.index + match[0].length);
    if (closing === -1) {
      continue;
    }
    masked += sql.slice(cursor, match.index) + MASKED_BODY;
    cursor = closing + match[0].length;
    DOLLAR_TAG.lastIndex = cursor;
  }
  return masked + sql.slice(cursor);
}

// Table-shape DDL whitelist. `IF NOT EXISTS` / `IF EXISTS` between keyword and
// object is tolerated because only the leading keywords are matched.
const TABLE_SHAPE_DDL =
  /^(?:CREATE\s+TABLE|CREATE\s+UNIQUE\s+INDEX|CREATE\s+INDEX|CREATE\s+TYPE|ALTER\s+TABLE|COMMENT\s+ON)\b/i;

function firstNonComment(statement) {
  let rest = statement;
  for (;;) {
    rest = rest.replace(/^\s+/, '');
    if (rest.startsWith('--')) {
      const newline = rest.indexOf('\n');
      if (newline === -1) {
        return '';
      }
      rest = rest.slice(newline + 1);
      continue;
    }
    if (rest.startsWith('/*')) {
      const end = rest.indexOf('*/');
      if (end === -1) {
        return '';
      }
      rest = rest.slice(end + 2);
      continue;
    }
    return rest;
  }
}

export function stripNonDdlStatements(sql) {
  return maskDollarQuotedBodies(sql)
    .split(';')
    .map((statement) => statement.trim())
    .filter((statement) => statement.length > 0 && TABLE_SHAPE_DDL.test(firstNonComment(statement)))
    .map((statement) => `${statement};\n`)
    .join('');
}

export function buildDbmlHeader() {
  return `${[
    '// MACHINE-GENERATED from the EF Core model via `dotnet ef dbcontext script` + sql2dbml.',
    '// DO NOT EDIT by hand — regenerate with: pnpm db:dbml',
    '// This file reflects the actual EF Core model (no native Postgres enums; enum-like columns are text).',
    '// Hand-curated design documentation (enums, business-rule notes): docs/program.dbml',
  ].join('\n')}\n`;
}

export function collectTables(dbmlDatabase) {
  const tables = (dbmlDatabase.schemas ?? []).flatMap((schema) => schema.tables ?? []);
  if (tables.length === 0) {
    throw new Error('collectTables: no tables found in the parsed DBML database');
  }
  return tables;
}

export function normalizeName(name) {
  const unquoted = name.replace(/^["`]+/, '').replace(/["`]+$/, '');
  const lowered = unquoted.toLowerCase();
  return lowered.replace(/^public\./, '');
}

export function stripByteOrderMark(text) {
  return text.charCodeAt(0) === 0xfeff ? text.slice(1) : text;
}

// sql2dbml can exit 0 on partially parsed input, so the DBML content itself is
// the truth probe: at least one table, and no migration-history table leaked in
// (EF's `migrations script` output ends with INSERT INTO "__EFMigrationsHistory").
export function validateDbmlOutput(dbml) {
  const tableCount = (dbml.match(/^Table /gm) ?? []).length;
  if (tableCount < 1) {
    return { ok: false, reason: 'no Table blocks found in the converted DBML' };
  }
  if (dbml.toLowerCase().includes('__efmigrationshistory')) {
    return { ok: false, reason: '__EFMigrationsHistory leaked into the converted DBML' };
  }
  return { ok: true, tableCount };
}

// Zip-style line comparison (not an LCS diff): machine-generated DBML drifts
// in whole regions, so paired lines locate divergences cheaply. `null` marks a
// line that does not exist on that side (one string is a strict line-prefix
// of the other).
function divergentLines(actual, expected, limit) {
  const actualLines = actual.split('\n');
  const expectedLines = expected.split('\n');
  const divergences = [];
  const lineCount = Math.max(actualLines.length, expectedLines.length);
  for (let index = 0; index < lineCount && divergences.length < limit; index += 1) {
    if (actualLines[index] !== expectedLines[index]) {
      divergences.push({
        line: index + 1,
        actual: actualLines[index] ?? null,
        expected: expectedLines[index] ?? null,
      });
    }
  }
  return divergences;
}

// Byte equality on the whole string; when they differ, report the FIRST
// divergent line (1-based) with both line contents. Generate is deterministic
// (T3 double-run proof), so string equality is the staleness contract —
// never timestamps.
export function compareDbml(actual, expected) {
  if (actual === expected) {
    return { ok: true };
  }
  const [firstDivergence] = divergentLines(actual, expected, 1);
  return { ok: false, firstDivergence };
}

const VERIFY_ALLOWLIST = ['__efmigrationshistory'];

// Pure drift classifier for --verify-db. Inputs are pre-normalized table
// descriptors ({ name, columns }); __EFMigrationsHistory (created by EF at
// migrate time, never part of the model) is allowlisted HERE so no caller can
// forget it. All output arrays are sorted for deterministic reporting.
export function computeDrift(modelTables, liveTables) {
  const modelByName = new Map(modelTables.map((table) => [table.name, table]));
  const liveByName = new Map(liveTables.map((table) => [table.name, table]));

  const danglingTables = [...liveByName.keys()]
    .filter((name) => !modelByName.has(name) && !VERIFY_ALLOWLIST.includes(name))
    .sort();

  const unmigratedTables = [...modelByName.keys()]
    .filter((name) => !liveByName.has(name))
    .sort();

  const danglingColumns = [];
  for (const [name, model] of modelByName) {
    const live = liveByName.get(name);
    if (live === undefined) {
      continue;
    }
    const modelColumns = new Set(model.columns);
    for (const column of live.columns) {
      if (!modelColumns.has(column)) {
        danglingColumns.push(`${name}.${column}`);
      }
    }
  }

  return { danglingTables, danglingColumns: danglingColumns.sort(), unmigratedTables };
}

// Pure exit-code contract for --verify-db (mirrors checkVerdict): any drift
// class > 0 fails; only a fully clean comparison exits 0.
export function verifyDbVerdict(drift) {
  const driftCount =
    drift.danglingTables.length + drift.danglingColumns.length + drift.unmigratedTables.length;
  if (driftCount > 0) {
    return { exitCode: 1 };
  }
  return { exitCode: 0, message: 'OK: live database matches the EF Core model.' };
}

// The DSN embeds the password; tool stderr that echoes it must be scrubbed
// before printing. The DSN goes first (it contains the encoded password),
// then each password form standalone.
export function redactSecrets(text, secrets) {
  let redacted = text;
  for (const secret of secrets) {
    if (secret) {
      redacted = redacted.split(secret).join('<redacted>');
    }
  }
  return redacted;
}

// @dbml/cli's emitters (sql2dbml, db2dbml) produce two constructs that
// @dbml/core's parser rejects: optional-cardinality markers in Ref operators
// (`?<?`, `<?`) and table-level `Checks { ... }` blocks. Neither carries
// table/column identity (drift lives in tables and columns only), so Ref `?`
// markers are folded out line-scoped and Checks blocks are dropped with
// brace-depth tracking. Grammar quirk verified against the 320-table model
// DBML: raw input fails BOTH 'dbmlv2' and 'dbml' with 470 diagnostics.
export function prepareDbmlForCore(dbml) {
  const kept = [];
  let checksDepth = 0;
  for (const line of dbml.split('\n')) {
    if (checksDepth > 0) {
      checksDepth += (line.match(/\{/g) ?? []).length;
      checksDepth -= (line.match(/\}/g) ?? []).length;
      continue;
    }
    if (/^\s*Checks\s*\{\s*$/.test(line)) {
      checksDepth = 1;
      continue;
    }
    kept.push(line.trimStart().startsWith('Ref') ? line.replace(/\?/g, '') : line);
  }
  return kept.join('\n');
}

const REPO_ROOT = fileURLToPath(new URL('../..', import.meta.url));
const API_PROJECT = 'apps/api/Source/GameGuild.API/GameGuild.API.csproj';
const MODEL_SQL_PATH = path.join(tmpdir(), 'gg-model.sql');
const FILTERED_SQL_PATH = path.join(tmpdir(), 'gg-model-filtered.sql');
const MODEL_DBML_PATH = path.join(tmpdir(), 'gg-model.dbml');
const LIVE_DBML_PATH = path.join(tmpdir(), 'gg-live.dbml');
const SPAWN_OPTS = { encoding: 'utf8', cwd: REPO_ROOT, maxBuffer: 16 * 1024 * 1024 };

function runDotnet(args) {
  const result = spawnSync('dotnet', args, SPAWN_OPTS);
  if (result.error?.code === 'ENOENT') {
    throw new Error('dotnet not found — install .NET 9 SDK');
  }
  if (result.error) {
    throw new Error(`failed to start dotnet ${args.join(' ')}: ${result.error.message}`);
  }
  return result;
}

function runSql2dbml(sqlPath) {
  const result = spawnSync(
    'corepack',
    ['pnpm', 'exec', 'sql2dbml', sqlPath, '--postgres', '-o', MODEL_DBML_PATH],
    SPAWN_OPTS,
  );
  // sql2dbml drops a dbml-error.log into its cwd on every run (empty on
  // success) — remove it so generate runs never litter the repo root.
  rmSync(path.join(REPO_ROOT, 'dbml-error.log'), { force: true });
  if (result.error?.code === 'ENOENT') {
    throw new Error('corepack not found — it ships with Node.js >= 20');
  }
  if (result.error) {
    throw new Error(`failed to start sql2dbml: ${result.error.message}`);
  }
  return result;
}

function convertWithSql2dbml(sqlPath) {
  const run = runSql2dbml(sqlPath);
  if (run.status !== 0) {
    return { ok: false, detail: `sql2dbml exited ${run.status}:\n${run.stderr || run.stdout}` };
  }
  if (!existsSync(MODEL_DBML_PATH)) {
    return { ok: false, detail: `sql2dbml exited 0 but did not write ${MODEL_DBML_PATH}` };
  }
  const dbml = readFileSync(MODEL_DBML_PATH, 'utf8');
  const check = validateDbmlOutput(dbml);
  return check.ok ? { ...check, dbml } : { ok: false, detail: `validation failed: ${check.reason}` };
}

// DDL source: `dotnet ef dbcontext script` scripts the CURRENT model, bypassing
// migrations. The todo-3 spike proved the cumulative `migrations script` output
// is unusable here: sql2dbml chokes on a migration-authored semicolon-less
// UPDATE and on drop/recreate cycles (duplicate CREATE TABLE), and it silently
// DROPS `ALTER TABLE ADD COLUMN` — so a migrations-derived DBML would miss
// columns and keep dead tables. The dbcontext script needs no running database
// thanks to DesignTimeDbContextFactory.
export function runGeneratePipeline(outPath) {
  const restore = runDotnet(['tool', 'restore']);
  if (restore.status !== 0) {
    throw new Error(`dotnet tool restore failed:\n${restore.stderr || restore.stdout}`);
  }

  let hadPendingChanges = false;
  const pending = runDotnet([
    'ef',
    'migrations',
    'has-pending-model-changes',
    '--project',
    API_PROJECT,
    '--startup-project',
    API_PROJECT,
  ]);
  if (pending.status !== 0) {
    const output = `${pending.stdout}\n${pending.stderr}`;
    // Exit 1 WITH the pending-changes message = model differs from migrations
    // (generate never blocks on that). Any other non-zero exit is a build or
    // tool failure and must never be misreported as pending changes.
    if (/changes have been detected/i.test(output)) {
      hadPendingChanges = true;
      console.warn(
        'WARN: EF model has pending changes without a migration; docs/schema.dbml reflects the working model (dbcontext script), not the committed migrations',
      );
    } else {
      throw new Error(`dotnet ef migrations has-pending-model-changes failed:\n${output}`);
    }
  }

  const script = runDotnet([
    'ef',
    'dbcontext',
    'script',
    '-o',
    MODEL_SQL_PATH,
    '--project',
    API_PROJECT,
    '--startup-project',
    API_PROJECT,
  ]);
  if (script.status !== 0) {
    throw new Error(`dotnet ef dbcontext script failed:\n${script.stderr || script.stdout}`);
  }
  // dotnet-ef writes UTF-8 with a BOM; sql2dbml's parser rejects it at line 1
  // column 0 (spike-verified), so strip it before converting.
  let sql = stripByteOrderMark(readFileSync(MODEL_SQL_PATH, 'utf8'));
  if (sql.trim().length === 0) {
    throw new Error(`dotnet ef dbcontext script produced an empty file: ${MODEL_SQL_PATH}`);
  }
  writeFileSync(MODEL_SQL_PATH, sql);

  let conversion = convertWithSql2dbml(MODEL_SQL_PATH);
  if (!conversion.ok) {
    // Fallback: reduce to table-shape DDL and retry once. Kept for robustness —
    // the raw dbcontext script parsed cleanly in the spike (least transformation).
    const rawFailure = conversion.detail;
    writeFileSync(FILTERED_SQL_PATH, stripNonDdlStatements(sql));
    conversion = convertWithSql2dbml(FILTERED_SQL_PATH);
    if (!conversion.ok) {
      throw new Error(
        [
          'sql2dbml failed on the raw model DDL and again on the filtered table-shape DDL.',
          `Raw failure: ${rawFailure}`,
          `Filtered failure: ${conversion.detail}`,
          `Files kept for debugging: ${MODEL_SQL_PATH} and ${FILTERED_SQL_PATH}`,
        ].join('\n'),
      );
    }
  }

  const destination = path.resolve(REPO_ROOT, outPath);
  writeFileSync(destination, `${buildDbmlHeader()}\n${conversion.dbml.replace(/\s+$/, '\n')}`);
  const enumCount = (conversion.dbml.match(/^Enum /gm) ?? []).length;
  return { outPath: destination, tableCount: conversion.tableCount, enumCount, hadPendingChanges };
}

// `node -e` leaves process.argv[1] undefined; pathToFileURL(undefined) would throw.
const entryHref = process.argv[1] ? pathToFileURL(process.argv[1]).href : null;

const DIVERGENCE_SUMMARY_LIMIT = 20;

function displayDbmlLine(line) {
  if (line === null) {
    return '(no such line)';
  }
  return line === '' ? '(empty line)' : line;
}

function reportDivergences(regenerated, committed) {
  const divergences = divergentLines(regenerated, committed, DIVERGENCE_SUMMARY_LIMIT);
  const [first] = divergences;
  console.error(`first divergence at line ${first.line}:`);
  console.error(`  regenerated: ${displayDbmlLine(first.actual)}`);
  console.error(`  committed:   ${displayDbmlLine(first.expected)}`);
  if (divergences.length > 1) {
    console.error(`divergent lines (zip comparison, first ${DIVERGENCE_SUMMARY_LIMIT} shown):`);
    for (const divergence of divergences.slice(1)) {
      console.error(
        `  line ${divergence.line}: regenerated: ${displayDbmlLine(divergence.actual)} | committed: ${displayDbmlLine(divergence.expected)}`,
      );
    }
  }
}

// Pure decision core for --check so the guard ORDER (pending changes before
// missing file before staleness) stays unit-testable without dotnet.
export function checkVerdict({ hadPendingChanges, committedExists, comparison, tableCount }) {
  if (hadPendingChanges) {
    return {
      exitCode: 1,
      message:
        'FAIL: EF model has pending changes without a migration. Commit the migration, then regenerate: pnpm db:dbml',
    };
  }
  if (!committedExists) {
    return {
      exitCode: 1,
      message: 'FAIL: docs/schema.dbml not found — run pnpm db:dbml and commit it',
    };
  }
  if (!comparison.ok) {
    return {
      exitCode: 1,
      message: 'FAIL: docs/schema.dbml is stale. Regenerate: pnpm db:dbml',
      firstDivergence: comparison.firstDivergence,
    };
  }
  return {
    exitCode: 0,
    message: `OK: docs/schema.dbml matches the EF Core model (${tableCount} tables.)`,
  };
}

// --check: staleness guard for CI. Regenerates ONLY into the tmpdir (never
// auto-fixes, never writes docs/) and byte-compares against the committed file.
function runCheckMode() {
  let summary;
  try {
    summary = runGeneratePipeline(path.join(tmpdir(), 'gg-check.dbml'));
  } catch (error) {
    // Pipeline failures (build/tool) must never be misreported as pending changes.
    console.error(`generate-dbml: ${error.message}`);
    process.exit(1);
  }

  const regenerated = readFileSync(summary.outPath, 'utf8');
  const committedPath = path.join(REPO_ROOT, 'docs/schema.dbml');
  const committed = existsSync(committedPath) ? readFileSync(committedPath, 'utf8') : null;
  const verdict = checkVerdict({
    hadPendingChanges: summary.hadPendingChanges,
    committedExists: committed !== null,
    comparison: committed === null ? { ok: true } : compareDbml(regenerated, committed),
    tableCount: summary.tableCount,
  });

  if (verdict.exitCode !== 0) {
    if (verdict.firstDivergence !== undefined) {
      reportDivergences(regenerated, committed);
    }
    console.error(verdict.message);
    process.exit(1);
  }
  console.log(verdict.message);
}

export function usageText() {
  return [
    'usage: node scripts/devops/generate-dbml.mjs [--check | --verify-db | --help]',
    '',
    'modes:',
    '  (default)     generate docs/schema.dbml from the EF Core model (dotnet ef dbcontext script + sql2dbml); deterministic, no timestamps',
    '  --check       regenerate to a temp file and fail when the committed docs/schema.dbml is stale or the EF model has pending changes without a migration',
    '  --verify-db   introspect the live Postgres (read-only) and report dangling tables/columns (in database, not in EF model) and unmigrated tables (in EF model, not in database); exit 1 on any drift',
    '  --help        print this usage',
    '',
    '--verify-db environment (see .env.example; export them or copy .env.example to .env):',
    '  POSTGRES_HOST      default localhost',
    '  POSTGRES_PORT      default 5432',
    '  POSTGRES_DB        database name (required)',
    '  POSTGRES_USER      database user',
    '  POSTGRES_PASSWORD  database password',
    '',
    'examples:',
    '  corepack pnpm db:dbml',
    '  corepack pnpm db:dbml:check',
    '  POSTGRES_DB=gameguild POSTGRES_USER=postgres POSTGRES_PASSWORD=postgres corepack pnpm db:dbml:verify',
  ].join('\n');
}

// CompilerError from @dbml/core carries `message: undefined` and puts the
// human-readable diagnostics in `error.diags[]` — extract a single-line
// description without relying on the stack trace.
function describeParseError(error) {
  if (typeof error?.message === 'string' && error.message.length > 0) {
    return error.message;
  }
  const firstDiag = Array.isArray(error?.diags) ? error.diags[0] : undefined;
  if (firstDiag) {
    const start = firstDiag.location?.start;
    return start ? `${firstDiag.message} (line ${start.line}, column ${start.column})` : firstDiag.message;
  }
  return String(error);
}

// dbmlv2 first: the legacy 'dbml' grammar additionally requires a newline
// before `}` and rejects some one-line shapes. Both grammars need the
// prepareDbmlForCore folding (@dbml/cli emits constructs @dbml/core rejects).
export async function parseDbmlFile(dbmlPath, label) {
  const { Parser } = await import('@dbml/core');
  const content = prepareDbmlForCore(readFileSync(dbmlPath, 'utf8'));
  try {
    return new Parser().parse(content, 'dbmlv2');
  } catch {
    try {
      return new Parser().parse(content, 'dbml');
    } catch (error) {
      throw new Error(`could not parse ${label} DBML: ${describeParseError(error)}`);
    }
  }
}

function toNormalizedTables(database) {
  return collectTables(database).map((table) => ({
    name: normalizeName(table.name),
    columns: (table.fields ?? []).map((field) => normalizeName(field.name)),
  }));
}

// Live introspection must cover every schema the model physically spans, so
// the schema list is derived from the freshly regenerated model DBML (never
// from the committed docs/schema.dbml).
function collectSchemaNames(database) {
  const names = (database.schemas ?? []).map((schema) => normalizeName(schema.name));
  return [...new Set(names.filter((name) => name.length > 0))].sort();
}

function runDb2Dbml(dsn) {
  // @dbml/cli 10.x takes the database type as a SEPARATE leading argument;
  // a bare DSN is misread as the type ("Unsupported database type: unknown").
  const result = spawnSync(
    'corepack',
    ['pnpm', 'exec', 'db2dbml', 'postgres', dsn, '-o', LIVE_DBML_PATH],
    SPAWN_OPTS,
  );
  // db2dbml drops a dbml-error.log into its cwd on every run (empty on
  // success) — same litter as runSql2dbml.
  rmSync(path.join(REPO_ROOT, 'dbml-error.log'), { force: true });
  if (result.error?.code === 'ENOENT') {
    throw new Error('corepack not found — it ships with Node.js >= 20');
  }
  if (result.error) {
    throw new Error(`failed to start db2dbml: ${result.error.message}`);
  }
  return result;
}

function printDriftSection(title, items) {
  console.log(title);
  if (items.length === 0) {
    console.log('none');
    return;
  }
  for (const item of items) {
    console.log(`  ${item}`);
  }
}

function printDriftReport(drift) {
  printDriftSection('DANGLING TABLES (in database, not in EF model):', drift.danglingTables);
  printDriftSection('UNMIGRATED TABLES (in EF model, not in database):', drift.unmigratedTables);
  printDriftSection('DANGLING COLUMNS:', drift.danglingColumns);
  console.log(
    `drift: ${drift.danglingTables.length} dangling tables, ${drift.danglingColumns.length} dangling columns, ${drift.unmigratedTables.length} unmigrated tables`,
  );
}

// Model side FIRST (spec order flipped deliberately): the introspection DSN's
// schema list derives from the freshly regenerated model, because
// `?schemas=public` alone would hide the 32 tables living in the model's six
// non-public schemas.
async function verifyDatabaseDrift() {
  const modelSummary = runGeneratePipeline(MODEL_DBML_PATH);
  if (modelSummary.hadPendingChanges) {
    console.log('INFO: model side reflects the working EF model (pending changes without a migration)');
  }

  const modelDatabase = await parseDbmlFile(modelSummary.outPath, 'model');
  const modelTables = toNormalizedTables(modelDatabase);

  const dsn = envToDsn(process.env, collectSchemaNames(modelDatabase));
  const live = runDb2Dbml(dsn);
  if (live.status !== 0 || !existsSync(LIVE_DBML_PATH)) {
    const host = process.env.POSTGRES_HOST ?? 'localhost';
    const port = process.env.POSTGRES_PORT ?? '5432';
    const database = process.env.POSTGRES_DB;
    // NEVER print the DSN (it embeds the password) — scrub every form of it.
    const secrets = [
      dsn,
      encodeURIComponent(process.env.POSTGRES_PASSWORD ?? ''),
      process.env.POSTGRES_PASSWORD ?? '',
    ];
    const stderr = redactSecrets(live.stderr || live.stdout || '', secrets).trim();
    throw new Error(
      `cannot introspect database at ${host}:${port}/${database} — is compose Postgres up? (docker compose up -d postgres)` +
        (stderr.length > 0 ? `\n${stderr}` : ''),
    );
  }

  const liveTables = toNormalizedTables(await parseDbmlFile(LIVE_DBML_PATH, 'live-database'));

  const drift = computeDrift(modelTables, liveTables);
  printDriftReport(drift);
  const verdict = verifyDbVerdict(drift);
  if (verdict.exitCode !== 0) {
    process.exit(1);
  }
  console.log(verdict.message);
}

// --verify-db: the user's core requirement — surface anything the live
// database has that the EF model no longer knows about (and vice versa).
// Read-only: db2dbml introspects information_schema over the wire.
async function runVerifyDbMode() {
  try {
    envToDsn(process.env); // fail fast on missing POSTGRES_* before any tooling runs
  } catch (error) {
    console.error(`generate-dbml: ${error.message}`);
    console.error('Set POSTGRES_* vars (see .env.example) or copy .env.example to .env');
    process.exit(1);
  }

  try {
    await verifyDatabaseDrift();
  } catch (error) {
    console.error(`FAIL: ${error.message}`);
    process.exit(1);
  }
}

if (entryHref !== null && import.meta.url === entryHref) {
  const { mode } = parseArgs(process.argv.slice(2));
  if (mode === 'generate') {
    try {
      const summary = runGeneratePipeline('docs/schema.dbml');
      console.log(
        `dbml: ${summary.tableCount} tables, ${summary.enumCount} enums (0 expected — the model has no native Postgres enums) -> ${summary.outPath}`,
      );
    } catch (error) {
      console.error(`generate-dbml: ${error.message}`);
      process.exit(1);
    }
  } else if (mode === 'check') {
    runCheckMode();
  } else if (mode === 'verify-db') {
    runVerifyDbMode().catch((error) => {
      console.error(`generate-dbml: ${error.message}`);
      process.exit(1);
    });
  } else {
    console.log(usageText());
  }
}
