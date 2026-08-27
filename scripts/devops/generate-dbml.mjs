#!/usr/bin/env node

import { pathToFileURL } from 'node:url';

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
    '// MACHINE-GENERATED from the EF Core model via `dotnet ef migrations script` + sql2dbml.',
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

// `node -e` leaves process.argv[1] undefined; pathToFileURL(undefined) would throw.
const entryHref = process.argv[1] ? pathToFileURL(process.argv[1]).href : null;

if (entryHref !== null && import.meta.url === entryHref) {
  const { mode } = parseArgs(process.argv.slice(2));
  console.error(`generate-dbml: mode '${mode}' is not implemented yet`);
  console.error('usage: node scripts/devops/generate-dbml.mjs [--check | --verify-db]');
  process.exit(1);
}
