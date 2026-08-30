import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import test from 'node:test';

import {
  buildDbmlHeader,
  checkVerdict,
  collectTables,
  compareDbml,
  computeDrift,
  envToDsn,
  normalizeName,
  parseArgs,
  parseDbmlFile,
  prepareDbmlForCore,
  redactSecrets,
  resolveCorepackProcess,
  stripByteOrderMark,
  stripNonDdlStatements,
  usageText,
  validateDbmlOutput,
  verifyDbVerdict,
} from './generate-dbml.mjs';

test('resolveCorepackProcess uses the shell-free Corepack entry point on Windows', () => {
  const invocation = resolveCorepackProcess(['pnpm', 'exec', 'db2dbml'], {
    platform: 'win32',
    nodePath: 'C:\\Program Files\\nodejs\\node.exe',
    fileExists: () => true,
  });

  assert.deepEqual(invocation, {
    command: 'C:\\Program Files\\nodejs\\node.exe',
    args: [
      'C:\\Program Files\\nodejs\\node_modules\\corepack\\dist\\corepack.js',
      'pnpm',
      'exec',
      'db2dbml',
    ],
  });
});

test('resolveCorepackProcess keeps the native Corepack executable on Unix', () => {
  assert.deepEqual(resolveCorepackProcess(['pnpm', '--version'], { platform: 'linux' }), {
    command: 'corepack',
    args: ['pnpm', '--version'],
  });
});

test('parseArgs defaults to generate mode when no flags are given', () => {
  assert.deepEqual(parseArgs([]), { mode: 'generate' });
});

test('parseArgs recognizes --check', () => {
  assert.deepEqual(parseArgs(['--check']), { mode: 'check' });
});

test('parseArgs recognizes --verify-db', () => {
  assert.deepEqual(parseArgs(['--verify-db']), { mode: 'verify-db' });
});

test('envToDsn builds the DSN from a full environment and encodes the password', () => {
  const dsn = envToDsn({
    POSTGRES_USER: 'gg_admin',
    POSTGRES_PASSWORD: 'p@ss w0rd&?=',
    POSTGRES_HOST: 'db.internal',
    POSTGRES_PORT: '6543',
    POSTGRES_DB: 'gameguild',
  });

  assert.equal(dsn, 'postgresql://gg_admin:p%40ss%20w0rd%26%3F%3D@db.internal:6543/gameguild?schemas=public');
});

test('envToDsn applies localhost and 5432 defaults for host and port', () => {
  const dsn = envToDsn({
    POSTGRES_USER: 'postgres',
    POSTGRES_PASSWORD: 'postgres',
    POSTGRES_DB: 'gameguild',
  });

  assert.equal(dsn, 'postgresql://postgres:postgres@localhost:5432/gameguild?schemas=public');
});

test('envToDsn throws naming POSTGRES_DB when the database name is missing', () => {
  assert.throws(
    () => envToDsn({ POSTGRES_USER: 'postgres', POSTGRES_PASSWORD: 'postgres' }),
    /POSTGRES_DB/,
  );
  assert.throws(() => envToDsn({}), /POSTGRES_DB/);
});

