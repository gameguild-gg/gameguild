import type { WorkspaceConfig } from './ide-types';
import { DEFAULT_CODE, DEFAULT_HEADER, DEFAULT_IMAGE, SDL_DEMO_CODE } from './ide-types';

// ── C++ SDL3 Bouncing Ball ──────────────────────────────────────

export const CPP_SDL3_PRESET: WorkspaceConfig = {
  id: 'cpp-sdl3',
  label: 'C++ SDL3 — Bouncing Ball',
  description: 'SDL3 graphics demo compiled in the browser with Emscripten',
  version: 1,
  compile: {
    tool: 'emcc',
    args: ['emcc', '{sourceFile}', '-sUSE_SDL=3', '-I/usr/include', '-sALLOW_MEMORY_GROWTH=1', '-sENVIRONMENT=web', '-O1', '-o', '/app/main.wasm'],
    cwd: '/app',
    output: '/app/main.wasm',
    sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: '/user/sdl-main.cpp' },
  },
  run: {
    type: 'sdl3-canvas',
  },
  features: {
    canvas: true,
    terminalInput: false,
    showTestButton: false,
  },
  layout: {
    activeFile: '/user/sdl-main.cpp',
    openTabs: [
      { path: '/user/sdl-main.cpp', group: 'main' },
      { path: '/user/sdl-canvas', group: 'right' },
    ],
    expandedDirs: ['/user'],
  },
  files: {
    '/user/sdl-main.cpp': { encoding: 'text', content: SDL_DEMO_CODE },
    '/user/workspace-preview.svg': { encoding: 'text', content: DEFAULT_IMAGE },
  },
};

// ── C++ Terminal (stdin/stdout) ─────────────────────────────────

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
    cwd: '/app',
    output: '/app/main.wasm',
    sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: '/user/main.cpp' },
  },
  run: {
    type: 'wasi-terminal',
    tool: 'wasi-run',
    args: ['wasi-run', '/app/main.wasm'],
  },
  features: {
    canvas: false,
    terminalInput: true,
    showTestButton: false,
  },
  layout: {
    activeFile: '/user/main.cpp',
    openTabs: [
      { path: '/user/main.cpp', group: 'main' },
      { path: '/user/greetings.h', group: 'main' },
    ],
    expandedDirs: ['/user'],
  },
  files: {
    '/user/main.cpp': { encoding: 'text', content: DEFAULT_CODE },
    '/user/greetings.h': { encoding: 'text', content: DEFAULT_HEADER },
  },
};

// ── CMake Project ───────────────────────────────────────────────

const CMAKE_LISTS = `cmake_minimum_required(VERSION 3.20)
project(hello LANGUAGES CXX)
set(CMAKE_CXX_STANDARD 17)
add_executable(hello main.cpp)
`;

const CMAKE_MAIN = `#include <iostream>

int main() {
  std::cout << "Hello from CMake + Ninja + Emscripten!" << std::endl;
  return 0;
}
`;

export const CMAKE_PRESET: WorkspaceConfig = {
  id: 'cmake',
  label: 'CMake Project',
  description: 'Multi-step build: cmake configure → ninja → run',
  version: 1,
  compile: {
    tool: 'cmake',
    args: ['cmake', '-B', '/app/build', '-G', 'Ninja', '-S', '/app'],
    cwd: '/app',
    output: '/app/build/hello',
    sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: '/user/main.cpp' },
  },
  run: {
    type: 'cmake-build',
    tool: 'wasi-run',
    args: ['wasi-run', '/app/build/hello'],
  },
  features: {
    canvas: false,
    terminalInput: true,
    showTestButton: false,
  },
  layout: {
    activeFile: '/user/main.cpp',
    openTabs: [
      { path: '/user/main.cpp', group: 'main' },
      { path: '/user/CMakeLists.txt', group: 'main' },
    ],
    expandedDirs: ['/user'],
  },
  files: {
    '/user/main.cpp': { encoding: 'text', content: CMAKE_MAIN },
    '/user/CMakeLists.txt': { encoding: 'text', content: CMAKE_LISTS },
  },
};

// ── Python Script ───────────────────────────────────────────────

const PYTHON_HELLO = `# Python 3 — runs directly in the browser via WebAssembly
name = input("What is your name? ")
print(f"Hello, {name}! Welcome to Python in the browser.")

for i in range(5):
    print(f"  {i+1}. Python + WebAssembly = ❤️")
`;

export const PYTHON_PRESET: WorkspaceConfig = {
  id: 'python',
  label: 'Python Script',
  description: 'Run Python 3 directly in the browser terminal',
  version: 1,
  compile: {
    tool: 'python3',
    args: ['python3', '{sourceFile}'],
    cwd: '/app',
    output: '',
    sourceDetect: { extensions: ['.py'], entryPoint: '/user/main.py' },
  },
  run: {
    type: 'python-script',
    tool: 'python3',
    args: ['python3', '{sourceFile}'],
  },
  features: {
    canvas: false,
    terminalInput: true,
    showTestButton: false,
  },
  layout: {
    activeFile: '/user/main.py',
    openTabs: [{ path: '/user/main.py', group: 'main' }],
    expandedDirs: ['/user'],
  },
  files: {
    '/user/main.py': { encoding: 'text', content: PYTHON_HELLO },
  },
};

// ── Preset registry ─────────────────────────────────────────────

export const PRESETS: Record<string, WorkspaceConfig> = {
  'cpp-sdl3': CPP_SDL3_PRESET,
  'cpp-terminal': CPP_TERMINAL_PRESET,
  cmake: CMAKE_PRESET,
  python: PYTHON_PRESET,
};

export const PRESET_IDS = Object.keys(PRESETS);
export const DEFAULT_PRESET = CPP_SDL3_PRESET;
