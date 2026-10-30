---
'@gameguild/emception-browser': patch
---

Fix em.run argv contract, python exit-code propagation and per-run stderr/stdout capture in the browser tool-runner.

- `em.run('emcc', [args])` now honors the documented pure-args form: the facade prepends the tool name for ToolRunner's C-style argv contract (argv[0] was consumed as the program name, silently dropping the first user argument → `emcc: error: no input files`).
- Python tool exit codes propagate again: the Asyncify-wrapped callMain loses the real status (EXITSTATUS is never set in WASM), so sitecustomize now records it via a `sys.exit` wrapper into a per-run file the tool-runner reads post-run.
- Python stderr/stdout capture moved to per-run env-keyed files: the shared write-through VFS made fixed `/tmp/stderr.log` paths leak a previous run's output into the next result (append-mode writers, never truncated).
- Inject `-sMINIFY_HTML=0` for emcc/em++ unless explicitly requested — html-minifier-terser is not shipped in the browser sysroot, so emcc's auto-enabled MINIFY_HTML (on at -O2+) failed noisily.

Verified against the deployed jsDelivr toolchain via the issue #451 repro (emcc -sSINGLE_FILE=1 -O2 produces the requested artifact).
