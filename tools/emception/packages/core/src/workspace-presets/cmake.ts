import type { WorkspaceConfig } from '../workspace-config.js';

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
        args: ['cmake', '-B', '/home/user/cmake/build', '-G', 'Ninja', '-S', '/home/user/cmake'],
        cwd: '/home/user/cmake',
        output: '/home/user/cmake/build/hello',
        sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: '/user/cmake/main.cpp' },
    },
    run: {
        type: 'cmake-build',
        tool: 'wasi-run',
        args: ['wasi-run', '/home/user/cmake/build/hello'],
    },
    features: {
        canvas: false,
        terminalInput: true,
        showTestButton: false,
    },
    files: {
        '/user/cmake/main.cpp': { encoding: 'text', content: CMAKE_MAIN },
        '/user/cmake/CMakeLists.txt': { encoding: 'text', content: CMAKE_LISTS },
    },
};
