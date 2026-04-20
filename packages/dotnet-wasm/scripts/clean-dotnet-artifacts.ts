import { existsSync, rmSync, readdirSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');

// Remove dotnet build artifacts
for (const dir of ['dotnet-runtime/bin', 'dotnet-runtime/obj']) {
  const fullPath = resolve(root, dir);
  if (existsSync(fullPath)) {
    rmSync(fullPath, { recursive: true, force: true });
    console.log(`Removed ${dir}`);
  }
}

// Remove package.json files inside managed/ that break npm workspaces
function removePackageJsons(dir: string) {
  if (!existsSync(dir)) return;
  for (const entry of readdirSync(dir, { withFileTypes: true, recursive: true })) {
    if (entry.name === 'package.json') {
      const fullPath = resolve(entry.parentPath, entry.name);
      rmSync(fullPath, { force: true });
      console.log(`Removed ${fullPath}`);
    }
  }
}

removePackageJsons(resolve(root, 'public/managed'));
