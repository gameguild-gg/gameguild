import type { OnMount } from '@monaco-editor/react';
import { bootInWorker, DEFAULT_MANIFEST_URL, wrapWorkerClient } from '@gameguild/emception-browser';
import { Terminal } from '@xterm/xterm';
import { forwardRef, useCallback, useEffect, useImperativeHandle, useLayoutEffect, useRef, useState, type ReactNode } from 'react';
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels';
import DockGroupPanel from './DockGroup';
import FileExplorer from './FileExplorer';
import type { DockGroup, FileMeta, FileMetaInput, GradingPlan, OpenTab, TabType, TerminalTab, WorkspaceConfig, WorkspaceFile } from './ide-types';
import { DEFAULT_IMAGE, SDL_CANVAS_PATH, parseWorkspaceBundle, resolveArgs, workspaceConfigToState, workspaceStorageKey } from './ide-types';
import TestResultsPanel from './TestResultsPanel';
import { buildFileTree, inferLanguage, isSourceFile, isTextFile, makeWasiStubs, toWorkspaceFsPath } from './ide-utils';
import { MINI_DOCTEST_H, parseMiniDoctest } from './doctest-header';
import TerminalPanel from './TerminalPanel';
import { DEFAULT_PRESET, PRESETS, PRESET_IDS } from './workspace-presets';

/** Creates a line-buffered stdin reader from the tty. Shared by WASI, CMake, and Python paths. */
function makeLineBufferedStdin(tty: { readByteExclusive: () => number | Promise<number> | null }): () => Promise<number> {
  const lineQueue: number[] = [];
  let lineBuf = '';
  let lineCursor = 0;
  return async (): Promise<number> => {
    if (lineQueue.length > 0) return lineQueue.shift()!;
    while (true) {
      const raw = tty.readByteExclusive();
      if (raw === null) {
        await new Promise<void>((resolve) => setTimeout(resolve, 8));
        continue;
      }
      const byte: number = typeof (raw as Promise<number>).then === 'function' ? await (raw as Promise<number>) : (raw as number);
      if (byte === -1 || byte === 0) {
        await new Promise<void>((resolve) => setTimeout(resolve, 8));
        continue;
      }
      if (byte === 127 || byte === 8) {
        if (lineCursor > 0) {
          lineBuf = lineBuf.slice(0, lineCursor - 1) + lineBuf.slice(lineCursor);
          lineCursor--;
        }
        continue;
      }
      if (byte === 13 || byte === 10) {
        // Ignore empty/whitespace-only CR/LF lines to avoid delivering stale
        // terminal newlines as immediate blank stdin records.
        if (lineBuf.trim().length === 0) {
          lineBuf = '';
          lineCursor = 0;
          continue;
        }
        for (let i = 0; i < lineBuf.length; i++) lineQueue.push(lineBuf.charCodeAt(i));
        lineQueue.push(10);
        lineBuf = '';
        lineCursor = 0;
        return lineQueue.shift()!;
      }
      if (byte >= 32) {
        const ch = String.fromCharCode(byte);
        lineBuf = lineBuf.slice(0, lineCursor) + ch + lineBuf.slice(lineCursor);
        lineCursor++;
      }
    }
  };
}

function matchesExpected(actual: string, expected: string | RegExp): boolean {
  // wasi-run's byte-level capture joins fd_write chunks with '\n', leaving a
  // spurious trailing newline; compare with trailing newlines trimmed.
  const trimmed = actual.replace(/[\r\n]+$/, '');
  return typeof expected === 'string' ? trimmed === expected.replace(/[\r\n]+$/, '') : expected.test(trimmed);
}

function stringifyExpected(v: string | RegExp): string {
  return v instanceof RegExp ? v.toString() : JSON.stringify(v);
}

export interface IdeProps {
  title?: string;
  manifestUrl?: string;
  workspaceConfig?: WorkspaceConfig;
  workspaceUrl?: string;
   /** Grading-aware test plan. When present, a "Run Tests" button appears. */
  testPlan?: GradingPlan;
   /** 'public' strips hidden cases; 'full' runs all. Defaults to 'full'. */
  testMode?: 'public' | 'full';
   /** Score scale maximum (preview only). Defaults to 100. */
  maxScore?: number;
   /** Minimum score to pass (preview only). Defaults to 60. */
  passingScore?: number;
   /** Fired after ref.runTests() resolves with a structured TestReport. */
  onTestReport?: (report: import('@gameguild/emception-browser').EmceptionAPI extends { runTests: (...a: any[]) => Promise<infer R> } ? R : never) => void;
   /** Tee'd from tty.write — receives raw text written to the terminal (stdout path). */
  onStdout?: (chunk: string) => void;
   /** Tee'd from tty.writeError — receives raw text written to the terminal (stderr path). */
  onStderr?: (chunk: string) => void;
   /** Fired when a compile-and-run or test execution finishes, with the exit code. */
  onExecutionComplete?: (exitCode: number) => void;
  // ── T6 authoring extensions (in-IDE authoring surface) ───────────────
  /** UUID used by T8 to namespace VFS path + localStorage key per assignment. */
  assignmentToken?: string;
  /** When supplied, the in-header preset picker is ALWAYS rendered (overrides the
   *  default `!workspaceConfig && !workspaceUrl` gate). Each entry renders as an `<option>`. */
  presetOptions?: Array<{ value: string; label: string }>;
  /** Fired when the user changes the preset via the in-header picker. The internal
   *  `switchWorkspace(value)` is ALSO called when `value` matches a known PRESETS key. */
  onPresetChange?: (value: string) => void;
  /** v1 2-tier file metadata map. When supplied, FileExplorer renders per-row
   *  visibility `<Select>` + modifiable `<Switch>` controls. */
  fileMeta?: Record<string, { visibility: 'Public' | 'Private'; modifiable: boolean }>;
  /** Fired on a visibility/modifiable change with the merged patch. */
  onFileMetaChange?: (path: string, patch: Partial<{ visibility: 'Public' | 'Private'; modifiable: boolean }>) => void;
  /** Opaque tests-model prop — consumer narrows. Echoed back via `getAuthoredState()`. */
  tests?: unknown;
  /** Fired when the consumer-supplied tests editor mutates the model. */
  onTestsChange?: (next: unknown) => void;
  /** Slot rendered in a collapsible sidebar region (below FileExplorer) when supplied.
   *  Hosts the page-composed StandardTest + FunctionalTestGroup editors. */
  testsPanelSlot?: ReactNode;
}

/** Imperative handle exposed by `<Ide ref={...}>`. */
export interface IdeHandle {
  runTests: import('@gameguild/emception-browser').EmceptionAPI['runTests'];
  compileAndRun: import('@gameguild/emception-browser').EmceptionAPI['compileAndRun'];
  getFiles(): Promise<Array<{ path: string; content: string; encoding: 'text' | 'base64' }>>;
  setFiles(files: Array<{ path: string; content: string }>): Promise<void>;
  reset(): Promise<void>;
  /** Write a single file to reactive state + worker VFS via `client.writeFile`. */
  addFile(path: string, content: string): Promise<void>;
  /** Remove a single file from reactive state + worker VFS. */
  removeFile(path: string): Promise<void>;
  /** Apply v1 2-tier metadata; translates internally to emception's 3-tier FileEntry. */
  setFileMeta(path: string, meta: FileMetaInput): Promise<void>;
  /** Content-diff against the seeded workspace — returns edited + student-created files. */
  getModifiedFiles(): Promise<Array<{ path: string; content: string; encoding: 'text' }>>;
  /** Single snapshot for the page's save handler — files + fileMeta + tests + activePresetId. */
  getAuthoredState(): Promise<{
    files: Array<{ path: string; content: string; encoding: 'text' | 'base64' }>;
    fileMeta: Record<string, { visibility: 'Public' | 'Private'; modifiable: boolean }>;
    tests?: unknown;
    presetId: string;
  }>;
}

type WorkerBoot = Awaited<ReturnType<typeof bootInWorker>>;

