import { readFileSync } from 'node:fs';
import { dirname, relative, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

interface GeneratorMetadata {
  hash: string;
  apiVersion?: string;
  source: string;
  generatedAt?: string;
  generatedBy?: string;
}

export function stableMetadata(metadata: GeneratorMetadata): Pick<GeneratorMetadata, 'hash' | 'apiVersion' | 'source'> {
  return {
    hash: metadata.hash,
    apiVersion: metadata.apiVersion,
    source: metadata.source,
  };
}

function git(repositoryRoot: string, args: string[]) {
  return spawnSync('git', args, {
    cwd: repositoryRoot,
    encoding: 'utf8',
  });
}

function readCommittedMetadata(repositoryRoot: string, metadataPath: string): GeneratorMetadata {
  const result = git(repositoryRoot, ['show', `HEAD:${metadataPath}`]);
  if (result.status !== 0) {
    throw new Error(result.stderr.trim() || `Unable to read committed ${metadataPath}`);
  }

  return JSON.parse(result.stdout) as GeneratorMetadata;
}

export function checkGeneratedClientDiff(): number {
  const scriptDirectory = dirname(fileURLToPath(import.meta.url));
  const packageRoot = resolve(scriptDirectory, '..');
  const repositoryRoot = resolve(packageRoot, '../../..');
  const generatedDirectory = resolve(packageRoot, 'src/generated');
  const generatedPath = relative(repositoryRoot, generatedDirectory).replaceAll('\\', '/');
  const metadataPath = `${generatedPath}/.metadata.json`;
  const generatedPathspec = generatedPath;
  const metadataExclusion = `:(exclude)${metadataPath}`;

  const diff = git(repositoryRoot, ['diff', '--exit-code', 'HEAD', '--', generatedPathspec, metadataExclusion]);
  const untracked = git(repositoryRoot, ['ls-files', '--others', '--exclude-standard', '--', generatedPathspec]);

  let metadataMatches = false;
  try {
    const current = JSON.parse(readFileSync(resolve(generatedDirectory, '.metadata.json'), 'utf8')) as GeneratorMetadata;
    const committed = readCommittedMetadata(repositoryRoot, metadataPath);
    metadataMatches = JSON.stringify(stableMetadata(current)) === JSON.stringify(stableMetadata(committed));
  } catch (error) {
    console.error(error instanceof Error ? error.message : error);
  }

  const untrackedFiles = untracked.stdout.trim();
  const hasDiff = diff.status !== 0 || untracked.status !== 0 || untrackedFiles.length > 0 || !metadataMatches;

  if (!hasDiff) {
    console.log('Generated client matches the committed output.');
    return 0;
  }

  console.error('Generated client drift detected. Run `pnpm generate`, review the output, and commit it.');
  if (diff.stdout) process.stdout.write(diff.stdout);
  if (diff.stderr) process.stderr.write(diff.stderr);
  if (untrackedFiles) console.error(`Untracked generated files:\n${untrackedFiles}`);
  if (!metadataMatches) console.error('Stable generator metadata (hash, apiVersion, or source) differs from HEAD.');
  return 1;
}

const invokedPath = process.argv[1] ? resolve(process.argv[1]) : null;
if (invokedPath === fileURLToPath(import.meta.url)) {
  process.exitCode = checkGeneratedClientDiff();
}
