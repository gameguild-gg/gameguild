import { execSync } from 'node:child_process';
import { cpSync, existsSync, mkdirSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { homedir, platform } from 'node:os';
import { dirname, resolve } from 'node:path';
import { createInterface } from 'node:readline';
import { fileURLToPath } from 'node:url';

const isWindows = platform() === 'win32';
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const dotnetRuntime = resolve(root, 'dotnet-runtime');
const publicManaged = resolve(root, 'public/managed');
const projectDotnet = resolve(root, '.dotnet');
const userDotnet = resolve(homedir(), '.dotnet');
const dotnetExeName = isWindows ? 'dotnet.exe' : 'dotnet';

// --- CLI flags ---
const args = process.argv.slice(2);
const envAutoInstall =
  process.env.DOTNET_WASM_AUTO_INSTALL === '1' ||
  process.env.CI === 'true' ||
  process.env.CI === '1' ||
  !!process.env.npm_config_yes;
const nonInteractive = !process.stdin.isTTY;
const flags = {
  help: args.includes('--help'),
  fakeNoDotnet: args.includes('--fake-no-dotnet'),
  fakeNoWorkload: args.includes('--fake-no-workload'),
  dryRun: args.includes('--dry-run'),
  autoInstall: args.includes('--auto-install') || args.includes('--yes') || args.includes('-y') || envAutoInstall || nonInteractive,
};

if (flags.help) {
  console.log(`Usage: tsx scripts/build-dotnet.ts [flags]

Flags:
  --help               Show this help message
  --dry-run            Run detection and prompts but skip actual build steps
  --auto-install, -y   Non-interactively install .NET 9 SDK and wasm-tools workload to the project-local .dotnet/ directory
  --fake-no-dotnet     Pretend .NET 9 SDK is not installed (test install prompt)
  --fake-no-workload   Pretend wasm-tools workload is missing (test workload install)

Environment variables:
  DOTNET_WASM_AUTO_INSTALL=1   Same as --auto-install
  CI=true                      Same as --auto-install
`);
  process.exit(0);
}

if (flags.dryRun) console.log('[DRY-RUN mode: no build steps will execute]');
if (flags.fakeNoDotnet) console.log('[TEST: pretending .NET 9 is not installed]');
if (flags.fakeNoWorkload) console.log('[TEST: pretending wasm-tools is not installed]');
if (flags.autoInstall) console.log('[AUTO-INSTALL mode: missing prerequisites will be installed to project-local .dotnet/ without prompting]');

let activeDotnetPath: string | null = null;

function getDotnet9Version(bin: string): string | null {
  try {
    const version = execSync(`"${bin}" --version`, { encoding: 'utf-8', stdio: ['pipe', 'pipe', 'pipe'] }).trim();
    return version.startsWith('9.') ? version : null;
  } catch {
    return null;
  }
}

function getDotnetVersion(bin: string): string | null {
  try {
    return execSync(`"${bin}" --version`, { encoding: 'utf-8', stdio: ['pipe', 'pipe', 'pipe'] }).trim();
  } catch {
    return null;
  }
}

function whichDotnet(): string | null {
  try {
    const cmd = isWindows ? 'where dotnet' : 'which dotnet';
    return execSync(cmd, { encoding: 'utf-8', stdio: ['pipe', 'pipe', 'pipe'] }).trim().split(/\r?\n/)[0];
  } catch {
    return null;
  }
}

interface DotnetInfo {
  bin: string;
  version: string;
  isLocal: boolean;
}

function findAnyDotnet(): DotnetInfo | null {
  // 1. Project-local .dotnet/
  const projectBin = resolve(projectDotnet, dotnetExeName);
  const projectVer = existsSync(projectBin) ? getDotnetVersion(projectBin) : null;
  if (projectVer) return { bin: projectBin, version: projectVer, isLocal: true };

  // 2. User-local ~/.dotnet/
  const userBin = resolve(userDotnet, dotnetExeName);
  const userVer = existsSync(userBin) ? getDotnetVersion(userBin) : null;
  if (userVer) return { bin: userBin, version: userVer, isLocal: true };

  // 3. System PATH
  const systemBin = whichDotnet();
  const systemVer = systemBin ? getDotnetVersion(systemBin) : null;
  if (systemBin && systemVer) return { bin: systemBin, version: systemVer, isLocal: false };

  return null;
}

function findDotnet9(): string | null {
  // 1. Project-local .dotnet/
  const projectBin = resolve(projectDotnet, dotnetExeName);
  if (existsSync(projectBin) && getDotnet9Version(projectBin)) return projectBin;

  // 2. User-local ~/.dotnet/
  const userBin = resolve(userDotnet, dotnetExeName);
  if (existsSync(userBin) && getDotnet9Version(userBin)) return userBin;

  // 3. System PATH
  const systemBin = whichDotnet();
  if (systemBin && getDotnet9Version(systemBin)) return systemBin;

  return null;
}

function ensureGlobalJson() {
  const globalJsonPath = resolve(root, 'global.json');
  if (existsSync(globalJsonPath)) return;

  const content = {
    sdk: {
      version: '9.0.0',
      rollForward: 'latestFeature',
      allowPrerelease: false,
    },
  };
  writeFileSync(globalJsonPath, JSON.stringify(content, null, 2) + '\n');
  console.log('✓ Created global.json (pins .NET SDK 9.0.x for this project)');
}

function dotnetCmd(): string {
  if (!activeDotnetPath) throw new Error('.NET 9 SDK not configured');
  return activeDotnetPath;
}

function run(cmd: string, cwd?: string) {
  console.log(`> ${cmd}`);
  const env: Record<string, string | undefined> = { ...process.env };
  if (activeDotnetPath) env['DOTNET_ROOT'] = dirname(activeDotnetPath);
  execSync(cmd, {
    cwd,
    stdio: 'inherit',
    shell: isWindows ? 'cmd.exe' : '/bin/bash',
    env,
  });
}

function dotnet(args: string, cwd?: string) {
  run(`"${dotnetCmd()}" ${args}`, cwd);
}

function dotnetExec(args: string, cwd?: string): string {
  const cmd = `"${dotnetCmd()}" ${args}`;
  console.log(`> ${cmd}`);
  const env: Record<string, string | undefined> = { ...process.env };
  if (activeDotnetPath) env['DOTNET_ROOT'] = dirname(activeDotnetPath);
  return execSync(cmd, { cwd, encoding: 'utf-8', env });
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
  console.log(`Installing .NET 9 SDK to ${installDir} ...`);
  mkdirSync(installDir, { recursive: true });
  if (isWindows) {
    run(`powershell -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing https://dot.net/v1/dotnet-install.ps1))) -Channel 9.0 -InstallDir '${installDir}'"`);
  } else {
    run(`curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir "${installDir}"`);
  }

  const bin = resolve(installDir, dotnetExeName);
  if (!getDotnet9Version(bin)) {
    console.error('Error: .NET 9 SDK installation failed.');
    process.exit(1);
  }
  activeDotnetPath = bin;
  console.log(`✓ .NET 9 SDK installed to ${installDir}`);
}

function isUserLocalDotnet(bin: string): boolean {
  const binDir = dirname(bin);
  return binDir === projectDotnet || binDir === userDotnet;
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

// Find .NET 9 SDK: project-local > user-local > system
activeDotnetPath = flags.fakeNoDotnet ? null : findDotnet9();

if (!activeDotnetPath) {
  // Check if there's a dotnet of another version
  const existing = flags.fakeNoDotnet ? null : findAnyDotnet();

  if (existing) {
    // dotnet exists but is not version 9
    console.log(`.NET SDK found: v${existing.version} at "${existing.bin}"`);
    console.log('This project requires .NET 9. You can install it alongside your current version.');
    console.log('A global.json file will pin this project to .NET 9 without affecting other projects.');
    console.log('');

    if (existing.isLocal) {
      // User-local dotnet: offer auto-install or show commands
      if (flags.autoInstall) {
        console.log(`Auto-installing .NET 9 SDK to ${projectDotnet} ...`);
        installDotnet(projectDotnet);
        // Skip the prompt branches below
      } else {
        console.log('Options:');
        console.log('  1) Show commands to install .NET 9 manually');
        console.log('  2) Install .NET 9 to ~/.dotnet          (alongside current version)');
        console.log(`  3) Install .NET 9 to ${projectDotnet}   (only this project)`);
        console.log('  4) Abort');
        console.log('');
        const answer = await ask('Choose [1/2/3/4]: ');

        if (answer === '1') {
          console.log('');
          console.log('Install .NET 9 SDK alongside your current version:');
          console.log('');
          if (isWindows) {
            console.log('  Option A - User-local (~/.dotnet):');
            console.log(`    powershell -NoProfile -ExecutionPolicy unrestricted -Command "&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing https://dot.net/v1/dotnet-install.ps1))) -Channel 9.0"`);
            console.log('');
            console.log('  Option B - Project-local:');
            console.log(`    powershell -NoProfile -ExecutionPolicy unrestricted -Command "&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing https://dot.net/v1/dotnet-install.ps1))) -Channel 9.0 -InstallDir '${projectDotnet}'"`);
          } else {
            console.log('  Option A - User-local (~/.dotnet):');
            console.log('    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0');
            console.log('');
            console.log('  Option B - Project-local:');
            console.log(`    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir "${projectDotnet}"`);
          }
          console.log('');
          console.log('Then re-run this script:');
          console.log('');
          console.log('  npm run build-runtime');
          console.log('');
          process.exit(1);
        } else if (answer === '2') {
          installDotnet(userDotnet);
        } else if (answer === '3') {
          installDotnet(projectDotnet);
        } else {
          console.error('Aborted.');
          process.exit(1);
        }
      }
    } else if (flags.autoInstall) {
      // System-wide dotnet but auto-install requested: install project-locally to avoid sudo
      console.log(`Auto-installing .NET 9 SDK to ${projectDotnet} (avoiding system-wide install) ...`);
      installDotnet(projectDotnet);
    } else {
      // System-wide dotnet (admin): user must install manually
      console.log(`The .NET SDK at "${existing.bin}" is installed system-wide (requires elevated privileges).`);
      console.log('');
      console.log('To install .NET 9 alongside your current version, run the following:');
      console.log('');
      if (isWindows) {
        console.log('  Option A - As Administrator (system-wide, side-by-side):');
        console.log(`    powershell -NoProfile -ExecutionPolicy unrestricted -Command "&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing https://dot.net/v1/dotnet-install.ps1))) -Channel 9.0"`);
        console.log('');
        console.log('  Option B - User-local (no admin needed):');
        console.log(`    powershell -NoProfile -ExecutionPolicy unrestricted -Command "&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing https://dot.net/v1/dotnet-install.ps1))) -Channel 9.0 -InstallDir '%USERPROFILE%\\.dotnet'"`);
      } else {
        console.log('  Option A - System-wide (side-by-side):');
        console.log('    sudo bash -c "curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir /usr/share/dotnet"');
        console.log('');
        console.log('  Option B - User-local (no admin needed):');
        console.log('    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0');
      }
      console.log('');
      console.log('Then re-run this script:');
      console.log('');
      console.log('  npm run build-runtime');
      console.log('');
      process.exit(1);
    }
  } else if (flags.autoInstall) {
    // No dotnet at all + auto-install: install project-locally
    console.log('.NET SDK not found.');
    console.log(`Auto-installing .NET 9 SDK to ${projectDotnet} ...`);
    installDotnet(projectDotnet);
  } else {
    // No dotnet at all
    console.log('.NET SDK not found.');
    console.log('');
    console.log('Options:');
    console.log('  1) Show commands to install manually');
    console.log('  2) Install to ~/.dotnet          (shared across projects)');
    console.log(`  3) Install to ${projectDotnet}   (only this project)`);
    console.log('  4) Abort');
    console.log('');
    const answer = await ask('Choose [1/2/3/4]: ');

    if (answer === '1') {
      console.log('');
      console.log('Install .NET 9 SDK manually using one of the following:');
      console.log('');
      if (isWindows) {
        console.log('  Option A - User-local (~/.dotnet):');
        console.log(`    powershell -NoProfile -ExecutionPolicy unrestricted -Command "&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing https://dot.net/v1/dotnet-install.ps1))) -Channel 9.0"`);
        console.log('');
        console.log('  Option B - Project-local:');
        console.log(`    powershell -NoProfile -ExecutionPolicy unrestricted -Command "&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing https://dot.net/v1/dotnet-install.ps1))) -Channel 9.0 -InstallDir '${projectDotnet}'"`);
      } else {
        console.log('  Option A - User-local (~/.dotnet):');
        console.log('    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0');
        console.log('');
        console.log('  Option B - Project-local:');
        console.log(`    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir "${projectDotnet}"`);
      }
      console.log('');
      console.log('Then re-run this script:');
      console.log('');
      console.log('  npm run build-runtime');
      console.log('');
      process.exit(1);
    } else if (answer === '2') {
      installDotnet(userDotnet);
    } else if (answer === '3') {
      installDotnet(projectDotnet);
    } else {
      console.error('Aborted. Please install .NET 9 SDK manually.');
      process.exit(1);
    }
  }
} else {
  console.log(`Using .NET 9 SDK: ${activeDotnetPath}`);
}

// Ensure global.json pins this project to .NET 9
ensureGlobalJson();

if (flags.dryRun) {
  console.log('');
  console.log(`[DRY-RUN] .NET 9 SDK resolved to: ${activeDotnetPath}`);
  console.log('[DRY-RUN] Would now: check workload, restore, publish, copy files.');
  console.log('[DRY-RUN] Done.');
  process.exit(0);
}

// Check wasm-tools workload
console.log('Checking for wasm-tools workload...');
const workloads = dotnetExec('workload list', dotnetRuntime);
const hasWorkload = !flags.fakeNoWorkload && workloads.includes('wasm-tools');
if (!hasWorkload) {
  const bin = dotnetCmd();
  const isLocal = isUserLocalDotnet(bin);

  if (!isLocal && flags.autoInstall) {
    console.log('');
    console.log('The wasm-tools workload is required but not installed on the system SDK.');
    console.log(`Auto-installing project-local .NET 9 SDK to ${projectDotnet} to avoid elevated privileges...`);
    installDotnet(projectDotnet);
    console.log('Auto-installing wasm-tools workload...');
    dotnet('workload install wasm-tools', dotnetRuntime);
    console.log('✓ wasm-tools workload installed');
  } else if (isLocal) {
    // User-local dotnet: offer to install or show command
    console.log('');
    console.log('The wasm-tools workload is required but not installed.');
    console.log('');
    if (flags.autoInstall) {
      console.log('Auto-installing wasm-tools workload...');
      dotnet('workload install wasm-tools', dotnetRuntime);
      console.log('✓ wasm-tools workload installed');
    } else {
      console.log('Options:');
      console.log('  1) Install wasm-tools now');
      console.log('  2) Show command to install manually');
      console.log('');
      const answer = await ask('Choose [1/2]: ');

      if (answer === '1') {
        console.log('Installing wasm-tools workload...');
        dotnet('workload install wasm-tools', dotnetRuntime);
        console.log('✓ wasm-tools workload installed');
      } else {
        console.log('');
        console.log('Run the following command to install wasm-tools:');
        console.log('');
        console.log(`  "${bin}" workload install wasm-tools`);
        console.log('');
        console.log('Then re-run this script:');
        console.log('');
        console.log('  npm run build-runtime');
        console.log('');
        process.exit(1);
      }
    }
  } else {
    // System dotnet: requires elevated privileges, user must install manually
    console.log('');
    console.log('The wasm-tools workload is required but not installed.');
    console.log(`The .NET SDK at "${bin}" is installed system-wide and requires elevated privileges to modify.`);
    console.log('');
    console.log('For security, please install the required dependencies yourself.');
    console.log('');
    if (isWindows) {
      console.log('Open an Administrator terminal and run:');
      console.log('');
      console.log('  dotnet workload install wasm-tools');
    } else {
      console.log('Run the following command:');
      console.log('');
      console.log('  sudo dotnet workload install wasm-tools');
    }
    console.log('');
    console.log('Then re-run this script:');
    console.log('');
    console.log('  npm run build-runtime');
    console.log('');
    process.exit(1);
  }
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

const frameworkDir = resolve(dotnetRuntime, 'bin/Release/net9.0/browser-wasm/AppBundle/_framework');
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
