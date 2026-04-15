import { setupEmsdk } from './lib/emsdk.ts';

const EMSDK_VERSION = process.argv[2] || 'latest';

console.log(`>>> Setting up Emscripten SDK (${EMSDK_VERSION})...`);
setupEmsdk(EMSDK_VERSION);
console.log('>>> Emscripten SDK setup complete.');
