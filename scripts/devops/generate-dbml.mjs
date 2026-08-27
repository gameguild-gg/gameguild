#!/usr/bin/env node

import { spawnSync } from 'node:child_process';
import { existsSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

export function parseArgs(argv) {
  if (argv.includes('--verify-db')) {
    return { mode: 'verify-db' };
  }
  if (argv.includes('--check')) {
    return { mode: 'check' };
  }
  return { mode: 'generate' };
}

export function envToDsn(env) {
  const database = env.POSTGRES_DB;
  if (!database) {
    throw new Error('POSTGRES_DB is required to build the database DSN (see .env.example)');
  }
  return `postgresql://${env.POSTGRES_USER}:${encodeURIComponent(env.POSTGRES_PASSWORD)}@${
    env.POSTGRES_HOST ?? 'localhost'
  }:${env.POSTGRES_PORT ?? '5432'}/${database}?schemas=public`;
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

const REPO_ROOT = fileURLToPath(new URL('../..', import.meta.url));
const API_PROJECT = 'apps/api/Source/GameGuild.API/GameGuild.API.csproj';
const MODEL_SQL_PATH = path.join(tmpdir(), 'gg-model.sql');
const FILTERED_SQL_PATH = path.join(tmpdir(), 'gg-model-filtered.sql');
const MODEL_DBML_PATH = path.join(tmpdir(), 'gg-model.dbml');
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
  } else {
    console.error(`generate-dbml: mode '${mode}' is not implemented yet`);
    console.error('usage: node scripts/devops/generate-dbml.mjs [--check | --verify-db]');
    process.exit(1);
  }
}
