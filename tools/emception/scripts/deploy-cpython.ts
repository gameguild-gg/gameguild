
import fs from 'fs';
import path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { detectPythonVersion, pythonMajorMinor } from './lib/detect-versions.ts';
import { setupEmsdk } from './lib/emsdk.ts';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = process.cwd();

// Ensure shell commands fail on error
shell.config.fatal = true;

// Setup EMSDK so we can detect the Python version
const EMSDK_VERSION = process.env.EMSDK_VERSION || 'latest';
setupEmsdk(EMSDK_VERSION);

const PYTHON_VERSION = process.env.PYTHON_VERSION || detectPythonVersion();
const PYTHON_MM = pythonMajorMinor(PYTHON_VERSION);

const BUILD_WASM_DIR = path.join(ROOT, `userland/cpython/cpython-${PYTHON_VERSION}/build-wasm`);
const SYSROOT_LIB = path.join(ROOT, 'sysroot/usr/lib');

console.log('Deploying CPython artifacts to sysroot...');

// Copy python.wasm
const pythonWasm = path.join(BUILD_WASM_DIR, 'python.wasm');
if (fs.existsSync(pythonWasm)) {
  const dest = path.join(SYSROOT_LIB, 'cpython.wasm');
  console.log(`Copying ${pythonWasm} -> ${dest}`);
  shell.cp(pythonWasm, dest);
} else {
  console.error(`Error: ${pythonWasm} not found!`);
  process.exit(1);
}

console.log('CPython artifacts deployed.');
