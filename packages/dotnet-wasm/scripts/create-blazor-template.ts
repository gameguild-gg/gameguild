import { execSync } from 'node:child_process';
import { existsSync, cpSync, rmSync, readdirSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { homedir, platform } from 'node:os';
import { fileURLToPath } from 'node:url';

const isWindows = platform() === 'win32';
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const dotnetRuntime = resolve(root, 'dotnet-runtime');
const publicManaged = resolve(root, 'public/managed');
const dotnetExeName = isWindows ? 'dotnet.exe' : 'dotnet';

function findDotnetBin(): string {

  const userBin = resolve(homedir(), '.dotnet', dotnetExeName);
  if (existsSync(userBin)) return userBin;

  return 'dotnet';
}

const dotnetBin = findDotnetBin();

function run(cmd: string, cwd?: string) {
  console.log(`> ${cmd}`);
  execSync(cmd, {
    cwd,
    stdio: 'inherit',
    shell: isWindows ? 'cmd.exe' : '/bin/bash',
    env: { ...process.env, DOTNET_ROOT: dirname(dotnetBin) },
  });
}

console.log('Creating Blazor WASM template...');
run(`"${dotnetBin}" new blazorwasm-empty -n BlazorTemplate -o BlazorTemplate --force`, dotnetRuntime);

const templateDir = resolve(dotnetRuntime, 'BlazorTemplate');

console.log('Building Blazor template...');
run(`"${dotnetBin}" publish -c Release -o ../blazor-output`, templateDir);

const blazorOutput = resolve(dotnetRuntime, 'blazor-output/wwwroot/_framework');

console.log('Copying necessary files to public/managed...');
if (existsSync(blazorOutput)) {
  for (const entry of readdirSync(blazorOutput)) {
    if (entry.endsWith('.dll') || entry.endsWith('.wasm') || entry.endsWith('.js') || entry === 'blazor.boot.json') {
      const src = resolve(blazorOutput, entry);
      const dest = resolve(publicManaged, entry);
      cpSync(src, dest);
      console.log(`  ${entry}`);
    }
  }
}

console.log('Cleaning up...');
rmSync(templateDir, { recursive: true, force: true });
rmSync(resolve(dotnetRuntime, 'blazor-output'), { recursive: true, force: true });

console.log('Done! Check public/managed/ for Blazor runtime files');

const dlls = readdirSync(publicManaged).filter(f => f.endsWith('.dll'));
for (const dll of dlls.slice(0, 20)) {
  console.log(`  ${dll}`);
}
