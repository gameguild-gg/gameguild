import { createHash } from 'node:crypto';
import { copyFile, lstat, mkdir, readFile, readdir, readlink, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';

const PYTHON_ROOT = /^\/usr\/lib\/python\d+\.\d+/;
const ENCODING_ALLOWLIST = new Set(['__init__', 'aliases', 'ascii', 'latin_1', 'mbcs', 'idna', 'unicode_escape']);

function shouldExclude(relativePath) {
  if (PYTHON_ROOT.test(relativePath)) {
    if (relativePath.endsWith('.opt-1.pyc') || relativePath.endsWith('.opt-2.pyc')) return true;
    if (/\/python\d+\.\d+\/(idlelib|tkinter|turtledemo|ensurepip|pydoc_data)\//.test(relativePath)) return true;
    if (/\/python\d+\.\d+\/(test|tests)\//.test(relativePath)) return true;
    if (/\/python\d+\.\d+\/unittest\//.test(relativePath)) return true;
    const encoding = relativePath.match(/\/python\d+\.\d+\/encodings\/([^/]+)$/);
    if (encoding) {
      const basename = encoding[1].replace(/\.(py|pyc)$/, '');
      if (!ENCODING_ALLOWLIST.has(basename) && !basename.startsWith('utf_')) return true;
    }
  }
  return relativePath.startsWith('/usr/lib/emscripten/third_party/ply/test/');
}

function priorityFor(relativePath) {
  if (relativePath === '/bin/sh' || relativePath === '/bin/busybox') return 'critical';
  if (relativePath.startsWith('/usr/bin/')) return 'high';
  if (relativePath.startsWith('/usr/include/')) return 'low';
  return 'normal';
}

function profileKind(name) {
  if (name === 'python') return 'interpreter';
  if (name === 'clang') return 'compiler';
  if (name === 'lld') return 'linker';
  if (name === 'cmake') return 'build-system';
  if (name === 'curl') return 'network';
  if (name.startsWith('wasm-')) return 'wasm-transform';
  return 'tool';
}

async function collectFiles(root) {
  const entries = [];
  async function walk(directory) {
    const children = await readdir(directory, { withFileTypes: true });
    children.sort((left, right) => left.name.localeCompare(right.name));
    for (const child of children) {
      const absolutePath = path.join(directory, child.name);
      if (child.isDirectory()) await walk(absolutePath);
      else entries.push(absolutePath);
    }
  }
  await walk(root);
  return entries;
}

async function createProfiles(sysroot, files) {
  const profiles = {};
  const wasmPaths = Object.keys(files).filter((filePath) => /^\/usr\/lib\/[^/]+\.wasm$/.test(filePath));
  for (const wasmPath of wasmPaths.sort()) {
    const name = path.posix.basename(wasmPath, '.wasm');
    const glue = `/usr/lib/${name}.mjs`;
    if (!files[glue]) continue;
    const wasmBytes = await readFile(path.join(sysroot, ...wasmPath.slice(1).split('/')));
    const module = await WebAssembly.compile(wasmBytes);
    const imports = WebAssembly.Module.imports(module)
      .map((entry) => `${entry.module}.${entry.name}`)
      .sort();
    const exports = WebAssembly.Module.exports(module)
      .map((entry) => entry.name)
      .sort();
    profiles[name] = {
      kind: profileKind(name),
      glue,
      wasm: wasmPath,
      profileHash: createHash('sha256')
        .update(`${files[glue].hash}:${files[wasmPath].hash}`)
        .digest('hex'),
      imports,
      exports,
    };
  }
  return profiles;
}

export async function generateReleaseManifest(options) {
  const stagedSuffix = path.join('build', 'stage', 'sysroot');
  if (!path.resolve(options.sysroot).endsWith(stagedSuffix)) {
    throw new Error(`release input must be a staged sysroot: ${options.sysroot}`);
  }

  const sourceFiles = await collectFiles(options.sysroot);
  if (sourceFiles.length === 0) throw new Error(`staged sysroot is empty: ${options.sysroot}`);
  await rm(options.outputDir, { recursive: true, force: true });
  await mkdir(options.outputDir, { recursive: true });

  const files = {};
  for (const absolutePath of sourceFiles) {
    const stats = await lstat(absolutePath);
    const relativePath = `/${path.relative(options.sysroot, absolutePath).replaceAll('\\', '/')}`;
    if (shouldExclude(relativePath)) continue;
    if (stats.isSymbolicLink()) {
      files[relativePath] = { symlink: await readlink(absolutePath) };
      continue;
    }
    if (!stats.isFile()) continue;
    const data = await readFile(absolutePath);
    const destination = path.join(options.outputDir, ...relativePath.slice(1).split('/'));
    await mkdir(path.dirname(destination), { recursive: true });
    await copyFile(absolutePath, destination);
    files[relativePath] = {
      size: data.byteLength,
      hash: createHash('sha256').update(data).digest('hex'),
      executable: (stats.mode & 0o111) !== 0 || relativePath.endsWith('.wasm'),
      priority: priorityFor(relativePath),
    };
  }

  const profiles = await createProfiles(options.sysroot, files);
  const fingerprintInput = {
    artifactVersion: options.artifactVersion,
    runtimeAbi: options.runtimeAbi,
    patchSetVersion: options.patchSetVersion,
    toolVersions: options.toolVersions,
    profiles,
    files,
  };
  const buildFingerprint = createHash('sha256')
    .update(JSON.stringify(fingerprintInput))
    .digest('hex');
  const generated = new Date().toISOString().replace(/\.\d+Z$/, 'Z');
  const manifest = {
    schemaVersion: 2,
    version: 2,
    artifactVersion: options.artifactVersion,
    runtimeAbi: options.runtimeAbi,
    patchSetVersion: options.patchSetVersion,
    buildFingerprint,
    generated,
    baseUrl: options.baseUrl,
    toolVersions: options.toolVersions,
    profiles,
    files,
    bundles: {},
  };
  await mkdir(path.dirname(options.manifestFile), { recursive: true });
  await writeFile(options.manifestFile, `${JSON.stringify(manifest, null, 2)}\n`);
  return manifest;
}
