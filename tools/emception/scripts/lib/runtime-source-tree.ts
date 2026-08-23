import fs from 'fs';
import path from 'path';

function isHostPythonBytecode(source: string): boolean {
  const segments = path.normalize(source).split(path.sep);
  return segments.includes('__pycache__') || /\.py[co]$/i.test(path.basename(source));
}

export function pruneHostPythonBytecode(root: string): void {
  if (!fs.existsSync(root)) return;
  if (!fs.statSync(root).isDirectory()) {
    if (/\.py[co]$/i.test(path.basename(root))) fs.rmSync(root, { force: true });
    return;
  }
  for (const entry of fs.readdirSync(root, { withFileTypes: true })) {
    const filename = path.join(root, entry.name);
    if (entry.name === '__pycache__') {
      fs.rmSync(filename, { recursive: true, force: true });
    } else if (entry.isDirectory()) {
      pruneHostPythonBytecode(filename);
    } else if (/\.py[co]$/i.test(entry.name)) {
      fs.rmSync(filename, { force: true });
    }
  }
}

export function copyRuntimeSourceTree(source: string, destination: string): void {
  fs.cpSync(source, destination, {
    recursive: true,
    force: true,
    filter: (candidate) => !isHostPythonBytecode(candidate),
  });
  pruneHostPythonBytecode(destination);
}

export function copyRuntimeDirectoryContents(source: string, destination: string): void {
  fs.mkdirSync(destination, { recursive: true });
  for (const entry of fs.readdirSync(source)) {
    copyRuntimeSourceTree(path.join(source, entry), path.join(destination, entry));
  }
  pruneHostPythonBytecode(destination);
}
