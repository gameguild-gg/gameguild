import path from 'node:path';

export function toolchainPaths(root: string = process.cwd()) {
  const toolchain = path.join(root, 'toolchain');
  const cache = path.join(root, '.cache', 'toolchain');
  const artifacts = path.join(root, 'artifacts', 'toolchain');
  const releaseCdn = path.join(artifacts, 'release', 'cdn');

  return Object.freeze({
    root,
    configFile: path.join(toolchain, 'toolchain.config.json'),
    lockFile: path.join(toolchain, 'toolchain.lock.json'),
    overlays: path.join(toolchain, 'overlays'),
    cache,
    downloads: path.join(cache, 'downloads'),
    sources: path.join(cache, 'sources'),
    builds: path.join(cache, 'builds'),
    emsdk: path.join(cache, 'emsdk'),
    artifacts,
    tools: path.join(artifacts, 'tools'),
    sysroot: path.join(artifacts, 'sysroot'),
    stagedSysroot: path.join(artifacts, 'stage', 'sysroot'),
    receipts: path.join(artifacts, 'receipts'),
    releaseCdn,
    manifestFile: path.join(releaseCdn, 'manifest.json'),
    publicCdn: path.join(root, 'public', 'cdn'),
    packageCdn: path.join(root, 'packages', 'toolchain', 'cdn'),
    compatibilityCdn: path.join(root, 'packages', 'core', 'cdn'),
  });
}

export type ToolchainPaths = ReturnType<typeof toolchainPaths>;
