---
'@gameguild/emception-browser': patch
'emception': patch
'@gameguild/emception-toolchain': patch
'@gameguild/emception-xterm': patch
'@gameguild/emception-react': patch
'@gameguild/emception-webcomponent': patch
'@gameguild/emception-ide': patch
---

Fix emcc: run Emscripten's Node.js build tools (compiler.mjs, preprocessor.mjs, acorn-optimizer.mjs) with their real sources in the browser worker instead of the symbols-only stub, so `em.run('emcc', ...)` produces the requested output artifact (#451).

The old runJavaScript stub for compiler.mjs never emitted the generated JS glue or the `//FORWARDED_DATA:` payload, so Python's emscripten.py aborted with "Did not receive forwarded data in pre output - process failed?" and no output file was written. The new node-tool loader builds the tools' ESM graph from the CDN VFS (blob URLs, node-builtin shims with VFS-backed fs, import-cycle merging for src/modules.mjs ↔ src/parseTools.mjs, vm shim with completion-value semantics and lexical-declaration harvesting). The sysroot now also ships node_modules/acorn and html/ (shell.html/shell.css) required by those tools; LazyFS gains an HTTP fallback for manifest-unlisted files so older CDN manifests keep working.