export default forwardRef<IdeHandle, IdeProps>(function Ide({
  title = 'Emception',
  manifestUrl = DEFAULT_MANIFEST_URL,
  workspaceConfig,
  workspaceUrl,
  onTestReport,
  onStdout,
  onStderr,
  onExecutionComplete,
  testPlan,
  testMode = 'full',
  maxScore = 100,
  passingScore = 60,
  assignmentToken,
  presetOptions,
  onPresetChange,
  fileMeta,
  onFileMetaChange,
  tests,
  onTestsChange,
  testsPanelSlot,
}, ref) {
  const editorRef = useRef<Parameters<OnMount>[0] | null>(null);
  const monacoRef = useRef<Parameters<OnMount>[1] | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  /** Hidden holder div that keeps the <canvas> alive across dock group moves */
  const canvasHolderRef = useRef<HTMLDivElement | null>(null);
  /** The div inside whichever DockGroupPanel currently shows the canvas tab */
  const canvasHostElRef = useRef<HTMLDivElement | null>(null);
  const orchestratorRef = useRef<WorkerBoot | null>(null);
  const apiRef = useRef<import('@gameguild/emception-browser').EmceptionAPI | null>(null);
  const xtermRef = useRef<Terminal | null>(null);
  const terminalLogRef = useRef<HTMLPreElement | null>(null);
  /** Tracks blob URLs created for SDL output so they can be revoked on reset/unmount */
  const sdlBlobUrlsRef = useRef<string[]>([]);
  /** Tracks the injected SDL script element for cleanup on recompile/reset/unmount */
  const sdlScriptRef = useRef<HTMLScriptElement | null>(null);
  /** Tracks the live SDL3 Emscripten module so its RAF loop can be stopped */

  const sdlModuleRef = useRef<{ pauseMainLoop?: () => void } | null>(null);

  const onTestReportRef = useRef(onTestReport);
  onTestReportRef.current = onTestReport;
  const onStdoutRef = useRef(onStdout);
  onStdoutRef.current = onStdout;
  const onStderrRef = useRef(onStderr);
  onStderrRef.current = onStderr;
  const onExecutionCompleteRef = useRef(onExecutionComplete);
  onExecutionCompleteRef.current = onExecutionComplete;
  const lastExitCodeRef = useRef(0);
  const [lastReport, setLastReport] = useState<import('./TestResultsPanel').TestReport | null>(null);
  const [testRunning, setTestRunning] = useState(false);

  // Resolve the active workspace config: prop > fetched bundle > default preset
  const [activePresetId, setActivePresetId] = useState<string>(workspaceConfig?.id ?? DEFAULT_PRESET.id);
  // Mirror activePresetId for stale-closure-free reads inside useImperativeHandle.
  const activePresetIdRef = useRef(activePresetId);
  activePresetIdRef.current = activePresetId;
  // Mirror the new authoring props for getAuthoredState (useImperativeHandle deps don't include them).
  const propsRef = useRef<{ fileMeta?: IdeProps['fileMeta']; tests?: unknown; assignmentToken?: string }>({});
  propsRef.current = { fileMeta, tests, assignmentToken };
  const [fetchedConfig, setFetchedConfig] = useState<WorkspaceConfig | null>(null);
  const resolvedConfig = workspaceConfig ?? fetchedConfig ?? PRESETS[activePresetId] ?? DEFAULT_PRESET;
  const initialState = workspaceConfigToState(resolvedConfig);

  const [files, setFiles] = useState<Record<string, WorkspaceFile>>(initialState.files);
  const [selectedPath, setSelectedPath] = useState(resolvedConfig.layout.activeFile);
  const [expandedDirs, setExpandedDirs] = useState<Set<string>>(initialState.expandedDirs);
  const [openTabs, setOpenTabs] = useState<OpenTab[]>(initialState.openTabs);
  const [activeTabId, setActiveTabId] = useState(initialState.activeTabId);
  const [canvasIsRunning, setCanvasIsRunning] = useState(false);

  const [terminalTabs, setTerminalTabs] = useState<TerminalTab[]>([{ id: 'terminal-1', title: 'bash' }]);
  const [activeTerminalId, setActiveTerminalId] = useState('terminal-1');

  const [status, setStatus] = useState('Initializing...');
  const [isReady, setIsReady] = useState(false);
  const [terminalReady, setTerminalReady] = useState(false);
  const [executionPhase, setExecutionPhase] = useState<'idle' | 'compiling' | 'running'>('idle');
  const [bottomTab, setBottomTab] = useState<'terminal' | 'tests'>('terminal');
  /** Set to true by handleStop so the catch block in handleCompile knows it was intentional */
  const stoppedRef = useRef(false);
  /** Tracks latest files for use in callbacks that can't close over state */
  const filesRef = useRef(files);
  filesRef.current = files;
  /** Parallel FileMeta map (v1 2-tier shape) keyed by workspace path. */
  const fileMetaRef = useRef<Map<string, FileMeta>>(new Map());
  /** Snapshot of seeded content (path → content) used by getModifiedFiles. */
  const seededContentRef = useRef<Map<string, string>>(new Map());
  /** Bumped on every setFileMeta call to re-run the per-file readOnly effect. */
  const [metaVersion, bumpMetaVersion] = useState(0);
  // Expose filesRef for e2e tests so Playwright can verify file content was updated.
  // Guarded for SSR (Next.js server render has no window).
  if (typeof window !== 'undefined') {
    (window as unknown as Record<string, unknown>).__emception_filesRef__ = filesRef;
  }

  const fileTree = buildFileTree(Object.keys(files).filter((path) => path !== SDL_CANVAS_PATH && files[path]?.type !== 'canvas'));
  const activeTab = openTabs.find((t) => t.id === activeTabId) ?? openTabs[0] ?? null;
  const activeFile = activeTab
    ? (files[activeTab.path] ??
      (activeTab.type === 'canvas' || activeTab.path === SDL_CANVAS_PATH
        ? { path: SDL_CANVAS_PATH, type: 'canvas' as const, content: canvasIsRunning ? 'sdl' : '' }
        : null))
    : null;
  const activeFileName = activeFile ? (activeFile.path.split('/').filter(Boolean).pop() ?? '') : '';
  const groupTabs = (group: DockGroup) => openTabs.filter((t) => t.group === group);
  const hasRightGroup = groupTabs('right').length > 0;
  const hasBottomGroup = groupTabs('bottom').length > 0;

  useEffect(() => {
    try {
      // Read with the mount-time preset id — the initial workspace's key.
      const raw = window.localStorage.getItem(workspaceStorageKey(assignmentToken, activePresetIdRef.current));
      if (!raw) return;
      const parsed = JSON.parse(raw) as {
        files?: Record<string, WorkspaceFile>;
        selectedPath?: string;
        expandedDirs?: string[];
        openTabs?: OpenTab[];
        activeTabId?: string;
      };
      if (parsed.files && Object.keys(parsed.files).length > 0) {
        const nextFiles = Object.fromEntries(Object.entries(parsed.files).filter(([path, file]) => path !== SDL_CANVAS_PATH && file?.type !== 'canvas'));
        setFiles(nextFiles);
      }
      if (parsed.selectedPath) setSelectedPath(parsed.selectedPath);
      if (parsed.expandedDirs) setExpandedDirs(new Set(parsed.expandedDirs));
      if (parsed.openTabs && parsed.openTabs.length > 0) setOpenTabs(parsed.openTabs);
      if (parsed.activeTabId) setActiveTabId(parsed.activeTabId);
    } catch {
      /* ignore storage read errors */
    }
  }, []);

  useEffect(() => {
    try {
      // Exclude runtime-only canvas entries and image files (base64 data-URIs
      // would blow the ~5MB localStorage quota) from the persisted workspace.
      // Images are re-seeded from the saved assignment on reload.
      const filesToSave = Object.fromEntries(
        Object.entries(files).filter(([path, file]) => path !== SDL_CANVAS_PATH && file.type !== 'canvas' && file.type !== 'image'),
      );
      window.localStorage.setItem(
        // Ref read at effect-run time: applyWorkspace updates the ref and the
        // persisted state in the same commit, so read/write keys never split.
        workspaceStorageKey(assignmentToken, activePresetIdRef.current),
        JSON.stringify({
          files: filesToSave,
          selectedPath,
          expandedDirs: [...expandedDirs],
          openTabs,
          activeTabId,
        }),
      );
    } catch {
      /* ignore storage write errors */
    }
  }, [files, selectedPath, expandedDirs, openTabs, activeTabId]);

  // ── Fetch workspace from URL ──────────────────────────────────
  useEffect(() => {
    if (!workspaceUrl) return;
    let cancelled = false;
    (async () => {
      try {
        const resp = await fetch(workspaceUrl);
        if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
        const text = await resp.text();
        const config = parseWorkspaceBundle(text);
        if (cancelled) return;
        setFetchedConfig(config);
        const state = workspaceConfigToState(config);
        setFiles(state.files);
        setOpenTabs(state.openTabs);
        setActiveTabId(state.activeTabId);
        setExpandedDirs(state.expandedDirs);
        setSelectedPath(config.layout.activeFile);
      } catch (e) {
        console.error('[Emception:IDE] Failed to fetch workspace bundle:', e);
      }
    })();
    return () => {
      cancelled = true;
    };
    }, [workspaceUrl]);

     // ── Sync workspace files into the Worker VFS (/home/user) ─────
  const syncFilesToVfs = useCallback(async (filesToSync: Record<string, WorkspaceFile>) => {
    const orch = orchestratorRef.current;
    if (!orch) return;
    const P = '[Emception:IDE]';
    const { client } = orch;
    const enc = new TextEncoder();
    const textFiles = Object.values(filesToSync).filter((f) => f.type === 'text' && isTextFile(f.path));
    for (const file of textFiles) {
      const fsPath = toWorkspaceFsPath(file.path, propsRef.current.assignmentToken);
      await client.writeFile(fsPath, enc.encode(file.content));
      console.log(`${P} VFS sync: ${file.path} -> ${fsPath}`);
    }
    console.log(`${P} VFS sync complete (${textFiles.length} files)`);
  }, []);

      // ── Run Tests button handler ───────────────────────────────────
      const handleRunTests = useCallback(async () => {
    const orch = orchestratorRef.current;
    if (!orch || !testPlan || testPlan.cases.length === 0) return;
    const { client, tty } = orch;
    setTestRunning(true);
    setLastReport(null);
    const totalStart = performance.now();

    let report: import('./TestResultsPanel').TestReport;
    try {
      // Sync workspace text files to /home/user/<name> in the VFS so the compiler
      // reads the latest editor content.
      await syncFilesToVfs(filesRef.current);

      // All text C/C++ sources — the stdio path compiles the first; the
      // doctest path concatenates all of them into one combined TU.
      const studentSources = Object.values(filesRef.current).filter(
        (f) => f.type === 'text' && isSourceFile(f.path),
      );
      if (studentSources.length === 0) {
        throw new Error('No C/C++ source file found in workspace');
      }

      // ── Filter cases per testMode (before building — a doctest-only plan
      //    must not pay for, or fail on, the stdio binary build) ──
      const cases = testMode === 'public' ? testPlan.cases.filter((c) => !c.hidden) : testPlan.cases;
      const wasmPath = '/home/user/main.wasm';

      // ── stdio binary (clang -cc1 + wasm-ld) — same args as the working
      //    handleCompile direct path. Only built when stdio cases exist: a
      //    doctest-only workspace has no main() and --entry=main fails. ──
      if (cases.some((c) => c.kind === 'stdio')) {
        const mainSrc = studentSources[0];
        const sourceFsPath = toWorkspaceFsPath(mainSrc.path, propsRef.current.assignmentToken);
        const objPath = '/tmp/emception-test-main.o';

        // ── Compile (clang -cc1) — same args as the working handleCompile direct path ──
        tty.writeLine('\x1b[36m[tests] Compiling...\x1b[0m');
        const clangResult = await client.run(
          'clang',
          [
            'clang',
            '-cc1',
            '-triple',
            'wasm32-unknown-emscripten',
            '-emit-obj',
            '-O1',
            '-disable-free',
            '-clear-ast-before-backend',
            '-disable-llvm-verifier',
            '-discard-value-names',
            '-main-file-name',
            'main.cpp',
            '-mrelocation-model',
            'static',
            '-mframe-pointer=none',
            '-ffp-contract=on',
            '-fno-rounding-math',
            '-mconstructor-aliases',
            '-target-cpu',
            'generic',
            '-fvisibility=hidden',
            '-internal-isystem',
            '/usr/include/c++/v1',
            '-internal-isystem',
            '/usr/include/compat',
            '-internal-isystem',
            '/usr/lib/clang/23/include',
            '-resource-dir',
            '/usr/lib/clang/23',
            '-internal-isystem',
            '/usr/include',
            '-fdeprecated-macro',
            '-ferror-limit',
            '19',
            '-fgnuc-version=4.2.1',
            '-fcxx-exceptions',
            '-fexceptions',
            '-o',
            objPath,
            '-x',
            'c++',
            sourceFsPath,
          ],
          {
            cwd: '/home/user',
            onStdout: (t: string) => console.log(t),
            onStderr: (t: string) => {
              console.error(t);
              tty.writeError(t);
            },
          },
        );
        if (clangResult.exitCode !== 0) {
          throw new Error(`Compilation failed (exit ${clangResult.exitCode})`);
        }

        // ── Link (wasm-ld) ──
        tty.writeLine('\x1b[36m[tests] Linking...\x1b[0m');
        const lldResult = await client.run(
          'wasm-ld',
          [
            'wasm-ld',
            objPath,
            '-o',
            wasmPath,
            '-L/usr/lib/emscripten/cache-lib/wasm32-emscripten',
            '--entry=main',
            '--import-undefined',
            '--allow-undefined',
            '--export-table',
            '--table-base=1',
            '--export=__wasm_call_ctors',
            '-lc',
            '-ldlmalloc',
            '-lcompiler_rt',
            '-lc++-noexcept',
            '-lc++abi-noexcept',
            '-lsockets',
          ],
          {
            cwd: '/home/user',
            onStdout: (t: string) => console.log(t),
            onStderr: (t: string) => {
              console.error(t);
              tty.writeError(t);
            },
          },
        );
        if (lldResult.exitCode !== 0) {
          throw new Error(`Link failed (exit ${lldResult.exitCode})`);
        }
      }

      // ── Run each case: stdio via wasi-run, doctest via a combined TU ──
      const enc = new TextEncoder();
      const reportCases: import('./TestResultsPanel').TestCaseResult[] = [];
      let doctestIdx = 0;
      let doctestHeaderWritten = false;
      for (const test of cases) {
        const name = test.name ?? test.kind;
        const caseStart = performance.now();

        if (test.kind === 'doctest') {
          try {
            const harnessPath = test.sourceFiles?.[0];
            const harness = harnessPath
              ? testPlan.generatedFiles?.find((g) => g.path === harnessPath)
              : undefined;
            if (!harness) {
              reportCases.push({
                name,
                passed: false,
                durationMs: Math.round(performance.now() - caseStart),
                diagnostic: `Doctest case skipped: no generated harness file for '${harnessPath ?? '<none>'}' (kind 'doctest' not yet supported without generatedFiles).`,
              });
              continue;
            }

            // One combined TU per doctest case: student sources with their
            // main() renamed (doctest's main wins) + the harness with its
            // extern "C" decl stripped — same-TU linkage needs no C mangling.
            if (!doctestHeaderWritten) {
              await client.writeFile('/home/user/doctest.h', enc.encode(MINI_DOCTEST_H));
              doctestHeaderWritten = true;
            }
            const combinedName = `functional_combined_${doctestIdx}.cpp`;
            const combinedPath = `/home/user/${combinedName}`;
            const doctestWasmPath = `/home/user/functional_${doctestIdx}.wasm`;
            const doctestObjPath = `/tmp/functional_${doctestIdx}.o`;
            const combined = [
              '#include "doctest.h"',
              '#include <string>',
              '#define main gg_student_main_disabled',
              ...studentSources.map((f) => f.content),
              '#undef main',
              harness.content.replace(/^extern "C" .*;\s*$/m, ''),
            ].join('\n');
            await client.writeFile(combinedPath, enc.encode(combined));

            tty.writeLine(`\x1b[36m[tests] Compiling doctest ${name}...\x1b[0m`);
            const dClangResult = await client.run(
              'clang',
              [
                'clang',
                '-cc1',
                '-triple',
                'wasm32-unknown-emscripten',
                '-emit-obj',
                '-O1',
                '-disable-free',
                '-clear-ast-before-backend',
                '-disable-llvm-verifier',
                '-discard-value-names',
                '-main-file-name',
                combinedName,
                '-mrelocation-model',
                'static',
                '-mframe-pointer=none',
                '-ffp-contract=on',
                '-fno-rounding-math',
                '-mconstructor-aliases',
                '-target-cpu',
                'generic',
                '-fvisibility=hidden',
                '-internal-isystem',
                '/usr/include/c++/v1',
                '-internal-isystem',
                '/usr/include/compat',
                '-internal-isystem',
                '/usr/lib/clang/23/include',
                '-resource-dir',
                '/usr/lib/clang/23',
                '-internal-isystem',
                '/usr/include',
                '-fdeprecated-macro',
                '-ferror-limit',
                '19',
                '-fgnuc-version=4.2.1',
                '-fcxx-exceptions',
                '-fexceptions',
                '-o',
                doctestObjPath,
                '-x',
                'c++',
                combinedPath,
              ],
              {
                cwd: '/home/user',
                onStdout: (t: string) => console.log(t),
                onStderr: (t: string) => {
                  console.error(t);
                  tty.writeError(t);
                },
              },
            );
            if (dClangResult.exitCode !== 0) {
              throw new Error(`Doctest compilation failed (exit ${dClangResult.exitCode}): ${dClangResult.stderr}`);
            }

            tty.writeLine(`\x1b[36m[tests] Linking doctest ${name}...\x1b[0m`);
            const dLldResult = await client.run(
              'wasm-ld',
              [
                'wasm-ld',
                doctestObjPath,
                '-o',
                doctestWasmPath,
                '-L/usr/lib/emscripten/cache-lib/wasm32-emscripten',
                '--entry=main',
                '--import-undefined',
                '--allow-undefined',
                '--export-table',
                '--table-base=1',
                '--export=__wasm_call_ctors',
                '-lc',
                '-ldlmalloc',
                '-lcompiler_rt',
                '-lc++-noexcept',
                '-lc++abi-noexcept',
                '-lsockets',
              ],
              {
                cwd: '/home/user',
                onStdout: (t: string) => console.log(t),
                onStderr: (t: string) => {
                  console.error(t);
                  tty.writeError(t);
                },
              },
            );
            if (dLldResult.exitCode !== 0) {
              throw new Error(`Doctest link failed (exit ${dLldResult.exitCode}): ${dLldResult.stderr}`);
            }

            const result = await client.run('wasi-run', ['wasi-run', doctestWasmPath], {
              cwd: '/home/user',
            });
            const parsed = parseMiniDoctest(result.stdout);
            let diagnostic: string | undefined;
            if (parsed.status === 'crash') {
              diagnostic = `Doctest binary crashed before printing a summary:\n${parsed.failures.join('\n')}`;
            } else if (parsed.status === 'failure') {
              diagnostic =
                parsed.failures.length > 0
                  ? parsed.failures.join('\n')
                  : `Doctest reported ${parsed.casesFailed} failed test case(s).`;
            }
            reportCases.push({
              name,
              passed: parsed.status === 'success',
              durationMs: Math.round(performance.now() - caseStart),
              diagnostic,
            });
          } catch (err) {
            reportCases.push({
              name,
              passed: false,
              durationMs: Math.round(performance.now() - caseStart),
              diagnostic: err instanceof Error ? err.message : String(err),
            });
          }
          doctestIdx++;
          continue;
        }

        if (test.kind !== 'stdio') {
          reportCases.push({
            name,
            passed: false,
            durationMs: 0,
            diagnostic: `Test kind '${test.kind}' not yet supported by the direct-compile test runner.`,
          });
          continue;
        }

        try {
          // Tool-runner stdin contract: a line must end with '\n' before the
          // feeder returns null (mirrors presets.ts makeStdinFeeder).
          const rawStdin = test.stdin ?? '';
          const stdinBytes = enc.encode(rawStdin.endsWith('\n') ? rawStdin : `${rawStdin}\n`);
          let stdinIdx = 0;
          const result = await client.run('wasi-run', ['wasi-run', wasmPath], {
            cwd: '/home/user',
            stdin: () => (stdinIdx >= stdinBytes.length ? null : stdinBytes[stdinIdx++]),
          });

          const stdoutOk =
            test.expectedStdout === undefined ? true : matchesExpected(result.stdout, test.expectedStdout);
          const stderrOk =
            test.expectedStderr === undefined ? true : matchesExpected(result.stderr, test.expectedStderr);
          const exitOk = test.expectedExit === undefined ? true : result.exitCode === test.expectedExit;
          const passed = stdoutOk && stderrOk && exitOk;

          let diagnostic: string | undefined;
          if (!passed) {
            const parts: string[] = [];
            if (!stdoutOk && test.expectedStdout !== undefined) {
              parts.push(
                `stdout mismatch:\n  expected: ${stringifyExpected(test.expectedStdout)}\n  actual:   ${JSON.stringify(result.stdout)}`,
              );
            }
            if (!stderrOk && test.expectedStderr !== undefined) {
              parts.push(
                `stderr mismatch:\n  expected: ${stringifyExpected(test.expectedStderr)}\n  actual:   ${JSON.stringify(result.stderr)}`,
              );
            }
            if (!exitOk && test.expectedExit !== undefined) {
              parts.push(`exit code mismatch: expected ${test.expectedExit}, got ${result.exitCode}`);
            }
            diagnostic = parts.join('\n');
          }

          reportCases.push({
            name,
            passed,
            durationMs: Math.round(performance.now() - caseStart),
            diagnostic,
          });
        } catch (err) {
          reportCases.push({
            name,
            passed: false,
            durationMs: Math.round(performance.now() - caseStart),
            diagnostic: err instanceof Error ? err.message : String(err),
          });
        }
      }

      const passedCount = reportCases.filter((c) => c.passed).length;
      report = {
        passed: passedCount,
        failed: reportCases.length - passedCount,
        totalDurationMs: Math.round(performance.now() - totalStart),
        cases: reportCases,
      };
      setLastReport(report);
      try {
        onTestReportRef.current?.(report);
      } catch {
        /* swallow */
      }
      setStatus(`Tests complete (${report.passed}/${report.cases.length} passed)`);
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err);
      setStatus(`Test error: ${message}`);
      tty.writeError(`\x1b[31m${message}\x1b[0m`);
      report = {
        passed: 0,
        failed: 1,
        totalDurationMs: Math.round(performance.now() - totalStart),
        cases: [{ name: 'compile', passed: false, durationMs: 0, diagnostic: message }],
      };
      setLastReport(report);
    } finally {
      setTestRunning(false);
    }
    }, [testPlan, testMode, syncFilesToVfs]);

  // ── Apply a workspace config to reactive state + Worker VFS ─────
  const applyWorkspace = useCallback(
    async (config: WorkspaceConfig, presetId: string) => {
      const P = '[Emception:IDE]';
      console.log(`${P} applying workspace "${presetId}" (${config.label})`);

      // Stop SDL3 loop if running + reset canvas state
      const sdlMod = sdlModuleRef.current;
      if (sdlMod) {
        try {
          sdlMod.pauseMainLoop?.();
        } catch {
          /* ignore */
        }
        sdlModuleRef.current = null;
      }
      sdlScriptRef.current?.remove();
      sdlScriptRef.current = null;
      sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
      sdlBlobUrlsRef.current = [];
      const canvas = canvasRef.current;
      if (canvas) {
        delete canvas.dataset.sdlRunning;
        canvas.style.display = 'none';
      }
      setCanvasIsRunning(false);
      setExecutionPhase('idle');

      stoppedRef.current = false;
      setActivePresetId(presetId);
      const state = workspaceConfigToState(config);
      setFiles(state.files);
      setOpenTabs(state.openTabs);
      setActiveTabId(state.activeTabId);
      setExpandedDirs(state.expandedDirs);
      setSelectedPath(config.layout.activeFile);

      // Dispose stale Monaco models from the OLD workspace after React re-renders.
      // We defer disposal so @monaco-editor/react can cleanly unmount its model
      // reference first (the DockGroupPanel remounts via key={activePresetId}).
      const mc = monacoRef.current;
      if (mc) {
        const oldModels = mc.editor.getModels().slice();
        queueMicrotask(() => {
          for (const m of oldModels) {
            try {
              m.dispose();
            } catch {
              /* already disposed */
            }
          }
        });
      }

      if (orchestratorRef.current) {
        orchestratorRef.current.tty.writeLine(`\x1b[32mSwitched to workspace: ${config.label}\x1b[0m`);
        // Sync new workspace files into VFS so /home/user is populated immediately
        await syncFilesToVfs(state.files);
      }
    },
    [syncFilesToVfs],
  );

  // ── Switch workspace preset (internal PRESETS registry) ─────────
  const switchWorkspace = useCallback(
    async (presetId: string) => {
      const preset = PRESETS[presetId];
      if (!preset) return;
      const P = '[Emception:IDE]';
      console.log(`${P} ===== WORKSPACE SWITCH: "${activePresetId}" → "${presetId}" =====`);
      stoppedRef.current = true;

      // Reset VFS in the Worker to clear stale build artifacts from the previous workspace
      if (orchestratorRef.current) {
        const { client, tty } = orchestratorRef.current;
        tty.clear();
        tty.writeLine(`\x1b[33mSwitching workspace...\x1b[0m`);
        try {
          console.log(`${P} Resetting Worker VFS (clearing /tmp and /home/user)...`);
          await client.resetVfs();
          console.log(`${P} Worker VFS reset complete`);
        } catch (err) {
          console.warn(`${P} VFS reset failed, continuing:`, err);
        }
      }

      await applyWorkspace(preset, presetId);
      console.log(`${P} ===== WORKSPACE SWITCH COMPLETE =====`);
    },
    [activePresetId, applyWorkspace],
  );

  // ── React to external workspaceConfig prop changes ──────────────
  // Parent-driven switches (e.g. the coding-definition page re-seeding on
  // onPresetChange) land here when their id diverges from the tracked preset.
  // The ref-dedupe keeps an in-IDE pick that echoes back through the parent
  // from re-applying the same config.
  useEffect(() => {
    if (!workspaceConfig) return;
    if (workspaceConfig.id === activePresetIdRef.current) return;
    void applyWorkspace(workspaceConfig, workspaceConfig.id);
  }, [workspaceConfig, applyWorkspace]);

  const handleBootTerminalReady = useCallback((term: Terminal) => {
    xtermRef.current = term;
    (window as Window & { __xterm__?: Terminal }).__xterm__ = term;
    term.writeln('\x1b[32mWelcome to the Browser C/C++ Toolchain!\x1b[0m');
    term.writeln('Booting system...');
    setTerminalReady(true);
  }, []);

  const doBootstrap = useCallback(
    async (isMounted: () => boolean = () => true) => {
      const xterm = xtermRef.current;
      if (!xterm) return;
      setIsReady(false);
      setStatus('Booting toolchain...');
      try {
        // Mirror ALL xterm output to a hidden DOM element for Playwright E2E tests.
        // Must be patched BEFORE bootInWorker so the MiniShell banner (sent by the
        // Worker before the 'booted' reply) is also captured in the log.
        // eslint-disable-next-line no-control-regex
        const stripAnsi = (s: string) => s.replace(/\u001b\[[\d;]*m/g, '');
        const log = terminalLogRef;
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const xtermAny = xterm as any;
        if (!xtermAny.__emceptionLogPatched) {
          const origXtermWriteln = xtermAny.writeln.bind(xterm);
          xtermAny.writeln = (data: string | Uint8Array, callback?: () => void) => {
            origXtermWriteln(data, callback);
            if (log.current && typeof data === 'string') log.current.textContent += stripAnsi(data) + '\n';
          };
          const origXtermWrite = xtermAny.write.bind(xterm);
          xtermAny.write = (data: string | Uint8Array, callback?: () => void) => {
            origXtermWrite(data, callback);
            if (log.current && typeof data === 'string') log.current.textContent += stripAnsi(data);
          };
          xtermAny.__emceptionLogPatched = true;
        }

        const result = await bootInWorker(manifestUrl, xterm);
        if (!isMounted()) {
          result.client.terminate();
          return;
        }
        // Patch tty.clear to also clear the mirror log so each compile run starts fresh.
        const origClear = result.tty.clear.bind(result.tty);
        result.tty.clear = () => {
          origClear();
          if (log.current) log.current.textContent = '';
        };
        const origTtyWrite = result.tty.write.bind(result.tty);
        result.tty.write = (data: string) => {
          origTtyWrite(data);
          try { onStdoutRef.current?.(data); } catch { /* swallow callback errors */ }
        };
        const origTtyWriteError = result.tty.writeError.bind(result.tty);
        result.tty.writeError = (data: string) => {
          origTtyWriteError(data);
          try { onStderrRef.current?.(data); } catch { /* swallow callback errors */ }
        };
        orchestratorRef.current = result;
        apiRef.current = wrapWorkerClient(result.client);
        // Expose worker client on window for E2E / debug access
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        (window as any).__emception_client__ = result.client;
        // Sync current workspace files into VFS so /home/user is populated on boot
        await syncFilesToVfs(filesRef.current);
        setStatus('Ready');
        setIsReady(true);
        xterm.writeln('\x1b[32mSystem Ready.\x1b[0m');
        xterm.writeln('Click \x1b[1m▶\x1b[0m to compile & run.');
      } catch (err) {
        if (!isMounted()) return;
        setStatus(`Error: ${err instanceof Error ? err.message : String(err)}`);
        xterm.writeln(`\x1b[31mBoot failed: ${err}\x1b[0m`);
      }
    },
    [manifestUrl, syncFilesToVfs],
  );

  useEffect(() => {
    if (!terminalReady || !xtermRef.current) return;
    let mounted = true;
    doBootstrap(() => mounted);
    return () => {
      mounted = false;
      orchestratorRef.current?.client.terminate();
      orchestratorRef.current = null;
      apiRef.current = null;
    };
  }, [terminalReady, manifestUrl, doBootstrap]);

  // Revoke SDL blob URLs and remove injected script on unmount
  useEffect(() => {
    return () => {
      sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
      sdlScriptRef.current?.remove();
    };
  }, []);

  useImperativeHandle(ref, () => {
    // Strips any data-URI mime prefix; non-matching (utf8/raw) content passes through.
    const base64Prefix = /^data:[^;]*;base64,/;
    const getFiles = async () => {
      return Object.values(filesRef.current)
        .filter((f) => f.type === 'text' || f.type === 'image')
        .map((f) =>
          f.type === 'image' && base64Prefix.test(f.content)
            ? { path: f.path, content: f.content.replace(base64Prefix, ''), encoding: 'base64' as const }
            : { path: f.path, content: f.content, encoding: 'text' as const },
        );
    };
    return {
    runTests: async (plan) => {
      const api = apiRef.current;
      if (!api) throw new Error('Worker not booted yet');
      const report = await api.runTests(plan);
      try { onTestReportRef.current?.(report); } catch { /* swallow */ }
      return report;
    },
    compileAndRun: async (sourceOrFiles?, opts?) => {
      const api = apiRef.current;
      if (!api) throw new Error('Worker not booted yet');
      return api.compileAndRun(sourceOrFiles, opts);
    },
    getFiles,
    setFiles: async (newFiles) => {
      // Replace semantics: discard prior state, seed fresh workspace + snapshot.
      const replaced: Record<string, WorkspaceFile> = {};
      const snapshot = new Map<string, string>();
      const newTabs: OpenTab[] = [];
      for (const { path, content } of newFiles) {
        replaced[path] = { path, type: 'text', content };
        snapshot.set(path, content);
        newTabs.push({ id: `tab:${path}`, path, type: 'text', group: 'main' });
      }
      const newActiveTabId = newTabs[0]?.id ?? '';
      setFiles(replaced);
      filesRef.current = replaced;
      setOpenTabs(newTabs);
      setActiveTabId(newActiveTabId);
      setSelectedPath(newTabs[0]?.path ?? '');
      seededContentRef.current = snapshot;
      fileMetaRef.current = new Map();
      bumpMetaVersion((v) => v + 1);
      await syncFilesToVfs(replaced);
    },
    reset: async () => {
      await orchestratorRef.current?.client.resetVfs();
    },
    addFile: async (path, content) => {
      setFiles((prev) => ({ ...prev, [path]: { path, type: 'text', content } }));
      filesRef.current = { ...filesRef.current, [path]: { path, type: 'text', content } };
      const orch = orchestratorRef.current;
      if (orch) {
        const enc = new TextEncoder();
        await orch.client.writeFile(toWorkspaceFsPath(path, propsRef.current.assignmentToken), enc.encode(content));
      }
    },
    removeFile: async (path) => {
      setFiles((prev) => {
        const next = { ...prev };
        delete next[path];
        return next;
      });
      const nextRef = { ...filesRef.current };
      delete nextRef[path];
      filesRef.current = nextRef;
      fileMetaRef.current.delete(path);
      // WorkerClient exposes no per-file delete; resetVfs+reseed in setFiles/addFile
      // is the authoritative path. ponytail: per-file VFS delete if WorkerClient grows one.
    },
    setFileMeta: async (path, meta) => {
      const prev = fileMetaRef.current.get(path) ?? { visibility: 'Public' as const, modifiable: true };
      const merged: FileMeta = {
        visibility: meta.visibility ?? prev.visibility,
        modifiable: meta.modifiable ?? prev.modifiable,
      };
      fileMetaRef.current.set(path, merged);
      // v1 2-tier → emception 3-tier translation: 'Private' → 'hidden', modifiable:false → readonly:true.
      // 'solution' tier is never set from v1 per Must-NOT-Have.
      bumpMetaVersion((v) => v + 1);
    },
    getModifiedFiles: async () => {
      const seeded = seededContentRef.current;
      return Object.values(filesRef.current)
        .filter((f) => f.type === 'text' && f.content !== seeded.get(f.path))
        .map(({ path, content }) => ({ path, content, encoding: 'text' as const }));
    },
    getAuthoredState: async () => {
      const fm = propsRef.current.fileMeta;
      const fileMetaSnapshot: Record<string, { visibility: 'Public' | 'Private'; modifiable: boolean }> = {};
      if (fm) {
        for (const [path, meta] of Object.entries(fm)) {
          fileMetaSnapshot[path] = { visibility: meta.visibility, modifiable: meta.modifiable };
        }
      }
      return {
        files: await getFiles(),
        fileMeta: fileMetaSnapshot,
        tests: propsRef.current.tests,
        presetId: activePresetIdRef.current,
      };
    },
  }; }, [syncFilesToVfs]);

  const ensureOpenTab = useCallback(
    (path: string, group: DockGroup = 'main') => {
      const file = files[path];
      const type: TabType | null = file?.type ?? (path === SDL_CANVAS_PATH ? 'canvas' : null);
      if (!type) return;
      const id = `tab:${path}`;
      setOpenTabs((prev) => {
        const existing = prev.find((t) => t.id === id);
        if (existing) return prev.map((t) => (t.id === id ? { ...t, group } : t));
        return [...prev, { id, path, type, group }];
      });
      setActiveTabId(id);
    },
    [files],
  );

  const closeTab = useCallback((tabId: string) => {
    setOpenTabs((prev) => {
      const idx = prev.findIndex((t) => t.id === tabId);
      if (idx === -1) return prev;
      const next = prev.filter((t) => t.id !== tabId);
      setActiveTabId((cur) => {
        if (cur !== tabId) return cur;
        return next[idx]?.id ?? next[idx - 1]?.id ?? next[0]?.id ?? '';
      });
      return next;
    });
  }, []);

  const moveTabToGroup = useCallback((tabId: string, group: DockGroup) => {
    setOpenTabs((prev) => prev.map((t) => (t.id === tabId ? { ...t, group } : t)));
    setActiveTabId(tabId);
  }, []);

  /** Reorder a tab by placing it just before `beforeTabId` in the openTabs array. */
  const reorderTab = useCallback((tabId: string, beforeTabId: string) => {
    setOpenTabs((prev) => {
      const srcIdx = prev.findIndex((t) => t.id === tabId);
      const dstIdx = prev.findIndex((t) => t.id === beforeTabId);
      if (srcIdx === -1 || dstIdx === -1 || srcIdx === dstIdx) return prev;
      const tab = { ...prev[srcIdx], group: prev[dstIdx].group };
      const next = prev.filter((_, i) => i !== srcIdx);
      const insertIdx = next.findIndex((t) => t.id === beforeTabId);
      next.splice(insertIdx, 0, tab);
      return next;
    });
    setActiveTabId(tabId);
  }, []);

  /** Called by DockGroupPanel when a canvas host div mounts/unmounts.
   *  Reparents the persistent <canvas> element into the new host. */
  const handleCanvasHost = useCallback((el: HTMLDivElement | null) => {
    canvasHostElRef.current = el;
    const canvas = canvasRef.current;
    if (!canvas) return;
    if (el) {
      // Move the canvas into the new host panel
      el.prepend(canvas);
      canvas.style.display = canvas.dataset.sdlRunning ? 'block' : 'none';
    } else {
      // Host unmounted — park canvas back in the hidden holder
      canvasHolderRef.current?.appendChild(canvas);
    }
  }, []);

  // Safety net: if handleCanvasHost fired before canvasRef was set (rare timing
  // edge on initial mount), reparent once both refs are available.
  useLayoutEffect(() => {
    const host = canvasHostElRef.current;
    const canvas = canvasRef.current;
    if (host && canvas && canvas.parentElement !== host) {
      host.prepend(canvas);
      canvas.style.display = canvas.dataset.sdlRunning ? 'block' : 'none';
    }
  });

  // 10MB assignment budget — backend validator enforces the authoritative limit.
  const MAX_ASSIGNMENT_BYTES = 10_000_000;

  /** Reads each uploaded image as a base64 data-URI and stores it under /user.
   *  Rejects batches whose estimated serialized size would exceed 10MB. */
  const handleUploadFiles = useCallback(async (uploads: File[]) => {
    if (uploads.length === 0) return;
    // Base64 inflates ~4/3 → estimate 1.4x the raw file size.
    const existingBytes = Object.values(filesRef.current).reduce((sum, f) => sum + f.content.length, 0);
    const incomingBytes = uploads.reduce((sum, f) => sum + f.size, 0);
    if (existingBytes + incomingBytes * 1.4 > MAX_ASSIGNMENT_BYTES) {
      const msg = `Upload rejected: assignment would exceed 10MB (current ${(existingBytes / 1e6).toFixed(1)}MB + ${(incomingBytes / 1e6).toFixed(1)}MB of images)`;
      setStatus('Upload rejected: assignment would exceed 10MB');
      const tty = orchestratorRef.current?.tty;
      if (tty) tty.writeError(`\x1b[31m${msg}\x1b[0m`);
      else xtermRef.current?.writeln(`\x1b[31m${msg}\x1b[0m`);
      return;
    }
    let stored = 0;
    for (const file of uploads) {
      let dataUrl: string;
      try {
        dataUrl = await new Promise<string>((resolve, reject) => {
          const reader = new FileReader();
          reader.onload = () => resolve(String(reader.result));
          reader.onerror = () => reject(reader.error ?? new Error('FileReader failed'));
          reader.readAsDataURL(file);
        });
      } catch (err) {
        setStatus(`Upload failed: ${file.name}`);
        orchestratorRef.current?.tty.writeError(`Upload failed: ${file.name}: ${err}`);
        continue;
      }
      const base = `/user/${file.name}`;
      const dot = base.lastIndexOf('.');
      const stem = dot > 0 ? base.slice(0, dot) : base;
      const ext = dot > 0 ? base.slice(dot) : '';
      let path = base;
      let n = 2;
      while (filesRef.current[path]) {
        path = `${stem}-${n}${ext}`;
        n++;
      }
      filesRef.current = { ...filesRef.current, [path]: { path, type: 'image' as const, content: dataUrl } };
      setFiles(filesRef.current);
      setSelectedPath(path);
      setOpenTabs((prev) =>
        prev.some((t) => t.id === `tab:${path}`)
          ? prev
          : [...prev, { id: `tab:${path}`, path, type: 'image' as const, group: 'main' as const }],
      );
      setActiveTabId(`tab:${path}`);
      stored++;
    }
    if (stored > 0) setStatus(`Uploaded ${stored} image file${stored === 1 ? '' : 's'}`);
  }, []);

  const createFile = useCallback(
    (kind: TabType) => {
      const baseDir = '/user';
      const defaultName = kind === 'canvas' ? 'new-canvas' : kind === 'image' ? 'new-image.svg' : 'new-file.cpp';
      const input = window.prompt(`Create new ${kind} file`, `${baseDir}/${defaultName}`);
      if (!input) return;
      const path = input.startsWith('/') ? input : `${baseDir}/${input}`;
      if (files[path]) {
        window.alert(`File already exists: ${path}`);
        return;
      }
      const content =
        kind === 'image' ? DEFAULT_IMAGE : kind === 'canvas' ? '' : kind === 'text' && path.endsWith('.h') ? '#pragma once\n\n' : '// New source file\n';
      setFiles((prev) => ({ ...prev, [path]: { path, type: kind, content } }));
      const parts = path.split('/').filter(Boolean);
      if (parts.length > 1) {
        setExpandedDirs((prev) => {
          const next = new Set(prev);
          let cur = '';
          for (let i = 0; i < parts.length - 1; i++) {
            cur += `/${parts[i]}`;
            next.add(cur);
          }
          return next;
        });
      }
      setSelectedPath(path);
      ensureOpenTab(path, 'main');
    },
    [files, ensureOpenTab],
  );

  const renameSelectedFile = useCallback(() => {
    if (!selectedPath || !files[selectedPath]) return;
    const nextPath = window.prompt('Rename file', selectedPath);
    if (!nextPath || nextPath === selectedPath) return;
    if (files[nextPath]) {
      window.alert(`File already exists: ${nextPath}`);
      return;
    }
    const norm = nextPath.startsWith('/') ? nextPath : `/${nextPath}`;
    const file = files[selectedPath];
    setFiles((prev) => {
      const clone = { ...prev };
      delete clone[selectedPath];
      clone[norm] = { ...file, path: norm };
      return clone;
    });
    setOpenTabs((prev) => prev.map((tab) => (tab.path === selectedPath ? { ...tab, path: norm, id: `tab:${norm}` } : tab)));
    setSelectedPath(norm);
    setActiveTabId((cur) => (cur === `tab:${selectedPath}` ? `tab:${norm}` : cur));
  }, [selectedPath, files]);

  const deleteSelectedFile = useCallback(() => {
    if (!selectedPath || !files[selectedPath]) return;
    if (!window.confirm(`Delete ${selectedPath}?`)) return;
    setFiles((prev) => {
      const c = { ...prev };
      delete c[selectedPath];
      return c;
    });
    closeTab(`tab:${selectedPath}`);
    setSelectedPath(Object.keys(files).find((p) => p !== selectedPath) ?? '');
  }, [selectedPath, files, closeTab]);

  const resetWorkspace = useCallback(async () => {
    if (!window.confirm('Reset the workspace to the default demo files and layout?')) return;
    const P = '[Emception:IDE]';
    console.log(`${P} ===== WORKSPACE RESET =====`);
    // Stop SDL3 loop if running
    const sdlMod = sdlModuleRef.current;
    if (sdlMod) {
      try {
        sdlMod.pauseMainLoop?.();
      } catch {
        /* ignore */
      }
      sdlModuleRef.current = null;
    }
    // Revoke any outstanding SDL blob URLs and remove injected script
    sdlScriptRef.current?.remove();
    sdlScriptRef.current = null;
    sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
    sdlBlobUrlsRef.current = [];
    // Reset canvas state
    const resetCanvas = canvasRef.current;
    if (resetCanvas) {
      delete resetCanvas.dataset.sdlRunning;
      resetCanvas.style.display = 'none';
    }
    setCanvasIsRunning(false);
    setExecutionPhase('idle');
    stoppedRef.current = true;

    // Reset VFS in the Worker to clear stale build artifacts
    if (orchestratorRef.current) {
      try {
        console.log(`${P} Resetting Worker VFS...`);
        await orchestratorRef.current.client.resetVfs();
        console.log(`${P} Worker VFS reset complete`);
      } catch (err) {
        console.warn(`${P} VFS reset failed, continuing:`, err);
      }
    }

    stoppedRef.current = false;
    const state = workspaceConfigToState(resolvedConfig);
    setFiles(state.files);
    setSelectedPath(resolvedConfig.layout.activeFile);
    setExpandedDirs(state.expandedDirs);
    setOpenTabs(state.openTabs);
    setActiveTabId(state.activeTabId);
    setTerminalTabs([{ id: 'terminal-1', title: 'bash' }]);
    setActiveTerminalId('terminal-1');
    if (orchestratorRef.current) {
      orchestratorRef.current.tty.clear();
      orchestratorRef.current.tty.writeLine('\x1b[32mWorkspace reset.\x1b[0m');
    } else {
      xtermRef.current?.clear();
      xtermRef.current?.writeln('\x1b[32mWorkspace reset.\x1b[0m');
    }
  }, [resolvedConfig]);

  const createTerminalTab = useCallback(() => {
    setTerminalTabs((prev) => {
      const nextId = `terminal-${prev.length + 1}`;
      const next = [...prev, { id: nextId, title: `bash ${prev.length + 1}` }];
      setActiveTerminalId(nextId);
      return next;
    });
  }, []);

  const closeTerminalTab = useCallback((tabId: string) => {
    setTerminalTabs((prev) => {
      if (prev.length === 1) return prev;
      const idx = prev.findIndex((t) => t.id === tabId);
      if (idx === -1) return prev;
      const next = prev.filter((t) => t.id !== tabId);
      setActiveTerminalId((cur) => {
        if (cur !== tabId) return cur;
        return (next[idx] ?? next[idx - 1] ?? next[0])?.id ?? 'terminal-1';
      });
      return next;
    });
  }, []);

  const handleEditorDidMount: OnMount = (editor, monaco) => {
    editorRef.current = editor;
    monacoRef.current = monaco;
    // Expose for e2e tests (e.g. Playwright can read file content via window.monaco)
    (window as unknown as Record<string, unknown>).monaco = monaco;
    syncMonacoModels();
  };

  const handleEditorChange = useCallback((path: string, value: string) => {
    setFiles((prev) => {
      const next = { ...prev, [path]: { ...prev[path], content: value } };
      // Update ref immediately so handleCompile (which reads filesRef.current)
      // always sees the latest content even if React hasn't re-rendered yet.
      filesRef.current = next;
      return next;
    });
  }, []);

  const syncMonacoModels = useCallback(() => {
    const monaco = monacoRef.current;
    if (!monaco) return;

    const desiredPaths = new Set<string>();

    const applyReadOnly = (model: { updateOptions: (opts: { readOnly: boolean }) => void }, path: string) => {
      const meta = fileMetaRef.current.get(path);
      model.updateOptions({ readOnly: meta?.modifiable === false });
    };

    const ensureModel = (path: string) => {
      const file = filesRef.current[path];
      if (!file || file.type !== 'text') return;

      desiredPaths.add(path);
      const uri = monaco.Uri.file(path);
      const existing = monaco.editor.getModel(uri);

      if (!existing) {
        const created = monaco.editor.createModel(file.content, inferLanguage(path), uri);
        applyReadOnly(created, path);
        return;
      }

      if (existing.getValue() !== file.content && activeTabId !== `tab:${path}`) {
        existing.setValue(file.content);
      }
      applyReadOnly(existing, path);
    };

    for (const tab of openTabs) ensureModel(tab.path);
    if (selectedPath) ensureModel(selectedPath);

    const activePath = activeTabId.startsWith('tab:') ? activeTabId.slice(4) : null;
    for (const model of monaco.editor.getModels()) {
      if (model.uri.scheme !== 'file') continue;
      const modelPath = model.uri.path;
      if (!desiredPaths.has(modelPath) && modelPath !== activePath) {
        model.dispose();
      }
    }
  }, [activeTabId, openTabs, selectedPath, metaVersion]);

  useEffect(() => {
    syncMonacoModels();
  }, [files, syncMonacoModels]);

  // Expose for e2e tests so Playwright can update file content directly in React state.
  // Guarded for SSR (Next.js server render has no window).
  if (typeof window !== 'undefined') {
    (window as unknown as Record<string, unknown>).__setFileContent = handleEditorChange;
  }

  const teardownSdlRuntime = useCallback(() => {
    const sdlMod = sdlModuleRef.current;
    if (!sdlMod && !sdlScriptRef.current && sdlBlobUrlsRef.current.length === 0) return false;

    try {
      sdlMod?.pauseMainLoop?.();
    } catch {
      /* ignore */
    }

    sdlModuleRef.current = null;
    sdlScriptRef.current?.remove();
    sdlScriptRef.current = null;
    sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
    sdlBlobUrlsRef.current = [];

    const canvas = canvasRef.current;
    if (canvas) {
      canvas.getContext('2d')?.clearRect(0, 0, canvas.width, canvas.height);
      delete canvas.dataset.sdlRunning;
      canvas.style.display = 'none';
    }

    setCanvasIsRunning(false);
    return true;
  }, []);

  const handleCompile = async () => {
    if (!orchestratorRef.current || !activeFile || activeFile.type !== 'text') return;
    stoppedRef.current = false;
    lastExitCodeRef.current = 0;
    setExecutionPhase('compiling');
    setActiveTerminalId('terminal-1');
    const P = '[Emception:IDE]';
    // Always return keyboard focus to the editor when done, regardless of exit path.
    // xterm captures document-level keydown when it has focus, blocking Monaco input.
    const restoreEditorFocus = () => {
      editorRef.current?.focus();
    };
    const tTotal = performance.now();
    const { client, tty } = orchestratorRef.current;
    // Read from filesRef.current (updated immediately by handleEditorChange)
    // instead of the render-time `files` closure, so e2e __setFileContent
    // updates are always picked up even before React re-renders.
    const currentFiles = filesRef.current;

    if (resolvedConfig.run.type === 'sdl3-canvas' && executionPhase === 'running') {
      teardownSdlRuntime();
    }

    const textFiles = Object.values(currentFiles).filter((f) => f.type === 'text' && isTextFile(f.path));

    // Determine which source file to compile/run
    const entryPoint = resolvedConfig.compile.sourceDetect?.entryPoint;
    const compileTarget = isSourceFile(activeFile.path)
      ? activeFile.path
      : entryPoint && currentFiles[entryPoint]
        ? entryPoint
        : textFiles.find((f) => isSourceFile(f.path))?.path;

    try {
      // ── Sync all text files to VFS ──────────────────────────────
      tty.clear();
      console.log(`${P} COMPILE & RUN START`);
      const enc = new TextEncoder();
      for (const file of textFiles) {
        const fsPath = toWorkspaceFsPath(file.path, propsRef.current.assignmentToken);
        await client.writeFile(fsPath, enc.encode(file.content));
        console.log(`${P} Synced ${file.path} -> ${fsPath}`);
      }

      const t0 = performance.now();
      const runType = resolvedConfig.run.type;

      // ── Python script path ──────────────────────────────────────
      if (runType === 'python-script') {
        const pyFile = compileTarget ?? entryPoint ?? '/user/main.py';
        const fsPath = toWorkspaceFsPath(pyFile, propsRef.current.assignmentToken);
        const args = resolvedConfig.run.args ? resolveArgs(resolvedConfig.run.args, fsPath) : ['python3', fsPath];
        setStatus('Running Python...');
        tty.writeLine(`\x1b[36mRunning ${pyFile}...\x1b[0m`);
        setExecutionPhase('running');
        const lineBufferedStdin = makeLineBufferedStdin(tty);
        await client.run(args[0], args, {
          cwd: resolvedConfig.compile.cwd ?? '/home/user',
          onStdout: (t: string) => {
            tty.write(t.replace(/\n/g, '\r\n'));
          },
          onStderr: (t: string) => {
            tty.write(`\x1b[31m${t.replace(/\n/g, '\r\n')}\x1b[0m`);
          },
          stdin: lineBufferedStdin,
        });
        setExecutionPhase('idle');
        setStatus(`Done (${((performance.now() - tTotal) / 1000).toFixed(1)}s)`);
        return;
      }

      // ── CMake build path ────────────────────────────────────────
      if (runType === 'cmake-build') {
        if (!compileTarget && !entryPoint) {
          setExecutionPhase('idle');
          setStatus('No source file found');
          tty.writeError('No source file found in workspace.');
          return;
        }
        setStatus('CMake configure...');
        tty.writeLine('\x1b[36mCMake configure...\x1b[0m');
        const configArgs = resolvedConfig.compile.args;
        const configResult = await client.run(configArgs[0], configArgs, {
          cwd: resolvedConfig.compile.cwd ?? '/home/user',
          onStdout: (t: string) => {
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            tty.writeError(t);
          },
        });
        if (configResult.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus('CMake configure failed');
          tty.writeLine(`\x1b[31mCMake configure failed (exit ${configResult.exitCode})\x1b[0m`);
          return;
        }
        setStatus('Ninja build...');
        tty.writeLine('\x1b[36mNinja build...\x1b[0m');
        const buildDir = configArgs.includes('-B') ? configArgs[configArgs.indexOf('-B') + 1] : '/home/user/build';
        const ninjaResult = await client.run('ninja', ['ninja', '-C', buildDir], {
          cwd: resolvedConfig.compile.cwd ?? '/home/user',
          onStdout: (t: string) => {
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            tty.writeError(t);
          },
        });
        const duration = ((performance.now() - t0) / 1000).toFixed(2);
        if (ninjaResult.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus(`Build failed (${duration}s)`);
          tty.writeLine(`\x1b[31mNinja build failed (exit ${ninjaResult.exitCode})\x1b[0m`);
          return;
        }
        tty.writeLine(`\x1b[32mBuild successful in ${duration}s\x1b[0m`);
        tty.writeLine('Running...');
        const runArgs = resolvedConfig.run.args ?? ['wasi-run', resolvedConfig.compile.output];
        setExecutionPhase('running');
        const lineBufferedStdin = makeLineBufferedStdin(tty);
        await client.run(runArgs[0], runArgs, {
          cwd: resolvedConfig.compile.cwd ?? '/home/user',
          onStdout: (t: string) => {
            tty.write(t.replace(/\n/g, '\r\n'));
          },
          onStderr: (t: string) => {
            tty.write(`\x1b[31m${t.replace(/\n/g, '\r\n')}\x1b[0m`);
          },
          stdin: lineBufferedStdin,
        });
        setExecutionPhase('idle');
        setStatus(`Done (${((performance.now() - tTotal) / 1000).toFixed(1)}s)`);
        return;
      }

      // ── SDL3 canvas path ────────────────────────────────────────
      if (runType === 'sdl3-canvas') {
        if (!compileTarget) {
          setExecutionPhase('idle');
          setStatus('No compilable source file found');
          tty.writeError('No .c/.cpp source file found in workspace.');
          return;
        }
        setStatus('Compiling...');
        tty.writeLine(`Compiling ${compileTarget}...`);
        tty.writeLine('\x1b[36mSDL3 detected \u2014 compiling object...\x1b[0m');

        const sourceFsPath = toWorkspaceFsPath(compileTarget, propsRef.current.assignmentToken);
        const sdlObjPath = '/tmp/emception-sdl-main.o';
        const wasmPath = resolvedConfig.compile.output || '/home/user/main.wasm';

        // Compile with clang -cc1 directly (driver mode silently exits in
        // browser because cc1 cannot be spawned as a subprocess; cc1_main is
        // linked into clang.wasm so direct -cc1 invocation works in-process).
        // Includes mirror what the driver would inject for SDL3 + Emscripten
        // sysroot, plus the shipped fakesdl/compat/SDL3 headers.
        const sdlCompile = await client.run(
          'clang',
          [
            'clang',
            '-cc1',
            '-triple',
            'wasm32-unknown-emscripten',
            '-emit-obj',
            '-O1',
            '-disable-free',
            '-clear-ast-before-backend',
            '-disable-llvm-verifier',
            '-discard-value-names',
            '-main-file-name',
            'main.cpp',
            '-mrelocation-model',
            'static',
            '-mframe-pointer=none',
            '-ffp-contract=on',
            '-fno-rounding-math',
            '-mconstructor-aliases',
            '-target-cpu',
            'generic',
            '-fvisibility=hidden',
            '-internal-isystem',
            '/usr/include/c++/v1',
            '-internal-isystem',
            '/usr/include/compat',
            '-internal-isystem',
            '/usr/lib/clang/23/include',
            '-internal-isystem',
            '/usr/include/fakesdl',
            '-internal-isystem',
            '/usr/include/SDL3',
            '-resource-dir',
            '/usr/lib/clang/23',
            '-internal-isystem',
            '/usr/include',
            '-fdeprecated-macro',
            '-ferror-limit',
            '19',
            '-fgnuc-version=4.2.1',
            '-fcxx-exceptions',
            '-fexceptions',
            '-o',
            sdlObjPath,
            '-x',
            'c++',
            sourceFsPath,
          ],
          {
            cwd: resolvedConfig.compile.cwd ?? '/home/user',
            onStdout: (t: string) => {
              console.log(t);
              tty.writeLine(t);
            },
            onStderr: (t: string) => {
              console.error(t);
              tty.writeError(t);
            },
          },
        );

        const sdlDuration = ((performance.now() - t0) / 1000).toFixed(2);
        if (sdlCompile.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus(`SDL3 compilation failed (${sdlDuration}s)`);
          tty.writeLine(`\x1b[31mSDL3 compile step failed (exit ${sdlCompile.exitCode})\x1b[0m`);
          return;
        }

        tty.writeLine('\x1b[36mSDL3 linking (wasm-ld)...\x1b[0m');

        const sdlLink = await client.run(
          'wasm-ld',
          [
            'wasm-ld',
            sdlObjPath,
            '-o',
            wasmPath,
            '-L/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten',
            '-L/usr/lib/emscripten/src/lib',
            '/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten/crt1.o',
            '/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten/libSDL3.a',
            '--no-entry',
            '--import-undefined',
            '--allow-undefined',
            '--export-if-defined=SDL_AppInit',
            '--export-if-defined=SDL_AppIterate',
            '--export-if-defined=SDL_AppEvent',
            '--export-if-defined=SDL_AppQuit',
            '--export-table',
            '--table-base=1',
            '-z',
            'stack-size=65536',
            '-lGL-getprocaddr',
            '-lal',
            '-lhtml5',
            '-lstubs',
            '-lc',
            '-ldlmalloc',
            '-lcompiler_rt',
            '-lc++-noexcept',
            '-lc++abi-noexcept',
            '-lsockets',
          ],
          {
            cwd: resolvedConfig.compile.cwd ?? '/home/user',
            onStdout: (t: string) => {
              console.log(t);
              tty.writeLine(t);
            },
            onStderr: (t: string) => {
              console.error(t);
              tty.writeError(t);
            },
          },
        );

        if (sdlLink.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus(`SDL3 compilation failed (${sdlDuration}s)`);
          tty.writeLine(`\x1b[31mSDL3 link step failed (exit ${sdlLink.exitCode})\x1b[0m`);
          return;
        }

        tty.writeLine(`\x1b[32mSDL3 compiled in ${sdlDuration}s — loading...\x1b[0m`);

        // Read the compiled WASM binary from the VFS.
        const wasmBytes = await client.getFile(wasmPath);
        if (!wasmBytes) {
          setExecutionPhase('idle');
          tty.writeError('main.wasm not found — emcc may have failed to produce it alongside main.js');
          return;
        }

        // Read the pre-built SDL3 JS runtime shell from the VFS
        const runtimeBytes = await client.getFile('/usr/lib/emscripten/sdl3-runtime.mjs');
        if (!runtimeBytes) {
          setExecutionPhase('idle');
          tty.writeError('sdl3-runtime.mjs not found in VFS — rebuild the CDN bundle');
          return;
        }

        // Mark canvas tab as SDL-active (keeps the canvas element visible)
        setCanvasIsRunning(true);
        ensureOpenTab(SDL_CANVAS_PATH, 'right');
        setActiveTabId(`tab:${compileTarget}`);

        // Wait for React to flush + browser to paint so canvasRef.current is ready
        await new Promise<void>((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));

        const canvas = canvasRef.current;
        if (!canvas) {
          setExecutionPhase('idle');
          tty.writeError('SDL canvas element not found — open the SDL Canvas tab first');
          return;
        }

        // Flag the canvas as SDL-active so the host callback shows it
        canvas.dataset.sdlRunning = 'true';
        canvas.style.display = 'block';

        // Initialize to the SDL demo's expected render size so the runtime
        // attaches to a correctly sized target immediately.
        canvas.width = 800;
        canvas.height = 600;

        // Revoke previous SDL blob URLs
        sdlScriptRef.current?.remove();
        sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
        sdlBlobUrlsRef.current = [];

        // Patch sdl3-runtime.mjs before creating the blob URL.
        let runtimeText = new TextDecoder().decode(runtimeBytes instanceof Uint8Array ? runtimeBytes : new Uint8Array(runtimeBytes as ArrayBuffer));
        const emAsmFallback = (varName: string) =>
          `if(!ASM_CONSTS[${varName}]){var _s=UTF8ToString(${varName});` + `ASM_CONSTS[${varName}]=eval("(function($0,$1,$2,$3,$4,$5,$6,$7,$8,$9){"+_s+"})");}`;
        runtimeText = runtimeText
          .replace('var _main,_SDL_free,', 'var _free,_main,_SDL_free,')
          .replace('_malloc=wasmExports["malloc"]', '_malloc=wasmExports["malloc"]||wasmExports["SDL_malloc"]')
          .replace(
            '_SDL_free=Module["_SDL_free"]=wasmExports["SDL_free"]',
            '_SDL_free=Module["_SDL_free"]=wasmExports["SDL_free"];_free=wasmExports["free"]||_SDL_free',
          )
          .replace(
            'var stringToNewUTF8=str=>{var size=lengthBytesUTF8(str)+1;var ret=_malloc(size)',
            'var stringToNewUTF8=str=>{var size=lengthBytesUTF8(str)+1;var allocFn=_malloc||_SDL_malloc;var ret=allocFn(size)',
          );
        const ORIG_RUNEMASM = 'var runEmAsmFunction=(code,sigPtr,argbuf)=>{var args=readEmAsmArgs(sigPtr,argbuf);return ASM_CONSTS[code](...args)}';
        const ORIG_RUNMTEMASM =
          'var runMainThreadEmAsm=(emAsmAddr,sigPtr,argbuf,sync)=>{var args=readEmAsmArgs(sigPtr,argbuf);return ASM_CONSTS[emAsmAddr](...args)}';
        if (runtimeText.includes(ORIG_RUNEMASM)) {
          runtimeText = runtimeText.replace(
            ORIG_RUNEMASM,
            `var runEmAsmFunction=(code,sigPtr,argbuf)=>{var args=readEmAsmArgs(sigPtr,argbuf);${emAsmFallback('code')}return ASM_CONSTS[code](...args)}`,
          );
        } else {
          tty.writeError('sdl3-runtime patch: runEmAsmFunction not found — EM_ASM may fail');
        }
        if (runtimeText.includes(ORIG_RUNMTEMASM)) {
          runtimeText = runtimeText.replace(
            ORIG_RUNMTEMASM,
            `var runMainThreadEmAsm=(emAsmAddr,sigPtr,argbuf,sync)=>{var args=readEmAsmArgs(sigPtr,argbuf);${emAsmFallback('emAsmAddr')}return ASM_CONSTS[emAsmAddr](...args)}`,
          );
        }

        // Patch: Scope SDL3's keyboard event handlers to the canvas element.
        const ORIG_KEY_HANDLER = 'var keyEventHandlerFunc=e=>{var keyEventData=JSEvents.keyEvent';
        const PATCHED_KEY_HANDLER = 'var keyEventHandlerFunc=e=>{if(Module["canvas"]&&e.target!==Module["canvas"])return;var keyEventData=JSEvents.keyEvent';
        if (runtimeText.includes(ORIG_KEY_HANDLER)) {
          runtimeText = runtimeText.replace(ORIG_KEY_HANDLER, PATCHED_KEY_HANDLER);
        } else {
          tty.writeError('sdl3-runtime patch: keyEventHandlerFunc not found — keyboard may be captured globally');
        }

        // Create a blob URL for the ES6 runtime module so we can dynamically import it
        const runtimeBlob = new Blob([new TextEncoder().encode(runtimeText)], { type: 'application/javascript' });
        const runtimeUrl = URL.createObjectURL(runtimeBlob);
        sdlBlobUrlsRef.current = [runtimeUrl];

        // Dynamically import the MODULARIZE ES6 factory and instantiate with WASM + canvas
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const { default: createSDL3Module } = await import(/* webpackIgnore: true */ /* @vite-ignore */ runtimeUrl as any);

        const wasmMemory: WebAssembly.Memory | null = null;
        const wasiStubs = makeWasiStubs(
          () => wasmMemory,
          (s: string) => tty.writeLine(s),
        );

        let sdlLoadOk = true;
        let sdlCallbackFns: { init?: (appstate: number, argc: number, argv: number) => number; iterate?: (appstate: number) => number } | null = null;
        const moduleTimeout = new Promise<never>((_, reject) => setTimeout(() => reject(new Error('SDL3 module load timeout (30s)')), 30_000));
        const sdlMod = await Promise.race([
          createSDL3Module({
            canvas: canvas,
            keyboardListeningElement: canvas,
            wasmBinary: wasmBytes,
            locateFile: (filename: string) => filename,
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            instantiateWasm(info: any, receiveInstance: (inst: WebAssembly.Instance) => void) {
              const env = {
                ...info.env,
                emscripten_notify_memory_growth: () => { },
              };
              const imports = { ...info, env, wasi_snapshot_preview1: wasiStubs };
              tty.writeLine('\x1b[90mSDL3: instantiating WASM…\x1b[0m');
              WebAssembly.instantiate(new Uint8Array(wasmBytes as unknown as ArrayBuffer), imports)
                .then((result) => {
                  tty.writeLine('\x1b[90mSDL3: WASM ok, patching exports…\x1b[0m');
                  const origExports = result.instance.exports;
                  // Capture callback exports directly from raw WASM exports so
                  // we can drive callback-only SDL apps even when glue doesn't
                  // surface these as Module methods.
                  // eslint-disable-next-line @typescript-eslint/no-explicit-any
                  const raw = origExports as any;
                  sdlCallbackFns = {
                    init: typeof raw.SDL_AppInit === 'function' ? raw.SDL_AppInit.bind(raw) : undefined,
                    iterate: typeof raw.SDL_AppIterate === 'function' ? raw.SDL_AppIterate.bind(raw) : undefined,
                  };
                  const patchedExports =
                    typeof origExports['__wasm_call_ctors'] === 'function' && typeof (origExports as Record<string, unknown>)['main'] === 'function'
                      ? origExports
                      : new Proxy(origExports, {
                        get(target, prop) {
                          if (prop === '__wasm_call_ctors' && !(prop in target)) return () => { };
                          if ((prop === 'main' || prop === '_main') && !(prop in target)) {
                            const noOpMain = () => 0;
                            // eslint-disable-next-line @typescript-eslint/no-explicit-any
                            (noOpMain as any).__emceptionNoop = true;
                            return noOpMain;
                          }
                          // eslint-disable-next-line @typescript-eslint/no-explicit-any
                          return (target as any)[prop];
                        },
                      });
                  const patchedInstance = new Proxy(result.instance, {
                    get(target, prop) {
                      if (prop === 'exports') return patchedExports;
                      // eslint-disable-next-line @typescript-eslint/no-explicit-any
                      return (target as any)[prop];
                    },
                  });
                  tty.writeLine('\x1b[90mSDL3: calling receiveInstance…\x1b[0m');
                  receiveInstance(patchedInstance);
                })
                .catch((err: unknown) => {
                  tty.writeError(`SDL3 WASM instantiation failed: ${err}`);
                  setStatus('SDL3 load failed');
                });
              return {};
            },
            print: (line: string) => tty.writeLine(line),
            printErr: (line: string) => tty.writeError(line),
          }),
          moduleTimeout,
        ]).catch((e: unknown) => {
          sdlLoadOk = false;
          tty.writeError(`SDL3 module error: ${e}`);
          setStatus('SDL3 load failed');
          return null;
        });

        if (!sdlLoadOk) {
          setExecutionPhase('idle');
          return;
        }

        // Some SDL callback builds linked with --no-entry need an explicit kick
        // from JS to start their lifecycle callbacks/main loop.
        try {
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          const sdlAny = sdlMod as any;
          let started = false;
          const callbackFns = sdlCallbackFns as {
            init?: (appstate: number, argc: number, argv: number) => number;
            iterate?: (appstate: number) => number;
          } | null;
          const wasmFns = sdlAny?.asm ?? sdlAny?.wasmExports ?? {};
          const appInit = callbackFns?.init ?? sdlAny?._SDL_AppInit ?? sdlAny?.SDL_AppInit ?? wasmFns?._SDL_AppInit ?? wasmFns?.SDL_AppInit;
          const appIterate = callbackFns?.iterate ?? sdlAny?._SDL_AppIterate ?? sdlAny?.SDL_AppIterate ?? wasmFns?._SDL_AppIterate ?? wasmFns?.SDL_AppIterate;

          // Prefer explicit callback lifecycle startup when exported.
          if (typeof appInit === 'function' && typeof appIterate === 'function') {
            tty.writeLine('\x1b[90mSDL3: starting callback lifecycle loop…\x1b[0m');
            const initResult = appInit(0, 0, 0);
            if (initResult !== 0) {
              tty.writeLine(`\x1b[33mSDL3: AppInit returned non-continue (${initResult})\x1b[0m`);
            }
            const step = () => {
              if (!sdlModuleRef.current) return;
              try {
                const iterateResult = appIterate(0);
                // SDL_APP_CONTINUE == 0
                if (iterateResult !== 0) {
                  tty.writeLine(`\x1b[90mSDL3: iterate requested stop (${iterateResult})\x1b[0m`);
                  return;
                }
              } catch (err: unknown) {
                const msg = err instanceof Error ? err.message : String(err);
                if (msg !== 'unwind') tty.writeError(`SDL3 iterate error: ${msg}`);
                return;
              }
              requestAnimationFrame(step);
            };
            requestAnimationFrame(step);
            started = true;
          } else if (typeof sdlAny?.callMain === 'function') {
            tty.writeLine('\x1b[90mSDL3: calling main entry…\x1b[0m');
            sdlAny.callMain([]);
            started = true;
          } else if (typeof sdlAny?._main === 'function' && !sdlAny._main.__emceptionNoop) {
            tty.writeLine('\x1b[90mSDL3: invoking _main…\x1b[0m');
            sdlAny._main(0, 0);
            started = true;
          } else if (typeof sdlAny?._SDL_main === 'function') {
            tty.writeLine('\x1b[90mSDL3: invoking _SDL_main…\x1b[0m');
            sdlAny._SDL_main(0, 0);
            started = true;
          }

          if (!started) tty.writeLine('\x1b[33mSDL3 warning: no runnable entry/callback exports found.\x1b[0m');
        } catch (e: unknown) {
          const msg = e instanceof Error ? e.message : String(e);
          // Emscripten may throw "unwind" to enter async main loop; ignore that.
          if (msg !== 'unwind') {
            tty.writeError(`SDL3 entry invocation error: ${msg}`);
          }
        }

        sdlModuleRef.current = sdlMod as { pauseMainLoop?: () => void } | null;
        setExecutionPhase('running');
        setStatus(`SDL3 done (${((performance.now() - tTotal) / 1000).toFixed(1)}s) — running`);
        tty.writeLine('\x1b[32mSDL3 rendering in canvas tab →\x1b[0m');
        return;
      }

      // ── Standard WASI terminal path ─────────────────────────────
      if (!compileTarget) {
        setExecutionPhase('idle');
        setStatus('No compilable source file found');
        tty.writeError('No source file found in workspace.');
        return;
      }
      setStatus('Compiling...');
      tty.writeLine(`Compiling ${compileTarget}...`);

      // ── Direct clang+wasm-ld fast path ──────────────────────────
      // When compile.args is empty, bypass the emcc Python pipeline entirely
      // and use direct clang → wasm-ld (same approach as SDL3 path).  This
      // eliminates ~13s of overhead: Python boot, 8970-file pre-warm,
      // wasm-opt asyncify pass, and wasm-emscripten-finalize.
      // The WASI runtime handles blocking stdin via SharedArrayBuffer +
      // Atomics.wait, so -sASYNCIFY is not needed.
      const useDirectPath = resolvedConfig.compile.args.length === 0 && resolvedConfig.run.type === 'wasi-terminal';

      if (useDirectPath) {
        const sourceFsPath = toWorkspaceFsPath(compileTarget, propsRef.current.assignmentToken);
        const objPath = '/tmp/emception-terminal-main.o';
        const wasmPath = resolvedConfig.compile.output || '/home/user/main.wasm';

        tty.writeLine('\x1b[36mDirect compile (clang -cc1)...\x1b[0m');
        // Use clang -cc1 directly (not driver mode). The driver tries to
        // posix_spawn cc1 as a subprocess which fails silently in the browser
        // (no fork/posix_spawn in emscripten libc), causing clang to exit 0
        // in ~35ms with no output. cc1_main is linked into clang.wasm so the
        // frontend runs in-process when invoked with -cc1 directly.
        // Args mirror the native `clang++ -###` driver dump for an equivalent
        // command line, with the resource-dir set to our shipped /usr/lib/clang/23
        // and compat shims enabled for libc++ headers such as xlocale.h.
        const clangResult = await client.run(
          'clang',
          [
            'clang',
            '-cc1',
            '-triple',
            'wasm32-unknown-emscripten',
            '-emit-obj',
            '-O1',
            '-disable-free',
            '-clear-ast-before-backend',
            '-disable-llvm-verifier',
            '-discard-value-names',
            '-main-file-name',
            'main.cpp',
            '-mrelocation-model',
            'static',
            '-mframe-pointer=none',
            '-ffp-contract=on',
            '-fno-rounding-math',
            '-mconstructor-aliases',
            '-target-cpu',
            'generic',
            '-fvisibility=hidden',
            '-internal-isystem',
            '/usr/include/c++/v1',
            '-internal-isystem',
            '/usr/include/compat',
            '-internal-isystem',
            '/usr/lib/clang/23/include',
            '-resource-dir',
            '/usr/lib/clang/23',
            '-internal-isystem',
            '/usr/include',
            '-fdeprecated-macro',
            '-ferror-limit',
            '19',
            '-fgnuc-version=4.2.1',
            '-fcxx-exceptions',
            '-fexceptions',
            '-o',
            objPath,
            '-x',
            'c++',
            sourceFsPath,
          ],
          {
            cwd: resolvedConfig.compile.cwd ?? '/home/user',
            onStdout: (t: string) => {
              console.log(t);
              tty.writeLine(t);
            },
            onStderr: (t: string) => {
              console.error(t);
              tty.writeError(t);
            },
          },
        );

        if (clangResult.exitCode !== 0) {
          const dur = ((performance.now() - t0) / 1000).toFixed(2);
          setExecutionPhase('idle');
          setStatus(`Compilation failed (${dur}s)`);
          tty.writeLine(`\x1b[31mCompilation failed (exit ${clangResult.exitCode})\x1b[0m`);
          return;
        }

        tty.writeLine('\x1b[36mLinking (wasm-ld)...\x1b[0m');
        const lldResult = await client.run(
          'wasm-ld',
          [
            'wasm-ld',
            objPath,
            '-o',
            wasmPath,
            '-L/usr/lib/emscripten/cache-lib/wasm32-emscripten',
            '--entry=main',
            '--import-undefined',
            '--allow-undefined',
            '--export-table',
            '--table-base=1',
            '--export=__wasm_call_ctors',
            '-lc',
            '-ldlmalloc',
            '-lcompiler_rt',
            '-lc++-noexcept',
            '-lc++abi-noexcept',
            '-lsockets',
          ],
          {
            cwd: resolvedConfig.compile.cwd ?? '/home/user',
            onStdout: (t: string) => {
              console.log(t);
              tty.writeLine(t);
            },
            onStderr: (t: string) => {
              console.error(t);
              tty.writeError(t);
            },
          },
        );

        if (lldResult.exitCode !== 0) {
          const dur = ((performance.now() - t0) / 1000).toFixed(2);
          setExecutionPhase('idle');
          setStatus(`Link failed (${dur}s)`);
          tty.writeLine(`\x1b[31mLinker step failed (exit ${lldResult.exitCode})\x1b[0m`);
          return;
        }

        const dur = ((performance.now() - t0) / 1000).toFixed(2);
        setStatus('Compilation successful');
        tty.writeLine(`\x1b[32mCompilation successful in ${dur}s\x1b[0m`);

        // Log output size for test verification
        const wasmFile = await client.getFile(wasmPath);
        const wasmSize = wasmFile ? wasmFile.length : 0;
        console.log(`${P} Compilation output: main.wasm=${wasmSize}B`);

        // Go directly to run phase
        tty.writeLine('Running...');
        const lineBufferedStdin = makeLineBufferedStdin(tty);
        const runArgs = resolvedConfig.run.args ?? ['wasi-run', wasmPath];
        setExecutionPhase('running');
        await client.run(runArgs[0], runArgs, {
          cwd: resolvedConfig.compile.cwd ?? '/home/user',
          onStdout: (t: string) => {
            tty.write(t.replace(/\n/g, '\r\n'));
          },
          onStderr: (t: string) => {
            tty.write(`\x1b[31m${t.replace(/\n/g, '\r\n')}\x1b[0m`);
          },
          stdin: lineBufferedStdin,
        });
        setExecutionPhase('idle');
        setStatus(`Done (${((performance.now() - tTotal) / 1000).toFixed(1)}s)`);
        return;
      }

      // ── emcc path (Python-based pipeline) ───────────────────────
      const compileArgs =
        resolvedConfig.compile.args.length > 0
          ? resolveArgs(resolvedConfig.compile.args, toWorkspaceFsPath(compileTarget, propsRef.current.assignmentToken))
          : ['emcc', toWorkspaceFsPath(compileTarget, propsRef.current.assignmentToken), '-o', '/home/user/main.wasm', '-O2'];
      const result = await client.run(compileArgs[0], compileArgs, {
        cwd: resolvedConfig.compile.cwd ?? '/home/user',
        onStdout: (t: string) => {
          console.log(t);
          tty.writeLine(t);
        },
        onStderr: (t: string) => {
          console.error(t);
          tty.writeError(t);
        },
      });
      const duration = ((performance.now() - t0) / 1000).toFixed(2);
      if (result.exitCode !== 0) {
        setExecutionPhase('idle');
        setStatus('Compilation failed');
        tty.writeLine(`\x1b[31mCompilation failed (exit ${result.exitCode})\x1b[0m`);
        return;
      }
      setStatus('Compilation successful');
      tty.writeLine(`\x1b[32mCompilation successful in ${duration}s\x1b[0m`);
      const wasmPath = resolvedConfig.compile.output || '/home/user/main.wasm';
      let wasmBytes = await client.getFile(wasmPath);

      // emcc may return before all subprocess-linked outputs are flushed to VFS.
      // Give the canonical artifact a short grace window before triggering fallback.
      if ((!wasmBytes || wasmBytes.length === 0) && resolvedConfig.run.type === 'wasi-terminal' && resolvedConfig.compile.tool === 'emcc') {
        const waitUntil = performance.now() + 12_000;
        while ((!wasmBytes || wasmBytes.length === 0) && performance.now() < waitUntil) {
          await new Promise<void>((resolve) => setTimeout(resolve, 120));
          wasmBytes = await client.getFile(wasmPath);
        }
      }

      if ((!wasmBytes || wasmBytes.length === 0) && resolvedConfig.run.type === 'wasi-terminal' && resolvedConfig.compile.tool === 'emcc') {
        tty.writeLine('\x1b[33mOutput artifact missing after emcc; attempting fallback link pipeline...\x1b[0m');
        const fallbackObj = '/tmp/emception-fallback-main.o';
        const sourceFsPath = toWorkspaceFsPath(compileTarget, propsRef.current.assignmentToken);

        const clangFallback = await client.run(
          'clang',
          [
            'clang',
            '--target=wasm32-unknown-emscripten',
            '--sysroot=/usr/lib/emscripten/cache/sysroot',
            '-Xclang',
            '-iwithsysroot/include/fakesdl',
            '-Xclang',
            '-iwithsysroot/include/compat',
            '-O2',
            '-c',
            sourceFsPath,
            '-o',
            fallbackObj,
          ],
          {
            cwd: resolvedConfig.compile.cwd ?? '/home/user',
            onStdout: (t: string) => tty.writeLine(t),
            onStderr: (t: string) => tty.writeError(t),
          },
        );

        if (clangFallback.exitCode === 0) {
          const crtPath = '/usr/lib/emscripten/cache-lib/wasm32-emscripten/crt1.o';
          const crtBytes = await client.getFile(crtPath);
          const magic = crtBytes
            ? Array.from((crtBytes as Uint8Array).slice(0, 8))
              .map((b: number) => b.toString(16).padStart(2, '0'))
              .join('')
            : 'none';
          tty.writeLine(`crt1.o probe: bytes=${crtBytes?.length ?? 0}, head=${magic}`);

          const lldFallback = await client.run(
            'wasm-ld',
            [
              'wasm-ld',
              fallbackObj,
              '-o',
              wasmPath,
              '-L/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten',
              '-L/usr/lib/emscripten/src/lib',
              '/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten/crt1.o',
              '--entry=main',
              '--import-undefined',
              '--allow-undefined',
              '--export-table',
              '--table-base=1',
              '--global-base=1024',
              '-z',
              'stack-size=65536',
              '-lGL-getprocaddr',
              '-lal',
              '-lhtml5',
              '-lstubs',
              '-lc',
              '-ldlmalloc',
              '-lcompiler_rt',
              '-lc++-noexcept',
              '-lc++abi-noexcept',
              '-lsockets',
            ],
            {
              cwd: resolvedConfig.compile.cwd ?? '/home/user',
              onStdout: (t: string) => tty.writeLine(t),
              onStderr: (t: string) => tty.writeError(t),
            },
          );

          if (lldFallback.exitCode !== 0) {
            tty.writeLine('\x1b[31mFallback linker step failed.\x1b[0m');
          }
        } else {
          tty.writeLine('\x1b[31mFallback compile step failed.\x1b[0m');
        }

        wasmBytes = await client.getFile(wasmPath);
      }
      console.log(`${P} Compilation output: main.wasm=${wasmBytes?.length ?? 0}`);
      tty.writeLine('Running...');
      const lineBufferedStdin = makeLineBufferedStdin(tty);
      const runArgs = resolvedConfig.run.args ?? ['wasi-run', resolvedConfig.compile.output || '/home/user/main.wasm'];
      setExecutionPhase('running');
      await client.run(runArgs[0], runArgs, {
        cwd: resolvedConfig.compile.cwd ?? '/home/user',
        onStdout: (t: string) => {
          tty.write(t.replace(/\n/g, '\r\n'));
        },
        onStderr: (t: string) => {
          tty.write(`\x1b[31m${t.replace(/\n/g, '\r\n')}\x1b[0m`);
        },
        stdin: lineBufferedStdin,
      });
      setExecutionPhase('idle');
      setStatus(`Done (${((performance.now() - tTotal) / 1000).toFixed(1)}s)`);
    } catch (e) {
      setExecutionPhase('idle');
      if (!stoppedRef.current) {
        console.error(`${P} Exception:`, e);
        setStatus('Error during execution');
        tty.writeError(String(e));
      }
    } finally {
      console.log(`${P} COMPILE & RUN COMPLETE`);
      try { onExecutionCompleteRef.current?.(lastExitCodeRef.current); } catch { /* swallow */ }
      restoreEditorFocus();
    }
  };

  const handleTest = async () => {
    const testConfig = resolvedConfig.test;
    if (!orchestratorRef.current || !testConfig) return;
    stoppedRef.current = false;
    setExecutionPhase('compiling');
    setActiveTerminalId('terminal-1');
    const restoreEditorFocus = () => {
      editorRef.current?.focus();
    };
    const { client, tty } = orchestratorRef.current;
    const tTotal = performance.now();
    try {
      tty.clear();
      tty.writeLine('\x1b[36mRunning tests...\x1b[0m');

      // Sync files to VFS
      const textFiles = Object.values(files).filter((f) => f.type === 'text' && isTextFile(f.path));
      const enc = new TextEncoder();
      for (const file of textFiles) {
        const fsPath = toWorkspaceFsPath(file.path, propsRef.current.assignmentToken);
        await client.writeFile(fsPath, enc.encode(file.content));
      }

      // Compile test if needed
      if (testConfig.compileArgs && testConfig.compileArgs.length > 0) {
        setStatus('Compiling tests...');
        const compileResult = await client.run(testConfig.tool, testConfig.compileArgs, {
          cwd: resolvedConfig.compile.cwd ?? '/home/user',
          onStdout: (t: string) => {
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            tty.writeError(t);
          },
        });
        if (compileResult.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus('Test compilation failed');
          tty.writeLine(`\x1b[31mTest compilation failed (exit ${compileResult.exitCode})\x1b[0m`);
          return;
        }
      }

      // Run tests
      setStatus('Running tests...');
      setExecutionPhase('running');
      const lineBufferedStdin = makeLineBufferedStdin(tty);
      const runResult = await client.run(testConfig.runArgs[0], testConfig.runArgs, {
        cwd: resolvedConfig.compile.cwd ?? '/home/user',
        onStdout: (t: string) => {
          tty.write(t.replace(/\n/g, '\r\n'));
        },
        onStderr: (t: string) => {
          tty.write(`\x1b[31m${t.replace(/\n/g, '\r\n')}\x1b[0m`);
        },
        stdin: lineBufferedStdin,
      });

      setExecutionPhase('idle');
      lastExitCodeRef.current = runResult.exitCode;
      const duration = ((performance.now() - tTotal) / 1000).toFixed(1);
      if (runResult.exitCode === 0) {
        setStatus(`Tests passed (${duration}s)`);
        tty.writeLine(`\x1b[32m✓ All tests passed (${duration}s)\x1b[0m`);
      } else {
        setStatus(`Tests failed (${duration}s)`);
        tty.writeLine(`\x1b[31m✗ Tests failed (exit ${runResult.exitCode}, ${duration}s)\x1b[0m`);
      }
    } catch (e) {
      setExecutionPhase('idle');
      if (!stoppedRef.current) {
        setStatus('Test error');
        tty.writeError(String(e));
      }
    } finally {
      try { onExecutionCompleteRef.current?.(lastExitCodeRef.current); } catch { /* swallow */ }
      restoreEditorFocus();
    }
  };

  const handleStop = async () => {
    if (executionPhase !== 'running') return;

    // SDL3 path: the RAF loop runs on the main thread; pause it and clean up.
    // The worker is idle (it only compiled), so we can keep it for re-compile.
    if (teardownSdlRuntime()) {
      setExecutionPhase('idle');
      setStatus('Stopped');
      xtermRef.current?.writeln('\x1b[33mExecution stopped.\x1b[0m');
      editorRef.current?.focus();
      return;
    }

    // WASI path: the worker is actively running wasi-run — terminate and reboot.
    if (!orchestratorRef.current) return;
    stoppedRef.current = true;
    const { client } = orchestratorRef.current;
    client.terminate();
    orchestratorRef.current = null;
    apiRef.current = null;
    setExecutionPhase('idle');
    setIsReady(false);
    setStatus('Stopped — rebooting...');
    xtermRef.current?.writeln('\x1b[33mExecution stopped.\x1b[0m');
    await doBootstrap();
    editorRef.current?.focus();
  };

  /** Shared resize-handle style for all PanelResizeHandle instances */
  const resizerStyle: React.CSSProperties = {
    width: 4,
    background: '#313244',
    cursor: 'col-resize',
    transition: 'background 0.15s',
  };
  const resizerVStyle: React.CSSProperties = { ...resizerStyle, width: '100%', height: 4, cursor: 'row-resize' };
  const canRecompileWhileRunning = executionPhase === 'running' && resolvedConfig.run.type === 'sdl3-canvas';
  const canCompile = isReady && activeFile?.type === 'text' && (executionPhase === 'idle' || canRecompileWhileRunning);
  const showCompileButton = executionPhase !== 'running' || canRecompileWhileRunning;

  return (
    <div className="emception-ide" style={{ display: 'flex', flexDirection: 'column', height: '100%', width: '100%', minHeight: 400, fontFamily: 'system-ui, sans-serif' }}>
      {/* Hidden log for Playwright E2E assertions — not visible to users */}
      <pre data-testid="terminal" ref={terminalLogRef} hidden aria-hidden="true" style={{ display: 'none' }} />
      {/* Hidden holder keeps the SDL <canvas> alive when no dock group hosts it.
          Rendered early so canvasRef is set before any DockGroupPanel host ref fires. */}
      <div ref={canvasHolderRef} style={{ position: 'absolute', width: 0, height: 0, overflow: 'hidden', pointerEvents: 'none' }}>
        <canvas id="canvas" data-testid="sdl-canvas" tabIndex={0} ref={canvasRef} style={{ width: '100%', height: '100%', display: 'none' }} />
      </div>
      {/* ── Title bar ── */}
      <header
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '0 1rem',
          height: 36,
          background: '#181825',
          borderBottom: '1px solid #313244',
          flexShrink: 0,
        }}
      >
        <h1 style={{ fontSize: '0.8rem', fontWeight: 600, color: '#cdd6f4', margin: 0, letterSpacing: '0.03em' }}>{title}</h1>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          {/* Workspace preset picker — visible when presetOptions supplied OR no workspaceConfig/workspaceUrl */}
          {((presetOptions && presetOptions.length > 0) || (!workspaceConfig && !workspaceUrl)) && (
            <select
              data-testid="workspace-picker"
              value={activePresetId}
              onChange={(e) => {
                const v = e.target.value;
                if (PRESETS[v]) switchWorkspace(v);
                onPresetChange?.(v);
              }}
              style={{
                height: 24,
                fontSize: '0.72rem',
                borderRadius: 4,
                border: '1px solid #45475a',
                background: '#1e1e2e',
                color: '#cdd6f4',
                cursor: 'pointer',
                padding: '0 0.4rem',
              }}
            >
              {(presetOptions && presetOptions.length > 0
                ? presetOptions
                : PRESET_IDS.map((id) => ({ value: id, label: PRESETS[id].label }))
              ).map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          )}
          <span data-testid="status" style={{ fontSize: '0.72rem', color: '#a6adc8' }}>
            {status}
          </span>
          {showCompileButton && (
            <button
              data-testid="compile-button"
              onClick={handleCompile}
              disabled={!canCompile}
              style={{
                height: 24,
                padding: '0 0.75rem',
                fontSize: '0.8rem',
                fontWeight: 500,
                borderRadius: 4,
                border: 'none',
                cursor: canCompile ? 'pointer' : 'not-allowed',
                background: canCompile ? '#a6e3a1' : '#313244',
                color: canCompile ? '#11111b' : '#585b70',
              }}
            >
              {resolvedConfig.run.type === 'python-script' ? '▶ Run' : canRecompileWhileRunning ? '↻' : '▶'}
            </button>
          )}
          {executionPhase === 'running' && (
            <button
              data-testid="stop-button"
              onClick={handleStop}
              style={{
                height: 24,
                padding: '0 0.75rem',
                fontSize: '0.8rem',
                fontWeight: 500,
                borderRadius: 4,
                border: 'none',
                cursor: 'pointer',
                background: '#f38ba8',
                color: '#11111b',
              }}
            >
              &#9632; Stop
            </button>
          )}
          {resolvedConfig.features.showTestButton && resolvedConfig.test && (
            <button
              data-testid="test-button"
              onClick={handleTest}
              disabled={executionPhase !== 'idle' || !isReady}
              style={{
                height: 24,
                padding: '0 0.75rem',
                fontSize: '0.8rem',
                fontWeight: 500,
                borderRadius: 4,
                border: 'none',
                cursor: executionPhase === 'idle' && isReady ? 'pointer' : 'not-allowed',
                background: executionPhase === 'idle' && isReady ? '#cba6f7' : '#313244',
                color: executionPhase === 'idle' && isReady ? '#11111b' : '#585b70',
              }}
            >
              ✓ Test
            </button>
          )}
          {testPlan && (
            <button
              data-testid="run-tests-button"
              onClick={handleRunTests}
              disabled={testRunning}
              style={{
                height: 24,
                padding: '0 0.75rem',
                fontSize: '0.8rem',
                fontWeight: 500,
                borderRadius: 4,
                border: 'none',
                cursor: testRunning ? 'not-allowed' : 'pointer',
                background: testRunning ? '#313244' : '#fab387',
                color: testRunning ? '#585b70' : '#11111b',
              }}
            >
              {testRunning ? 'Running…' : 'Run Tests'}
            </button>
          )}
          <button
            onClick={resetWorkspace}
            style={{
              height: 24,
              padding: '0 0.6rem',
              fontSize: '0.8rem',
              borderRadius: 4,
              border: '1px solid #45475a',
              cursor: 'pointer',
              background: 'transparent',
              color: '#a6adc8',
            }}
          >
            Reset
          </button>
        </div>
      </header>

      {/* ── Main body: sidebar | editor + terminal ── */}
      <PanelGroup direction="horizontal" style={{ flex: 1, overflow: 'hidden' }}>
        {/* Sidebar */}
        <Panel defaultSize={18} minSize={10} maxSize={40} style={{ overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
          <FileExplorer
            files={files}
            selectedPath={selectedPath}
            expandedDirs={expandedDirs}
            fileTree={fileTree}
            onSelectPath={setSelectedPath}
            onToggleDir={(path) =>
              setExpandedDirs((prev) => {
                const next = new Set(prev);
                if (next.has(path)) next.delete(path);
                else next.add(path);
                return next;
              })
            }
            onOpenTab={(path) => ensureOpenTab(path, 'main')}
            onCreateFile={createFile}
            onRename={renameSelectedFile}
            onDelete={deleteSelectedFile}
            onUploadFiles={handleUploadFiles}
            fileMeta={fileMeta}
            onFileMetaChange={onFileMetaChange}
          />
        </Panel>

        <PanelResizeHandle style={resizerStyle} />

        {/* Editor + terminal column */}
        <Panel style={{ display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
          <PanelGroup direction="vertical" style={{ flex: 1, overflow: 'hidden' }}>
            {/* Editor row: main (+ optional right panel) */}
            <Panel style={{ display: 'flex', overflow: 'hidden' }}>
              <PanelGroup direction="horizontal" style={{ flex: 1, overflow: 'hidden' }}>
                <Panel minSize={20} style={{ overflow: 'hidden' }}>
                  <DockGroupPanel
                    key={activePresetId}
                    group="main"
                    tabs={groupTabs('main')}
                    activeTabId={activeTabId}
                    files={files}
                    onCanvasHost={handleCanvasHost}
                    onSetActiveTab={setActiveTabId}
                    onCloseTab={closeTab}
                    onMoveTab={moveTabToGroup}
                    onReorderTab={reorderTab}
                    onEditorMount={handleEditorDidMount}
                    onEditorChange={handleEditorChange}
                    canvasIsRunning={canvasIsRunning}
                  />
                </Panel>

                {hasRightGroup && (
                  <>
                    <PanelResizeHandle style={resizerStyle} />
                    <Panel defaultSize={35} minSize={15} style={{ overflow: 'hidden' }}>
                      <DockGroupPanel
                        key={`${activePresetId}-right`}
                        group="right"
                        tabs={groupTabs('right')}
                        activeTabId={activeTabId}
                        files={files}
                        onCanvasHost={handleCanvasHost}
                        onSetActiveTab={setActiveTabId}
                        onCloseTab={closeTab}
                        onMoveTab={moveTabToGroup}
                        onReorderTab={reorderTab}
                        onEditorMount={handleEditorDidMount}
                        onEditorChange={handleEditorChange}
                        canvasIsRunning={canvasIsRunning}
                      />
                    </Panel>
                  </>
                )}
              </PanelGroup>
            </Panel>

            {hasBottomGroup && (
              <>
                <PanelResizeHandle style={resizerVStyle} />
                <Panel defaultSize={25} minSize={10} style={{ overflow: 'hidden' }}>
                  <DockGroupPanel
                    key={`${activePresetId}-bottom`}
                    group="bottom"
                    tabs={groupTabs('bottom')}
                    activeTabId={activeTabId}
                    files={files}
                    onCanvasHost={handleCanvasHost}
                    onSetActiveTab={setActiveTabId}
                    onCloseTab={closeTab}
                    onMoveTab={moveTabToGroup}
                    onReorderTab={reorderTab}
                    onEditorMount={handleEditorDidMount}
                    onEditorChange={handleEditorChange}
                    canvasIsRunning={canvasIsRunning}
                  />
                </Panel>
              </>
            )}

            {/* Bottom panel: Terminal + Tests tabs */}
            <PanelResizeHandle style={resizerVStyle} />
            <Panel defaultSize={28} minSize={8} style={{ overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
              <div style={{ display: 'flex', flexShrink: 0, borderBottom: '1px solid #313244', background: '#181825' }}>
                <button
                  type="button"
                  onClick={() => setBottomTab('terminal')}
                  style={{
                    padding: '4px 12px',
                    fontSize: '12px',
                    cursor: 'pointer',
                    background: bottomTab === 'terminal' ? '#1e1e2e' : 'transparent',
                    color: bottomTab === 'terminal' ? '#cdd6f4' : '#6c7086',
                    border: 'none',
                    borderBottom: bottomTab === 'terminal' ? '2px solid #89b4fa' : '2px solid transparent',
                  }}
                >
                  Terminal
                </button>
                {testsPanelSlot != null && (
                  <button
                    type="button"
                    onClick={() => setBottomTab('tests')}
                    style={{
                      padding: '4px 12px',
                      fontSize: '12px',
                      cursor: 'pointer',
                      background: bottomTab === 'tests' ? '#1e1e2e' : 'transparent',
                      color: bottomTab === 'tests' ? '#cdd6f4' : '#6c7086',
                      border: 'none',
                      borderBottom: bottomTab === 'tests' ? '2px solid #89b4fa' : '2px solid transparent',
                    }}
                  >
                    Tests
                  </button>
                )}
              </div>
              <div style={{ flex: 1, display: bottomTab === 'terminal' ? 'flex' : 'none', flexDirection: 'column', overflow: 'hidden' }}>
                <TerminalPanel
                  terminalTabs={terminalTabs}
                  activeTerminalId={activeTerminalId}
                  onSetActiveTerminal={setActiveTerminalId}
                  onNewTerminal={createTerminalTab}
                  onCloseTerminal={closeTerminalTab}
                  onBootTerminalReady={handleBootTerminalReady}
                />
              </div>
              {bottomTab === 'tests' && testsPanelSlot != null && (
                <div
                  data-testid="tests-panel-slot"
                  style={{ flex: 1, overflow: 'auto', background: '#11111b', padding: '8px' }}
                >
                  {testsPanelSlot}
                </div>
              )}
            </Panel>
          </PanelGroup>
        </Panel>
      </PanelGroup>

      {/* ── Status bar ── */}
      <div
        style={{
          height: 22,
          background: '#89b4fa',
          display: 'flex',
          alignItems: 'center',
          padding: '0 0.75rem',
          gap: '1rem',
          flexShrink: 0,
          fontSize: '0.7rem',
          color: '#11111b',
        }}
      >
        <span>{isReady ? '✓ Ready' : '⟳ Booting...'}</span>
        {activeFile && <span>{activeFile.path}</span>}
        {activeFile?.type === 'text' && <span style={{ marginLeft: 'auto' }}>{inferLanguage(activeFileName).toUpperCase()}</span>}
      </div>
      {lastReport && (
        <div style={{ marginTop: '0.5rem' }}>
          <TestResultsPanel
            report={lastReport}
            maxScore={maxScore}
            passingScore={passingScore}
            weights={testPlan?.cases.map((c) => c.weight ?? 1)}
          />
        </div>
      )}
    </div>
  );
});
