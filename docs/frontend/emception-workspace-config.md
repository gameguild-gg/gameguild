# Plan: Configurable Emception Workspace Types

## TL;DR

Introduce a `WorkspaceConfig` type system + single-file `.workspace.json` bundle format that drives the IDE's initial files, compile/run commands, test commands, execution strategy, and UI layout. The IDE loads a workspace bundle from a URL (or uses built-in presets), hydrates files into the VFS, and dispatches compile/run/test actions based on the config. Workspaces are self-contained and distributable.

## Current State

- IdeProps: only `title` + `manifestUrl`
- Initial files hardcoded in `INITIAL_FILES` (ide-types.ts)
- handleCompile() branches SDL3 vs WASI via `detectsSDL()` — 350 lines inline
- worker-client has `writeFile()` for main→worker VFS transfer
- VFS already supports .tar.br bundle loading with SHA256 verification
- Python3 WASM available but not exposed for user scripts
- No C++ test framework headers bundled (doctest.h not in /usr/include)

## Bundle Format: `.workspace.json`

A single JSON file (optionally gzipped as `.workspace.json.gz`) containing:

```jsonc
{
  "id": "cpp-sdl3",
  "label": "C++ SDL3 — Bouncing Ball",
  "description": "SDL3 graphics demo compiled in the browser",
  "version": 1,
  // Compilation & execution
  "compile": {
    "tool": "emcc",
    "args": ["emcc", "{sourceFile}", "-sUSE_SDL=3", "-O1", "-o", "/home/user/main.wasm"],
    "cwd": "/home/user",
    "output": "/home/user/main.wasm",
    "sourceDetect": { "extensions": [".cpp", ".c"], "entryPoint": "/src/main.cpp" },
  },
  "run": {
    "type": "sdl3-canvas", // or "wasi-terminal", "python-script", "cmake-build"
    "tool": "wasi-run", // ignored for sdl3-canvas
    "args": ["wasi-run", "/home/user/main.wasm"],
  },
  // Optional test configuration
  "test": {
    "tool": "emcc",
    "compileArgs": ["emcc", "/src/test_main.cpp", "-o", "/home/user/test.wasm", "-O0"],
    "runArgs": ["wasi-run", "/home/user/test.wasm"],
    "framework": "doctest", // for UI hints; or "pytest", "unittest", "custom"
  },
  // UI features
  "features": {
    "canvas": true,
    "terminalInput": false,
    "showTestButton": true,
  },
  // Initial layout
  "layout": {
    "activeFile": "/src/sdl-main.cpp",
    "openTabs": [
      { "path": "/src/sdl-main.cpp", "group": "main" },
      { "path": "/runtime/sdl-canvas", "group": "right" },
    ],
    "expandedDirs": ["/src"],
  },
  // Files (inline base64 or plain text)
  "files": {
    "/src/sdl-main.cpp": { "encoding": "text", "content": "..." },
    "/src/doctest.h": { "encoding": "text", "content": "..." },
    "/assets/logo.png": { "encoding": "base64", "content": "iVBOR..." },
  },
}
```

## Steps

### Phase 1: Types + Bundle Format

1. Add types to `ide-types.ts`: `WorkspaceConfig`, `CompileConfig`, `RunConfig`, `TestConfig`, `WorkspaceFeatures`, `LayoutConfig`, `BundleFile`
2. Add `parseWorkspaceBundle(json: string): WorkspaceConfig` utility that validates and hydrates the bundle JSON into typed config + files map

### Phase 2: Built-in Presets (workspace-presets.ts — NEW file)

3. `CPP_SDL3_PRESET` — current SDL3 bouncing ball (sdl3-canvas run type)
4. `CPP_TERMINAL_PRESET` — current DEFAULT_CODE + greetings.h (wasi-terminal run type, terminalInput: true)
5. `CMAKE_PRESET` — CMakeLists.txt + main.cpp (cmake-build run type: cmake configure → ninja → wasi-run)
6. `PYTHON_PRESET` — hello.py (python-script run type: python3 direct, compile button becomes "Run")
7. Export `PRESETS` map + `DEFAULT_PRESET`

### Phase 3: Refactor Ide.tsx

8. Expand `IdeProps`:
   - `workspaceConfig?: WorkspaceConfig` — inline config
   - `workspaceUrl?: string` — URL to fetch a `.workspace.json` bundle
   - Keep `title`, `manifestUrl` as-is
