import { execSync } from 'node:child_process';
import { existsSync, mkdirSync, rmSync, cpSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { readdirSync } from 'node:fs';

const root = resolve(dirname(new URL(import.meta.url).pathname), '..');
const dotnetRuntime = resolve(root, 'dotnet-runtime');
const publicManaged = resolve(root, 'public/managed');

function run(cmd: string, cwd?: string) {
  console.log(`> ${cmd}`);
  execSync(cmd, { cwd, stdio: 'inherit' });
}

function removePackageJsonFiles(dir: string) {
  if (!existsSync(dir)) return;
  for (const entry of readdirSync(dir, { withFileTypes: true, recursive: true })) {
    if (entry.name === 'package.json') {
      const fullPath = resolve(entry.parentPath, entry.name);
      rmSync(fullPath, { force: true });
    }
  }
}

console.log('=== Building RoslynWrapper for Browser WASM ===');

// Check if .NET SDK is installed
try {
  execSync('dotnet --version', { stdio: 'pipe' });
} catch {
  console.error('Error: .NET SDK not found. Please install .NET 8 SDK.');
  process.exit(1);
}

// Check wasm-tools workload
console.log('Checking for wasm-tools workload...');
const workloads = execSync('dotnet workload list', { cwd: dotnetRuntime, encoding: 'utf-8' });
if (!workloads.includes('wasm-tools')) {
  console.log('Installing wasm-tools workload...');
  run('dotnet workload install wasm-tools', dotnetRuntime);
  console.log('✓ wasm-tools workload installed');
} else {
  console.log('✓ wasm-tools workload already installed');
}

console.log('Restoring packages...');
run('dotnet restore', dotnetRuntime);

console.log('Publishing for browser-wasm...');
run('dotnet publish -c Release -r browser-wasm', dotnetRuntime);

console.log('Cleaning up npm workspace conflicts...');
removePackageJsonFiles(resolve(dotnetRuntime, 'bin'));

console.log('Copying all _framework contents recursively...');
mkdirSync(publicManaged, { recursive: true });
rmSync(publicManaged, { recursive: true, force: true });
mkdirSync(publicManaged, { recursive: true });

const frameworkDir = resolve(dotnetRuntime, 'bin/Release/net8.0/browser-wasm/AppBundle/_framework');
cpSync(frameworkDir, publicManaged, { recursive: true, verbatimSymlinks: true });

// Copy main.js from source
console.log('Copying main.js...');
cpSync(resolve(dotnetRuntime, 'main.js'), resolve(publicManaged, 'main.js'));

console.log('Final cleanup of npm workspace conflicts...');
removePackageJsonFiles(publicManaged);

const fileCount = readdirSync(publicManaged).length;
console.log('');
console.log('=== Build Complete ===');
console.log('');
console.log(`✓ All files copied to public/managed/`);
console.log(`✓ Total files: ${fileCount}`);
console.log('');
console.log('Next steps:');
console.log("1. Run 'npm run compress' to compress assets");
console.log("2. Run 'npm run build' to build TypeScript");
console.log("3. Run 'npm run integrate' to copy to apps/web");
console.log('');
console.log("Or simply run 'npm run setup' to do all steps");
console.log('');
