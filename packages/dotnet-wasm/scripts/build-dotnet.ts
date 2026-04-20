import { execSync } from 'node:child_process';
import { existsSync, mkdirSync, rmSync, cpSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { readdirSync } from 'node:fs';
import { homedir, platform } from 'node:os';
import { createInterface } from 'node:readline';

const root = resolve(dirname(new URL(import.meta.url).pathname), '..');
const dotnetRuntime = resolve(root, 'dotnet-runtime');
const publicManaged = resolve(root, 'public/managed');
const projectDotnet = resolve(root, '.dotnet');
const userDotnet = resolve(homedir(), '.dotnet');

let activeDotnetPath: string | null = null;

function getDotnet8Version(bin: string): string | null {
  try {
    const version = execSync(`"${bin}" --version`, { encoding: 'utf-8', stdio: ['pipe', 'pipe', 'pipe'] }).trim();
    return version.startsWith('8.') ? version : null;
  } catch {
    return null;
  }
}

function findDotnet8(): string | null {
  // 1. Project-local .dotnet/
  const projectBin = resolve(projectDotnet, 'dotnet');
  if (existsSync(projectBin) && getDotnet8Version(projectBin)) return projectBin;

  // 2. User-local ~/.dotnet/
  const userBin = resolve(userDotnet, 'dotnet');
  if (existsSync(userBin) && getDotnet8Version(userBin)) return userBin;

  // 3. System PATH
  try {
    const systemBin = execSync('which dotnet', { encoding: 'utf-8', stdio: ['pipe', 'pipe', 'pipe'] }).trim();
    if (getDotnet8Version(systemBin)) return systemBin;
  } catch { /* not found */ }

  return null;
}

function dotnetCmd(): string {
  if (!activeDotnetPath) throw new Error('.NET 8 SDK not configured');
  return activeDotnetPath;
}

function dotnetRoot(): string {
  return dirname(activeDotnetPath!);
}

function run(cmd: string, cwd?: string) {
  console.log(`> ${cmd}`);
  execSync(cmd, { cwd, stdio: 'inherit', env: { ...process.env, DOTNET_ROOT: dotnetRoot() } });
}

function dotnet(args: string, cwd?: string) {
  run(`"${dotnetCmd()}" ${args}`, cwd);
}

function dotnetExec(args: string, cwd?: string): string {
  const cmd = `"${dotnetCmd()}" ${args}`;
  console.log(`> ${cmd}`);
  return execSync(cmd, { cwd, encoding: 'utf-8', env: { ...process.env, DOTNET_ROOT: dotnetRoot() } });
}

function ask(question: string): Promise<string> {
  const rl = createInterface({ input: process.stdin, output: process.stdout });
  return new Promise((res) => {
    rl.question(question, (answer) => {
      rl.close();
      res(answer.trim().toLowerCase());
    });
  });
}

function installDotnet(installDir: string) {
  console.log(`Installing .NET 8 SDK to ${installDir} ...`);
  mkdirSync(installDir, { recursive: true });
  const os = platform();
  if (os === 'win32') {
    run(`powershell -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing https://dot.net/v1/dotnet-install.ps1))) -Channel 8.0 -InstallDir '${installDir}'"`);
  } else {
    run(`curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --install-dir "${installDir}"`);
  }

  const bin = resolve(installDir, 'dotnet');
  if (!getDotnet8Version(bin)) {
    console.error('Error: .NET 8 SDK installation failed.');
    process.exit(1);
  }
  activeDotnetPath = bin;
  console.log(`✓ .NET 8 SDK installed to ${installDir}`);
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

// Find .NET 8 SDK: project-local > user-local > system
activeDotnetPath = findDotnet8();

if (!activeDotnetPath) {
  console.log('.NET 8 SDK not found.');
  console.log('');
  console.log('Options:');
  console.log('  1) Install to ~/.dotnet          (shared across projects)');
  console.log(`  2) Install to ${projectDotnet}   (only this project)`);
  console.log('  3) Abort');
  console.log('');
  const answer = await ask('Choose [1/2/3]: ');

  if (answer === '1') {
    installDotnet(userDotnet);
  } else if (answer === '2') {
    installDotnet(projectDotnet);
  } else {
    console.error('Aborted. Please install .NET 8 SDK manually.');
    process.exit(1);
  }
} else {
  console.log(`Using .NET 8 SDK: ${activeDotnetPath}`);
}

// Check wasm-tools workload
console.log('Checking for wasm-tools workload...');
const workloads = dotnetExec('workload list', dotnetRuntime);
if (!workloads.includes('wasm-tools')) {
  console.log('Installing wasm-tools workload...');
  dotnet('workload install wasm-tools', dotnetRuntime);
  console.log('✓ wasm-tools workload installed');
} else {
  console.log('✓ wasm-tools workload already installed');
}

console.log('Restoring packages...');
dotnet('restore', dotnetRuntime);

console.log('Publishing for browser-wasm...');
dotnet('publish -c Release -r browser-wasm', dotnetRuntime);

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
