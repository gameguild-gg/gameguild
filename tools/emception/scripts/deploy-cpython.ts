
import fs from 'fs';
import path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = process.cwd();

// Ensure shell commands fail on error
shell.config.fatal = true;

const BUILD_WASM_DIR = path.join(ROOT, 'userland/cpython/cpython-3.14.3/build-wasm');
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

// Copy python3.14.zip
const pythonZip = path.join(BUILD_WASM_DIR, 'python3.14.zip');
if (fs.existsSync(pythonZip)) {
  const dest = path.join(SYSROOT_LIB, 'python3.14.zip');
  console.log(`Copying ${pythonZip} -> ${dest}`);
  shell.cp(pythonZip, dest);
} else {
  console.error(`Error: ${pythonZip} not found!`);
  // Not fatal if zip is missing, but good to have
}

console.log('CPython artifacts deployed.');
