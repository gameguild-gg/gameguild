import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import test from 'node:test';

import {
  buildDbmlHeader,
  collectTables,
  envToDsn,
  normalizeName,
  parseArgs,
  stripNonDdlStatements,
} from './generate-dbml.mjs';

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
    '// MACHINE-GENERATED from the EF Core model via `dotnet ef migrations script` + sql2dbml.\n' +
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
