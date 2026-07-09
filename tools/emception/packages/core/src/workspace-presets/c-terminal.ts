import type { WorkspaceConfig } from '../workspace-config.js';
import { ToolchainPreset } from '../types.js';

// _Generic demo — showcases compile-time type dispatch, a C11 feature.
export const C_GENERIC_CODE = `#include <stdio.h>
#include <stdbool.h>

// _Generic lets you dispatch on type at compile time (C11+, extended in C23).
#define type_name(x) _Generic((x), \\
    int:     "int",               \\
    long:    "long",              \\
    float:   "float",             \\
    double:  "double",            \\
    char:    "char",              \\
    bool:    "bool",              \\
    char *:  "char *",            \\
    default: "other")

int main(void) {
    int    i = 42;
    float  f = 3.14f;
    char   c = 'A';
    char  *s = "hello";
    bool   b = true;

    printf("%-12s -> %s\\n", "42",       type_name(i));
    printf("%-12s -> %s\\n", "3.14f",    type_name(f));
    printf("%-12s -> %s\\n", "\\'A\\'",  type_name(c));
    printf("%-12s -> %s\\n", "\"hello\"",type_name(s));
    printf("%-12s -> %s\\n", "true",     type_name(b));
    return 0;
}
`;

export const C_TERMINAL_PRESET: WorkspaceConfig = {
    id: 'c-terminal',
    label: 'C Terminal',
    description: 'Standard C program with stdin/stdout in the terminal',
    version: 1,
    compile: {
        tool: 'clang',
        args: [],
        output: 'main.wasm',
        toolchain: ToolchainPreset.C,
        sourceDetect: { extensions: ['.c'], entryPoint: 'main.c' },
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
        'main.c': { encoding: 'text', content: C_GENERIC_CODE },
    },
};
