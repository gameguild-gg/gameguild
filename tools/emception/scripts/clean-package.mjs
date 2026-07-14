import { rm } from 'node:fs/promises';
import path from 'node:path';

const packageRoot = process.cwd();
const targets = process.argv.slice(2);

if (targets.length === 0) {
    throw new Error('At least one package-relative path is required.');
}

for (const target of targets) {
    const resolved = path.resolve(packageRoot, target);
    const relative = path.relative(packageRoot, resolved);

    if (relative.startsWith('..') || path.isAbsolute(relative)) {
        throw new Error(`Refusing to remove a path outside the package: ${target}`);
    }

    await rm(resolved, { recursive: true, force: true });
}
