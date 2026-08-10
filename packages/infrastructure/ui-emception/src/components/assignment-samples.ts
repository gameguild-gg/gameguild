import type { GradingPlan, WorkspaceConfig } from './ide-types';

// ── C++ stdin/stdout starter with grading plan ───────────────────

const STARTER_CPP = `#include <iostream>
#include <string>
int main() {
  std::string line;
  std::getline(std::cin, line);
  std::cout << line << std::endl;
  return 0;
}
`;

const DOCTEST_CPP = `#define DOCTEST_CONFIG_IMPLEMENT_WITH_MAIN
#include <doctest/doctest.h>

TEST_CASE("basic math") {
  CHECK(1 + 1 == 2);
}
`;

export const ASSIGNMENT_SAMPLES: {
  cpp: { workspaceConfig: WorkspaceConfig; plan: GradingPlan };
} = {
  cpp: {
    workspaceConfig: {
      id: 'assignment-cpp',
      label: 'C++ Assignment',
      description: 'Stdin/stdout starter with hidden doctest case',
      version: 1,
      compile: {
        tool: 'clang',
        args: [],
        cwd: '/home/user',
        output: '/home/user/main.wasm',
        sourceDetect: { extensions: ['.cpp', '.c'], entryPoint: '/user/main.cpp' },
      },
      run: {
        type: 'wasi-terminal',
        tool: 'wasi-run',
        args: ['wasi-run', '/home/user/main.wasm'],
      },
      features: {
        canvas: false,
        terminalInput: true,
        showTestButton: false,
      },
      layout: {
        activeFile: '/user/main.cpp',
        openTabs: [{ path: '/user/main.cpp', group: 'main' }],
        expandedDirs: ['/user'],
      },
      files: {
        '/user/main.cpp': { encoding: 'text', content: STARTER_CPP },
      },
    },
    plan: {
      cases: [
        {
          kind: 'stdio',
          name: 'echo stdin',
          stdin: 'hello world',
          expectedStdout: 'hello world',
          weight: 2,
          hidden: false,
        },
        {
          kind: 'doctest',
          name: 'unit tests',
          sourceFiles: ['/user/test.cpp'],
          weight: 3,
          hidden: true,
        },
      ],
      build: { sources: ['/user/main.cpp'] },
    },
  },
};
