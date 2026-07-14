import type { WorkspaceConfig } from '../workspace-config.js';
import { ToolchainPreset } from '../types.js';
import { DEFAULT_CODE, DEFAULT_HEADER } from './defaults.js';

export const CPP_TERMINAL_PRESET: WorkspaceConfig = {
  id: 'cpp-terminal',
  label: 'C++ Terminal',
  description: 'Standard C++ program with stdin/stdout in the terminal',
  version: 1,
  compile: {
    // Direct clang + wasm-ld fast path — bypasses the emcc Python pipeline
    // (~13s savings: no Python boot, no 8970-file pre-warm, no wasm-opt
    // asyncify pass). The WASI runtime handles blocking stdin natively via
    // SharedArrayBuffer + Atomics.wait, so -sASYNCIFY is not needed.
    tool: 'clang',
    args: [],
    output: 'main.wasm',
    toolchain: ToolchainPreset.CPP,
    sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: 'main.cpp' },
  },
  run: {
    type: 'wasi-terminal',
    tool: 'wasi-run',
    args: ['wasi-run', 'main.wasm'],
  },
  features: {
    canvas: false,
    terminalInput: true,
    showTestButton: false,
  },
  files: {
    'main.cpp': { encoding: 'text', content: DEFAULT_CODE },
    'greetings.h': { encoding: 'text', content: DEFAULT_HEADER },
  },
};
