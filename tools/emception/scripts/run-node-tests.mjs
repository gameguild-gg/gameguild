import { readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { spawnSync } from 'node:child_process';

const testDirectory = resolve(process.argv[2] ?? 'tests');
const testFiles = readdirSync(testDirectory, { withFileTypes: true })
  .filter((entry) => entry.isFile() && entry.name.endsWith('.test.mjs'))
  .map((entry) => resolve(testDirectory, entry.name))
  .sort();

if (testFiles.length === 0) {
  throw new Error(`No test files found in ${testDirectory}`);
}

const result = spawnSync(
  process.execPath,
  ['--test', '--test-concurrency=1', '--test-timeout=15000', ...testFiles],
  { stdio: 'inherit' },
);

if (result.error) {
  throw result.error;
}

process.exitCode = result.status ?? 1;