test('stripNonDdlStatements keeps table-shape DDL and drops everything else in a migration-like script', () => {
  const migrationSql = [
    'CREATE TABLE public.Users (',
    '    "Id" text NOT NULL,',
    '    "Email" text,',
    '    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")',
    ');',
    'ALTER TABLE public.Users ALTER COLUMN "Email" SET NOT NULL;',
    'CREATE INDEX "IX_Users_Email" ON public.Users ("Email");',
    'CREATE UNIQUE INDEX "IX_Users_Id" ON public.Users ("Id");',
    'CREATE TYPE public.user_role AS ENUM (\'player\', \'admin\');',
    'COMMENT ON TABLE public.Users IS \'A user account\';',
    'DO $roles$',
    'BEGIN',
    '  PERFORM 1; -- embedded semicolon inside the dollar-quoted body',
    'END',
    '$roles$;',
    'CREATE FUNCTION public.audit_delete() RETURNS trigger AS $$',
    'BEGIN',
    '  INSERT INTO audit_log VALUES (1);',
    '  RETURN OLD;',
    'END',
    '$$ LANGUAGE plpgsql;',
    'INSERT INTO "__EFMigrationsHistory" (migration_id, product_version) VALUES (\'x\',\'y\');',
    'CREATE SCHEMA IF NOT EXISTS audit;',
    'GRANT SELECT ON public.Users TO app_reader;',
    'REVOKE UPDATE ON public.Users FROM app_writer;',
    'SET client_min_messages = warning;',
    'BEGIN;',
    'COMMIT;',
    'SELECT 1;',
    'CREATE EXTENSION IF NOT EXISTS "uuid-ossp";',
    'CREATE ROLE app_reader LOGIN;',
  ].join('\n');

  const result = stripNonDdlStatements(migrationSql);

  assert.ok(result.includes('CREATE TABLE public.Users'), result);
  assert.ok(result.includes('ALTER TABLE public.Users'), result);
  assert.ok(result.includes('CREATE INDEX "IX_Users_Email"'), result);
  assert.ok(result.includes('CREATE UNIQUE INDEX "IX_Users_Id"'), result);
  assert.ok(result.includes("CREATE TYPE public.user_role AS ENUM ('player', 'admin')"), result);
  assert.ok(result.includes("COMMENT ON TABLE public.Users IS 'A user account'"), result);
  // exactly the six whitelisted statement classes survive
  assert.equal(result.split(';').filter((part) => part.trim().length > 0).length, 6);

  assert.ok(!result.includes('__EFMigrationsHistory'), result);
  assert.ok(!result.includes('PERFORM'), result);
  assert.ok(!result.includes('audit_delete'), result);
  assert.ok(!result.includes('CREATE SCHEMA'), result);
  assert.ok(!result.includes('GRANT'), result);
  assert.ok(!result.includes('REVOKE'), result);
  assert.ok(!result.includes('client_min_messages'), result);
  assert.ok(!result.includes('uuid-ossp'), result);
  assert.ok(!result.includes('CREATE ROLE'), result);
  assert.ok(!result.includes('SELECT 1'), result);
});

test('stripNonDdlStatements tolerates IF NOT EXISTS between the keyword and the object', () => {
  const result = stripNonDdlStatements(
    'CREATE TABLE IF NOT EXISTS "public.Coupons" ("Id" text NOT NULL);\nDROP TABLE IF EXISTS "OldThings";\n',
  );

  assert.ok(result.includes('CREATE TABLE IF NOT EXISTS "public.Coupons"'), result);
  assert.ok(!result.includes('OldThings'), result);
});

test('stripNonDdlStatements returns an empty string for empty input', () => {
  assert.equal(stripNonDdlStatements(''), '');
});

test('stripNonDdlStatements does not crash on an unclosed dollar quote', () => {
  const result = stripNonDdlStatements('DO $$ BEGIN\n  SELECT 1;\n  END');
  assert.equal(result, '');
});

test('stripNonDdlStatements does not crash on $$ inside a string literal', () => {
  const result = stripNonDdlStatements(
    "COMMENT ON COLUMN coupons.note IS 'pay $$ now';\nCREATE TABLE coupons (id int);",
  );

  assert.ok(result.includes("IS 'pay $$ now'"), result);
  assert.ok(result.includes('CREATE TABLE coupons'), result);
});

test('buildDbmlHeader returns the exact machine-generated header block with no timestamps', () => {
  const expected =
    '// MACHINE-GENERATED from the EF Core model via `dotnet ef dbcontext script` + sql2dbml.\n' +
    '// DO NOT EDIT by hand — regenerate with: pnpm db:dbml\n' +
    '// This file reflects the actual EF Core model (no native Postgres enums; enum-like columns are text).\n' +
    '// Hand-curated design documentation (enums, business-rule notes): docs/program.dbml\n';

  assert.equal(buildDbmlHeader(), expected);
});

