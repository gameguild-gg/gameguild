import type { WorkspaceConfig } from '../workspace-config.js';
import { ToolchainPreset } from '../types.js';

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
        args: ['cmake', '-B', 'build', '-G', 'Ninja', '-S', '.'],
        output: 'build/hello',
        toolchain: ToolchainPreset.CMake,
        sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: 'main.cpp' },
    },
    run: {
        type: 'cmake-build',
        tool: 'wasi-run',
        args: ['wasi-run', 'build/hello'],
    },
    features: {
        canvas: false,
        terminalInput: true,
        showTestButton: false,
    },
    files: {
        'main.cpp': { encoding: 'text', content: CMAKE_MAIN },
        'CMakeLists.txt': { encoding: 'text', content: CMAKE_LISTS },
    },
};
