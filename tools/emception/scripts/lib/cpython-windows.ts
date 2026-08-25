import { rewriteMsysPathReferences } from './posix-path.ts';

const EMSCRIPTEN_MULTIARCH = 'wasm32-emscripten';

/** Adapt CPython's generated Makefile for a native Windows make executable. */
export function normalizeWindowsCpythonMakefile(content: string, sourceDirectory: string): string {
  return rewriteMsysPathReferences(content, sourceDirectory, 'win32')
    .replace(
      /^(SOABI=([ \t]*))(cpython-\d+)[ \t]*$/m,
      `$1$3-${EMSCRIPTEN_MULTIARCH}`,
    )
    .replace(
      /^MULTIARCH=([ \t]*)$/m,
      `MULTIARCH=$1${EMSCRIPTEN_MULTIARCH}`,
    )
    .replace(
      /^MULTIARCH_CPPFLAGS =[ \t]*$/m,
      `MULTIARCH_CPPFLAGS = -DMULTIARCH=\\"${EMSCRIPTEN_MULTIARCH}\\"`,
    )
    // configure invokes the native emcc.bat with an MSYS include path, so its
    // pyatomic probe can fail before compilation and add an unavailable
    // libatomic. Emscripten provides the required atomics without that library.
    .replace(/^(LIBS=[^\r\n]*?)[ \t]+-latomic[ \t]*$/m, '$1');
}