test('collectTables flattens tables across all schemas', () => {
  const dbmlDatabase = {
    schemas: [{ tables: [{ name: 'users' }] }, { name: 'empty' }, { tables: [{ name: 'orders' }] }],
  };

  const tables = collectTables(dbmlDatabase);

  assert.deepEqual(tables.map((table) => table.name), ['users', 'orders']);
});

test('collectTables throws when no tables are found', () => {
  assert.throws(() => collectTables({ schemas: [] }), /no tables/i);
  assert.throws(() => collectTables({}), /no tables/i);
  assert.throws(() => collectTables({ schemas: [{ name: 'empty' }] }), /no tables/i);
});

test('normalizeName folds double-quoted identifiers', () => {
  assert.equal(normalizeName('"AspNetUsers"'), 'aspnetusers');
});

test('normalizeName folds backticked identifiers', () => {
  assert.equal(normalizeName('`Users`'), 'users');
});

test('normalizeName strips a leading public. schema prefix', () => {
  assert.equal(normalizeName('"public.Users"'), 'users');
  assert.equal(normalizeName('public.Orders'), 'orders');
});

test('importing the module has zero side effects and exports its helpers', () => {
  const moduleUrl = new URL('./generate-dbml.mjs', import.meta.url).href;
  const probe = spawnSync(
    process.execPath,
    ['-e', `import('${moduleUrl}').then((m) => console.log(typeof m.envToDsn))`],
    { encoding: 'utf8' },
  );

  assert.equal(probe.status, 0, probe.stderr);
  assert.equal(probe.stdout.trim(), 'function');
});

test('stripByteOrderMark removes a leading U+FEFF', () => {
  assert.equal(stripByteOrderMark('﻿CREATE TABLE x'), 'CREATE TABLE x');
});

test('stripByteOrderMark leaves BOM-free text untouched and inner BOMs alone', () => {
  assert.equal(stripByteOrderMark('CREATE TABLE x'), 'CREATE TABLE x');
  assert.equal(stripByteOrderMark('a﻿b'), 'a﻿b');
});

test('validateDbmlOutput accepts DBML with tables and no migration history', () => {
  const result = validateDbmlOutput('Table "users" {\n  id text\n}\n\nTable "orders" {\n  id text\n}\n');

  assert.deepEqual(result, { ok: true, tableCount: 2 });
});

test('validateDbmlOutput rejects DBML without Table blocks', () => {
  const result = validateDbmlOutput('-- nothing parsed\n');

  assert.equal(result.ok, false);
  assert.match(result.reason, /no Table blocks/i);
});

test('validateDbmlOutput rejects an __EFMigrationsHistory leak case-insensitively', () => {
  const result = validateDbmlOutput('Table "__EFMigrationsHistory" {\n  migration_id text\n}\n');

  assert.equal(result.ok, false);
  assert.match(result.reason, /__EFMigrationsHistory leaked/i);
});

test('compareDbml returns exactly { ok: true } for identical strings', () => {
  assert.deepEqual(compareDbml('// header\nTable "users" {\n  id text\n}\n', '// header\nTable "users" {\n  id text\n}\n'), {
    ok: true,
  });
});

test('compareDbml reports the first divergence with 1-based line number and both line contents', () => {
  const result = compareDbml('// header\nTable "users" {\n  id text\n}\n', '// header\nTable "orders" {\n  id text\n}\n');

  assert.equal(result.ok, false);
  assert.deepEqual(result.firstDivergence, {
    line: 2,
    actual: 'Table "users" {',
    expected: 'Table "orders" {',
  });
});

