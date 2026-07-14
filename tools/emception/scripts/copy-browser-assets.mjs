import { copyFile, mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const emceptionRoot = path.resolve(scriptsDir, '..');
const browserRoot = path.join(emceptionRoot, 'packages', 'browser');

const assets = [
    [path.join(browserRoot, 'src', 'emscripten', 'subprocess_shim.py'), path.join(browserRoot, 'dist', 'emscripten', 'subprocess_shim.py')],
    [path.join(emceptionRoot, 'public', 'coi-serviceworker.js'), path.join(browserRoot, 'dist', 'coi-serviceworker.js')],
];

for (const [source, destination] of assets) {
    await mkdir(path.dirname(destination), { recursive: true });
    await copyFile(source, destination);
}

// TypeScript preserves the bundler-only `?raw` import used by the source
// module. Replace the emitted module with a portable ESM string export so
// Node, web components, and published package consumers can import it too.
const shimSource = await readFile(path.join(browserRoot, 'src', 'emscripten', 'subprocess_shim.py'), 'utf8');
const shimModule = `const SUBPROCESS_SHIM = ${JSON.stringify(shimSource)};\n\nexport { SUBPROCESS_SHIM };\n`;
await writeFile(path.join(browserRoot, 'dist', 'emscripten', 'subprocess-shim.js'), shimModule, 'utf8');
await writeFile(
    path.join(browserRoot, 'dist', 'emscripten', 'subprocess-shim.d.ts'),
    'declare const SUBPROCESS_SHIM: string;\n\nexport { SUBPROCESS_SHIM };\n',
    'utf8',
);