9. Replace hardcoded initial state with config-driven: `files`, `openTabs`, `activeTabId`, `expandedDirs`
10. Refactor `handleCompile()` dispatch based on `config.run.type`:
    - `'sdl3-canvas'` → existing SDL3 path (extract to helper `runSdl3Canvas()`)
    - `'wasi-terminal'` → existing WASI compile+run path (extract to `runWasiTerminal()`)
    - `'cmake-build'` → sequential: `cmake -B build -G Ninja` → `ninja -C build` → `wasi-run output`
    - `'python-script'` → `python3 <file>` directly
11. Add `handleTest()` — similar to compile but uses `config.test` args, shows pass/fail in terminal
12. Compile args use `{sourceFile}` placeholder, resolved at runtime to the active/detected source

### Phase 4: Workspace Loader

13. Add `loadWorkspaceFromUrl(url: string): Promise<WorkspaceConfig>` — fetches JSON (or .gz), parses, validates
14. In Ide.tsx init: if `workspaceUrl` prop given, fetch + parse + hydrate files into VFS via `client.writeFile()`
15. File content with `encoding: "base64"` is decoded to Uint8Array; `"text"` is used as-is

### Phase 5: Workspace Picker UI

16. Add dropdown/selector in toolbar showing available presets
17. Switching workspace resets state to new config defaults
18. Persist selected workspace id + any file modifications in localStorage
19. Add "Test" button (▶ with checkmark icon) next to compile button, visible when `features.showTestButton`

### Phase 6: Update Demos

20. Update demos to pass workspace config or accept URL param `?workspace=cpp-sdl3`
21. Default to SDL3 workspace for backward compatibility

### Phase 7: C++ Test Support

22. Bundle `doctest.h` single-header into CDN (add to sysroot build: download from GitHub, place in `/usr/include/doctest/`)
23. CPP_TERMINAL_PRESET can optionally include a test file using doctest
24. Python presets can use stdlib `doctest` or `unittest` module (already available in Python VFS)

## Relevant Files

- `packages/emception/src/components/ide-types.ts` — type definitions
- `packages/emception/src/components/workspace-presets.ts` — NEW: built-in presets
- `packages/emception/src/components/Ide.tsx` — refactor compile dispatch, add config prop, picker, test button
- `packages/emception/src/components/ide-utils.ts` — `buildSDL3ArgsPort` migrates to SDL3 preset
- `tools/emception/src/worker-client.ts` — already has writeFile (no changes needed)
- `tools/emception/apps/ide-react/src/App.tsx` + `tools/emception/apps/ide-next/src/app/page.tsx` — pass config

## Verification

1. Load SDL3 preset → bouncing ball compiles, canvas renders, keyboard scoped
2. Load C++ terminal preset → hello world compiles, stdin works, no canvas
3. Load CMake preset → cmake generates Ninja project, builds, runs output
4. Load Python preset → python3 runs script, output in terminal
5. Load `.workspace.json` from URL → files hydrated, compile works
6. Test button → runs test config, shows pass/fail
7. Workspace switcher resets state
8. Existing e2e test still passes
9. Both demos render with default workspace

## Decisions

- **Single JSON bundle** (not tar/zip) — simpler tooling, human-readable, can be gzipped for size. Binary assets use base64 encoding.
- **Config is data-driven** (JSON-serializable) — no function references. Build args use `{sourceFile}` placeholder string instead of callbacks. This enables URL-loaded configs.
- **run.type enum** controls dispatch — not auto-detection. The workspace author explicitly declares the execution model.
- **Test config is optional** — workspaces without tests simply don't show the test button.
- **doctest.h for C++** — single-header, no build system needed, ideal for in-browser testing.
- **Python doctest/unittest** — already in stdlib, just needs preset config pointing to the right command.

## Further Considerations

1. **Large workspaces**: For workspaces with many files or large binaries, support a `"filesUrl"` field pointing to a .tar.br bundle (reuses existing VFS bundle infrastructure) instead of inlining everything in JSON.
2. **Grading/automated testing**: The test config could output JSON results for automated grading. Recommend a follow-up to add structured test output parsing.
3. **Workspace export**: Add "Export Workspace" button that serializes current files + config back to `.workspace.json` — enables students to share their work.