test('compareDbml with multi-line divergence reports only the FIRST divergent line', () => {
  const result = compareDbml('l1\nA1\nA2\nA3\nsame\n', 'l1\nB1\nB2\nsame\nsame\n');

  assert.deepEqual(result.firstDivergence, { line: 2, actual: 'A1', expected: 'B1' });
});

test('compareDbml marks a line absent past one side end as null', () => {
  const result = compareDbml('a\nb', 'a\nb\nc');

  assert.deepEqual(result.firstDivergence, { line: 3, actual: null, expected: 'c' });
});

test('compareDbml detects an appended junk line across a utf8 file round-trip', () => {
  const dir = mkdtempSync(path.join(tmpdir(), 'gg-dbml-'));
  try {
    const regeneratedPath = path.join(dir, 'regenerated.dbml');
    const committedPath = path.join(dir, 'committed.dbml');
    const content = '// header\nTable "users" {\n  id text\n}\n';
    writeFileSync(regeneratedPath, content, 'utf8');
    writeFileSync(committedPath, `${content}// junk\n`, 'utf8');

    const result = compareDbml(readFileSync(regeneratedPath, 'utf8'), readFileSync(committedPath, 'utf8'));

    assert.equal(result.ok, false);
    assert.deepEqual(result.firstDivergence, { line: 5, actual: '', expected: '// junk' });
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});

test('checkVerdict prioritizes the pending-changes failure over missing file and staleness', () => {
  const verdict = checkVerdict({
    hadPendingChanges: true,
    committedExists: false,
    comparison: { ok: false, firstDivergence: { line: 1, actual: 'a', expected: 'b' } },
    tableCount: 320,
  });

  assert.equal(verdict.exitCode, 1);
  assert.match(verdict.message, /pending changes without a migration/);
});

test('checkVerdict fails with the missing-file message when docs/schema.dbml is absent', () => {
  const verdict = checkVerdict({
    hadPendingChanges: false,
    committedExists: false,
    comparison: { ok: true },
    tableCount: 320,
  });

  assert.equal(verdict.exitCode, 1);
  assert.match(verdict.message, /docs\/schema\.dbml not found/);
});

test('checkVerdict fails with the stale message and carries the first divergence', () => {
  const verdict = checkVerdict({
    hadPendingChanges: false,
    committedExists: true,
    comparison: compareDbml('a\nx\n', 'a\ny\n'),
    tableCount: 320,
  });

  assert.equal(verdict.exitCode, 1);
  assert.match(verdict.message, /stale/);
  assert.deepEqual(verdict.firstDivergence, { line: 2, actual: 'x', expected: 'y' });
});

test('checkVerdict approves a clean tree with the exact table-count OK line', () => {
  const verdict = checkVerdict({
    hadPendingChanges: false,
    committedExists: true,
    comparison: compareDbml('Table t {\n}\n', 'Table t {\n}\n'),
    tableCount: 320,
  });

  assert.deepEqual(verdict, {
    exitCode: 0,
    message: 'OK: docs/schema.dbml matches the EF Core model (320 tables.)',
  });
});

test('parseArgs recognizes --help', () => {
  assert.deepEqual(parseArgs(['--help']), { mode: 'help' });
});

test('envToDsn accepts a model-derived schema list for live introspection', () => {
  const dsn = envToDsn(
    {
      POSTGRES_USER: 'postgres',
      POSTGRES_PASSWORD: 'postgres',
      POSTGRES_HOST: 'localhost',
      POSTGRES_PORT: '5432',
      POSTGRES_DB: 'gameguild',
    },
    ['public', 'gameguild.authentication', 'assets'],
  );

  assert.equal(
    dsn,
    'postgresql://postgres:postgres@localhost:5432/gameguild?schemas=public,gameguild.authentication,assets',
  );
});

test('computeDrift detects a table present only in the live database', () => {
  const model = [{ name: 'users', columns: ['id'] }];
  const live = [{ name: 'users', columns: ['id'] }, { name: 'orphan_things', columns: ['id'] }];

  assert.deepEqual(computeDrift(model, live), {
    danglingTables: ['orphan_things'],
    danglingColumns: [],
    unmigratedTables: [],
  });
});

test('computeDrift detects a column present only in the live database', () => {
  const model = [{ name: 'users', columns: ['id', 'email'] }];
  const live = [{ name: 'users', columns: ['id', 'email', 'legacy_flag'] }];

  assert.deepEqual(computeDrift(model, live), {
    danglingTables: [],
    danglingColumns: ['users.legacy_flag'],
    unmigratedTables: [],
  });
});

test('computeDrift ignores the allowlisted __efmigrationshistory table', () => {
  const model = [{ name: 'users', columns: ['id'] }];
  const live = [
    { name: 'users', columns: ['id'] },
    { name: '__efmigrationshistory', columns: ['migration_id', 'product_version'] },
  ];

  assert.deepEqual(computeDrift(model, live), {
    danglingTables: [],
    danglingColumns: [],
    unmigratedTables: [],
  });
});

test('computeDrift ignores the allowlisted project version migration review table', () => {
  const model = [{ name: 'project_versions', columns: ['id', 'status'] }];
  const live = [
    { name: 'project_versions', columns: ['id', 'status'] },
    {
      name: 'project_version_status_migration_review',
      columns: ['projectversionid', 'originalstatus', 'recordedat'],
    },
  ];

  assert.deepEqual(computeDrift(model, live), {
    danglingTables: [],
    danglingColumns: [],
    unmigratedTables: [],
  });
});

test('computeDrift ignores the allowlisted raw-SQL economy gateway tables', () => {
  const model = [{ name: 'users', columns: ['id'] }];
  const live = [
    { name: 'users', columns: ['id'] },
    { name: 'economy_fifo_transfer_operations', columns: ['id'] },
    { name: 'economy_fragment_reservations', columns: ['id'] },
    { name: 'economy_hard_to_soft_conversion_operations', columns: ['id'] },
    { name: 'economy_payout_requests', columns: ['id'] },
    { name: 'economy_payout_request_review_audit_events', columns: ['id'] },
    { name: 'economy_provider_reversal_operations', columns: ['id'] },
    { name: 'economy_provider_reversal_fragments', columns: ['id'] },
  ];

  assert.deepEqual(computeDrift(model, live), {
    danglingTables: [],
    danglingColumns: [],
    unmigratedTables: [],
  });
});

test('computeDrift still reports unknown live tables despite the allowlist', () => {
  const model = [{ name: 'users', columns: ['id'] }];
  const live = [
    { name: 'users', columns: ['id'] },
    { name: 'economy_wallet_debts_legacy', columns: ['id'] },
    { name: 'economy_totally_new_gateway_table', columns: ['id'] },
  ];

  assert.deepEqual(computeDrift(model, live), {
    danglingTables: ['economy_totally_new_gateway_table', 'economy_wallet_debts_legacy'],
    danglingColumns: [],
    unmigratedTables: [],
  });
});

test('computeDrift detects a table present only in the EF model (unmigrated)', () => {
  const model = [{ name: 'users', columns: ['id'] }, { name: 'fresh_things', columns: ['id'] }];
  const live = [{ name: 'users', columns: ['id'] }];

  assert.deepEqual(computeDrift(model, live), {
    danglingTables: [],
    danglingColumns: [],
    unmigratedTables: ['fresh_things'],
  });
});

test('computeDrift on a clean self-comparison reports zero drift everywhere', () => {
  const model = [
    { name: 'users', columns: ['id', 'email'] },
    { name: 'orders', columns: ['id', 'user_id'] },
  ];

  assert.deepEqual(computeDrift(model, model), {
    danglingTables: [],
    danglingColumns: [],
    unmigratedTables: [],
  });
});

test('computeDrift treats public.AspNetUsers / "AspNetUsers" / aspnetusers as one table after normalizeName', () => {
  assert.equal(normalizeName('public.AspNetUsers'), normalizeName('"AspNetUsers"'));
  assert.equal(normalizeName('"AspNetUsers"'), normalizeName('aspnetusers'));

  const model = [
    {
      name: normalizeName('"public.AspNetUsers"'),
      columns: [normalizeName('"Id"'), normalizeName('"Email"')],
    },
  ];
  const live = [
    { name: normalizeName('aspnetusers'), columns: [normalizeName('id'), normalizeName('email')] },
  ];

  assert.deepEqual(computeDrift(model, live), {
    danglingTables: [],
    danglingColumns: [],
    unmigratedTables: [],
  });
});

test('computeDrift sorts every drift class alphabetically for deterministic output', () => {
  const model = [{ name: 'alpha', columns: ['a'] }];
  const live = [
    { name: 'zeta', columns: ['z'] },
    { name: 'beta', columns: ['b'] },
    { name: 'alpha', columns: ['a', 'zz_col', 'mm_col'] },
  ];

  const drift = computeDrift(model, live);

  assert.deepEqual(drift.danglingTables, ['beta', 'zeta']);
  assert.deepEqual(drift.danglingColumns, ['alpha.mm_col', 'alpha.zz_col']);
});

test('computeDrift plan fixture: orphan_things table plus users.legacy_flag column, exit contract 1', () => {
  const model = [
    { name: 'users', columns: ['id', 'email'] },
    { name: 'orders', columns: ['id'] },
  ];
  const live = [
    { name: 'users', columns: ['id', 'email', 'legacy_flag'] },
    { name: 'orders', columns: ['id'] },
    { name: 'orphan_things', columns: ['id'] },
  ];

  const drift = computeDrift(model, live);

  assert.deepEqual(drift, {
    danglingTables: ['orphan_things'],
    danglingColumns: ['users.legacy_flag'],
    unmigratedTables: [],
  });
  // A verify-db that always exited 0 would hide this drift — pin the contract.
  assert.equal(verifyDbVerdict(drift).exitCode, 1);
});

test('verifyDbVerdict exits 1 on any drift class and 0 with the OK line when clean', () => {
  const clean = { danglingTables: [], danglingColumns: [], unmigratedTables: [] };

  assert.deepEqual(verifyDbVerdict(clean), {
    exitCode: 0,
    message: 'OK: live database matches the EF Core model.',
  });
  assert.equal(verifyDbVerdict({ ...clean, danglingTables: ['x'] }).exitCode, 1);
  assert.equal(verifyDbVerdict({ ...clean, danglingColumns: ['t.c'] }).exitCode, 1);
  assert.equal(verifyDbVerdict({ ...clean, unmigratedTables: ['y'] }).exitCode, 1);
});

test('redactSecrets scrubs the DSN and every password form from tool stderr', () => {
  const dsn = 'postgresql://postgres:p%40ss@localhost:1/gameguild?schemas=public';
  const stderr = `connect failed for ${dsn}\nretrying with password=p@ss at ${dsn}`;

  const redacted = redactSecrets(stderr, [dsn, 'p%40ss', 'p@ss']);

  assert.ok(!redacted.includes(dsn));
  assert.ok(!redacted.includes('p@ss'));
  assert.ok(!redacted.includes('p%40ss'));
  assert.ok(redacted.includes('<redacted>'));
});

test('prepareDbmlForCore folds Ref optional markers and drops Checks blocks', () => {
  const dbml = [
    '// header',
    'Table "users" {',
    '  id text [pk]',
    '  Checks {',
    '    `"id" <> \'\'` [name: \'ck_users_id\']',
    '  }',
    '  Indexes {',
    '    id [pk]',
    '  }',
    '}',
    'Ref "FK_pages_pages_ParentPageId":"pages"."Id" ?<? "pages"."ParentPageId" [delete: restrict]',
    'Ref "FK_a_b":"a"."Id" <? "b"."a_id"',
  ].join('\n');

  const prepared = prepareDbmlForCore(dbml);

  assert.ok(!prepared.includes('?'));
  assert.ok(!prepared.includes('Checks'));
  assert.ok(!prepared.includes('ck_users_id'));
  assert.ok(prepared.includes('Table "users" {'));
  assert.ok(prepared.includes('id text [pk]'));
  assert.ok(prepared.includes('Indexes {'));
  assert.ok(prepared.includes('Ref "FK_pages_pages_ParentPageId":"pages"."Id" < "pages"."ParentPageId" [delete: restrict]'));
  assert.ok(prepared.includes('Ref "FK_a_b":"a"."Id" < "b"."a_id"'));
});

test('prepareDbmlForCore leaves question marks in non-Ref lines alone', () => {
  const dbml = "Table t {\n  note text [note: 'is this set? yes']\n}\n";

  assert.equal(prepareDbmlForCore(dbml), dbml);
});

test('prepareDbmlForCore folds column-level check settings db2dbml emits', () => {
  // Shapes taken verbatim from db2dbml 10.1.1 live introspection output
  // (gg-live.dbml): check-last, check-middle, check-first, check-sole, and a
  // body containing `]` (ARRAY[...]) which must not end the setting early.
  const dbml = [
    'Table "tax_rates" {',
    '  "Rate" numeric(5,4) [not null, check: `("Rate" >= (0)::numeric) AND ("Rate" <= (1)::numeric)`]',
    '  "TimeSpentSeconds" int4 [not null, check: `"TimeSpentSeconds" >= 0`, default: 0]',
    '  "PresentationMode" int4 [check: `"PresentationMode" = ANY (ARRAY[0, 1])`, not null]',
    '  "AttemptNumber" int4 [check: `"AttemptNumber" > 0`]',
    '  "Name" text [not null, unique]',
    '}',
  ].join('\n');

  const prepared = prepareDbmlForCore(dbml);

  assert.ok(!prepared.includes('check:'));
  assert.ok(prepared.includes('"Rate" numeric(5,4) [not null]'));
  assert.ok(prepared.includes('"TimeSpentSeconds" int4 [not null, default: 0]'));
  assert.ok(prepared.includes('"PresentationMode" int4 [not null]'));
  assert.ok(prepared.includes('"AttemptNumber" int4'));
  assert.ok(prepared.includes('"Name" text [not null, unique]'));
  assert.ok(!prepared.includes('[]'));
});

test('parseDbmlFile parses a table DBML and reports a labeled failure for garbage input', async () => {
  const dir = mkdtempSync(path.join(tmpdir(), 'gg-dbml-'));
  try {
    const goodPath = path.join(dir, 'good.dbml');
    writeFileSync(goodPath, 'Table users {\n  id text\n}\n', 'utf8');
    const database = await parseDbmlFile(goodPath, 'model');
    assert.ok(collectTables(database).some((table) => table.name === 'users'));

    const badPath = path.join(dir, 'bad.dbml');
    writeFileSync(badPath, 'Table users {\n', 'utf8');
    await assert.rejects(parseDbmlFile(badPath, 'live-database'), (error) => {
      assert.match(error.message, /^could not parse live-database DBML: /);
      assert.ok(!error.message.includes('at ')); // no stack-trace leakage
      return true;
    });
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});

test('usageText documents all three modes and the five POSTGRES_* variables', () => {
  const usage = usageText();

  for (const flag of ['--check', '--verify-db', '--help']) {
    assert.ok(usage.includes(flag), `usage must mention ${flag}`);
  }
  for (const variable of [
    'POSTGRES_HOST',
    'POSTGRES_PORT',
    'POSTGRES_DB',
    'POSTGRES_USER',
    'POSTGRES_PASSWORD',
  ]) {
    assert.ok(usage.includes(variable), `usage must mention ${variable}`);
  }
});
