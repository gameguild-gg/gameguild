import { execSync } from 'node:child_process';
import { existsSync, readdirSync } from 'node:fs';
import { resolve, dirname } from 'node:path';

const root = resolve(dirname(new URL(import.meta.url).pathname), '..');

function gzipFile(filePath: string) {
  execSync(`gzip -9 -c "${filePath}" > "${filePath}.gz"`);
}

console.log('=== Compressing DotNet Runtime Assets ===');

// Check if gzip is available
try {
  execSync('gzip --version', { stdio: 'pipe' });
} catch {
  console.error('Error: gzip not found');
  process.exit(1);
}

const managedDir = resolve(root, 'public/managed');

// Compress managed assemblies
if (existsSync(managedDir)) {
  console.log('Compressing managed assemblies...');

  // Remove package.json that causes npm workspace conflicts
  const pkgJson = resolve(managedDir, 'package.json');
  if (existsSync(pkgJson)) {
    const { rmSync } = await import('node:fs');
    rmSync(pkgJson, { force: true });
  }

  // Find and compress all .dll files
  const findDlls = (dir: string): string[] => {
    const results: string[] = [];
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      const fullPath = resolve(dir, entry.name);
      if (entry.isDirectory()) {
        results.push(...findDlls(fullPath));
      } else if (entry.name.endsWith('.dll')) {
        results.push(fullPath);
      }
    }
    return results;
  };

  for (const dll of findDlls(managedDir)) {
    gzipFile(dll);
  }
  console.log('✓ Managed assemblies compressed');
}

// Compress dotnet.native.wasm
const nativeWasm = resolve(managedDir, 'dotnet.native.wasm');
if (existsSync(nativeWasm)) {
  console.log('Compressing dotnet.native.wasm...');
  gzipFile(nativeWasm);
  console.log('✓ dotnet.native.wasm compressed');
}

// Compress dotnet.js
const dotnetJs = resolve(managedDir, 'dotnet.js');
if (existsSync(dotnetJs)) {
  console.log('Compressing dotnet.js...');
  gzipFile(dotnetJs);
  console.log('✓ dotnet.js compressed');
}

// Compress icudt.dat
const icudat = resolve(root, 'public/icudt.dat');
if (existsSync(icudat)) {
  console.log('Compressing icudt.dat...');
  gzipFile(icudat);
  console.log('✓ icudt.dat compressed');
}

console.log('');
console.log('=== Compression Complete ===');
console.log('');
console.log('Compressed files are ready in public/ directory');
console.log('Original files are preserved for local development');
