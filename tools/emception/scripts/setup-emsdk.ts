import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';

enableBuildKeepalive('setup-emsdk');

const EMSDK_VERSION = process.argv[2] || 'latest';

console.log(`>>> Setting up Emscripten SDK (${EMSDK_VERSION})...`);
setupEmsdk(EMSDK_VERSION);
console.log('>>> Emscripten SDK setup complete.');
