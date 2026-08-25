import { createHash } from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';

import { loadToolchainState, serializeToolchainLock } from './lock.ts';
import type { BuildContext, BuildRecipe } from './recipes.ts';
import { hashDirectory } from './sources.ts';
import { toolchainPaths } from './paths.ts';

interface BuildReceipt {
  readonly schemaVersion: 1;
  readonly name: string;
  readonly lockHash: string;
  readonly recipeHash: string;
  readonly overlaysHash: string;
  readonly dependencies: Readonly<Record<string, string>>;
  readonly commands: readonly string[];
  readonly detectedVersions: Readonly<Record<string, string>>;
  readonly outputs: Readonly<Record<string, string>>;
}

export interface ExecuteBuildRecipeOptions {
  readonly root: string;
  readonly recipes: Readonly<Record<string, BuildRecipe>>;
  readonly target: string;
  readonly force?: boolean;
  readonly forceRecipes?: readonly string[];
  readonly environment?: NodeJS.ProcessEnv;
  readonly lockFile?: string;
  readonly runScript?: (script: string, environment?: NodeJS.ProcessEnv) => void;
  readonly output?: (line: string) => void;
}

function sortDeep(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(sortDeep);
  if (value && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>)
        .sort(([left], [right]) => left.localeCompare(right))
        .map(([key, entry]) => [key, sortDeep(entry)]),
    );
  }
  return value;
}

function stableJson(value: unknown): string {
  return JSON.stringify(sortDeep(value));
}

function sha256(value: string | Buffer): string {
  return createHash('sha256').update(value).digest('hex');
}

function relativeName(root: string, filename: string): string {
  return path.relative(root, filename).split(path.sep).join('/');
}

function hashOutput(root: string, output: string): string {
  const absolute = path.resolve(root, output);
  const relative = path.relative(root, absolute);
  if (!relative || relative.startsWith('..') || path.isAbsolute(relative)) {
    throw new Error(`Recipe output must be inside ${root}: ${output}`);
  }
  if (!fs.existsSync(absolute)) throw new Error(`Recipe output is missing: ${output}`);
  const stat = fs.statSync(absolute);
  if (stat.isDirectory()) return hashDirectory(absolute);
  if (stat.isFile()) return sha256(fs.readFileSync(absolute));
  throw new Error(`Recipe output has unsupported type: ${output}`);
}

function outputHashes(root: string, outputs: readonly string[]): Record<string, string> {
  return Object.fromEntries(outputs.map((output) => [output.split(path.sep).join('/'), hashOutput(root, output)]));
}

function readReceipt(filename: string): BuildReceipt | null {
  if (!fs.existsSync(filename)) return null;
  try {
    return JSON.parse(fs.readFileSync(filename, 'utf8')) as BuildReceipt;
  } catch {
    return null;
  }
}

function receiptHash(filename: string): string {
  if (!fs.existsSync(filename)) throw new Error(`Dependency receipt is missing: ${filename}`);
  return sha256(fs.readFileSync(filename));
}

function recipeIdentity(recipe: BuildRecipe): string {
  return sha256(stableJson({
    name: recipe.name,
    cacheKey: recipe.cacheKey ?? null,
    dependencies: recipe.dependencies,
    lockEntries: recipe.lockEntries,
    outputs: recipe.outputs,
    run: recipe.run.toString(),
  }));
}

function writeReceiptAtomic(filename: string, receipt: BuildReceipt): void {
  fs.mkdirSync(path.dirname(filename), { recursive: true });
  const temporary = `${filename}.tmp-${process.pid}`;
  fs.writeFileSync(temporary, `${JSON.stringify(sortDeep(receipt), null, 2)}\n`, { flag: 'wx' });
  try {
    fs.renameSync(temporary, filename);
  } catch (error) {
    fs.rmSync(temporary, { force: true });
    throw error;
  }
}

export async function executeBuildRecipe(options: ExecuteBuildRecipeOptions): Promise<void> {
  const paths = toolchainPaths(options.root);
  const state = await loadToolchainState(options.root, options.lockFile);
  const lockHash = sha256(serializeToolchainLock(state.lock));
  const overlaysHash = fs.existsSync(paths.overlays) ? hashDirectory(paths.overlays) : sha256('');
  const active = new Set<string>();
  const complete = new Set<string>();
  const output = options.output ?? console.log;

  const execute = async (name: string): Promise<void> => {
    if (complete.has(name)) return;
    if (active.has(name)) throw new Error(`Toolchain recipe cycle detected at ${name}`);
    const recipe = options.recipes[name];
    if (!recipe) throw new Error(`Unknown Toolchain recipe: ${name}`);
    active.add(name);
    for (const dependency of recipe.dependencies) await execute(dependency);

    const dependencyHashes = Object.fromEntries(
      recipe.dependencies.map((dependency) => [dependency, receiptHash(path.join(paths.receipts, `${dependency}.json`))]),
    );
    const detectedVersions = Object.fromEntries(recipe.lockEntries.map((entry) => {
      const tool = state.lock.tools[entry];
      if (!tool) throw new Error(`${entry} is missing from the Toolchain lock`);
      return [entry, tool.version];
    }));
    const receiptFile = path.join(paths.receipts, `${name}.json`);
    const previous = readReceipt(receiptFile);
    const identity = {
      lockHash,
      recipeHash: recipeIdentity(recipe),
      overlaysHash,
      dependencies: dependencyHashes,
      detectedVersions,
    };
    let currentOutputs: Record<string, string> | null = null;
    try {
      currentOutputs = outputHashes(options.root, recipe.outputs);
    } catch {
      currentOutputs = null;
    }
    const forced = Boolean(options.force || options.forceRecipes?.includes(name));
    const reusable = !forced
      && previous?.schemaVersion === 1
      && previous.name === name
      && stableJson(identity) === stableJson({
        lockHash: previous.lockHash,
        recipeHash: previous.recipeHash,
        overlaysHash: previous.overlaysHash,
        dependencies: previous.dependencies,
        detectedVersions: previous.detectedVersions,
      })
      && currentOutputs !== null
      && stableJson(currentOutputs) === stableJson(previous.outputs);

    if (reusable) {
      output(`[toolchain] ${name}: receipt and outputs verified; reusing build.`);
    } else {
      const commands: string[] = [];
      const context: BuildContext = {
        root: options.root,
        force: forced,
        runScript(script, environment = options.environment ?? process.env) {
          commands.push(`pnpm run ${script}`);
          if (!options.runScript) throw new Error(`No script runner configured for recipe ${name}`);
          options.runScript(script, environment);
        },
      };
      output(`[toolchain] ${name}: building.`);
      await recipe.run(context);
      const outputs = outputHashes(options.root, recipe.outputs);
      writeReceiptAtomic(receiptFile, {
        schemaVersion: 1,
        name,
        ...identity,
        commands,
        outputs,
      });
    }
    active.delete(name);
    complete.add(name);
  };

  await execute(options.target);
}

export function hashBuildReceipt(root: string, name: string): string {
  return receiptHash(path.join(toolchainPaths(root).receipts, `${name}.json`));
}
