import type { TreeNode, WorkspaceFile } from './ide-types.js';

export function isSourceFile(path: string): boolean {
    return path.endsWith('.cpp') || path.endsWith('.cc') || path.endsWith('.cxx') || path.endsWith('.c');
}

export function isTextFile(path: string): boolean {
    return (
        !path.endsWith('.svg') && !path.endsWith('.png') && !path.endsWith('.jpg') && !path.endsWith('.jpeg') && !path.endsWith('.gif') && !path.endsWith('.webp')
    );
}

export function resolveWsPath(cwd: string, relOrAbs: string): string {
    if (relOrAbs.startsWith('/')) return relOrAbs;
    return `${cwd}/${relOrAbs}`;
}

export function fileName(path: string): string {
    const parts = path.split('/').filter(Boolean);
    return parts[parts.length - 1] || path;
}

export function inferLanguage(path: string): string {
    if (path.endsWith('.cpp') || path.endsWith('.cc') || path.endsWith('.cxx')) return 'cpp';
    if (path.endsWith('.c')) return 'c';
    if (path.endsWith('.h') || path.endsWith('.hpp')) return 'cpp';
    if (path.endsWith('.md')) return 'markdown';
    if (path.endsWith('.json')) return 'json';
    return 'plaintext';
}

export function buildFileTree(paths: string[]): TreeNode[] {
    const root: TreeNode = { name: '/', path: '/', isDir: true, children: [] };

    for (const rawPath of paths.sort()) {
        const parts = rawPath.split('/').filter(Boolean);
        let current = root;
        let currentPath = '';

        for (let i = 0; i < parts.length; i++) {
            const part = parts[i];
            currentPath += `/${part}`;
            const isDir = i < parts.length - 1;

            let next = current.children.find((c) => c.name === part && c.isDir === isDir);
            if (!next) {
                next = { name: part, path: currentPath, isDir, children: [] };
                current.children.push(next);
                current.children.sort((a, b) => {
                    if (a.isDir !== b.isDir) return a.isDir ? -1 : 1;
                    return a.name.localeCompare(b.name);
                });
            }
            current = next;
        }
    }
    return root.children;
}

/**
 * Returns true if any text source file in the workspace includes SDL3 headers.
 * Used to select the SDL3 compile path over the standard WASI path.
 */
export function detectsSDL(files: Record<string, WorkspaceFile>): boolean {
    return Object.values(files)
        .filter((f) => f.type === 'text' && isSourceFile(f.path))
        .some((f) => f.content.includes('#include <SDL3/') || f.content.includes('#include "SDL3/'));
}

/**
 * Returns emcc args that use emscripten's built-in SDL3 port (-sUSE_SDL=3).
 *
 * Output is main.wasm (standalone/WASI mode). The linker's JS-generation step
 * (compiler.mjs) is not available in the Emception browser environment, so we
 * cannot use -o main.js. The browser canvas API owns the matching runtime
 * imports and generated glue.
 */
export function buildSDL3ArgsPort(targetFsPath: string): string[] {
    return ['emcc', targetFsPath, '-sUSE_SDL=3', '-I/usr/include', '-sALLOW_MEMORY_GROWTH=1', '-sENVIRONMENT=web', '-O1', '-o', '/home/user/main.wasm'];
}
