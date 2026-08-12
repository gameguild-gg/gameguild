import type { NativePreset } from '@gameguild/emception-browser';
import { TOOLCHAIN_PRESETS as EMCEPTION_PRESETS, bootInWorker } from '@gameguild/emception-browser';
import type { OnMount } from '@monaco-editor/react';
import { Terminal } from '@xterm/xterm';
import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { Group, Panel, Separator } from 'react-resizable-panels';
import DockGroupPanel from './DockGroup.js';
import FileExplorer from './FileExplorer.js';
import type { DockGroup, IdeProps, OpenTab, TerminalTab, WorkspaceConfig, WorkspaceFile } from './ide-types.js';
import { DEFAULT_IMAGE, deriveStorageKey, parseWorkspaceBundle, resolveArgs, workspaceConfigToState } from './ide-types.js';
import { buildFileTree, inferLanguage, isSourceFile, isTextFile, makeWasiStubs, resolveWsPath } from './ide-utils.js';
import TerminalPanel from './TerminalPanel.js';
import { DEFAULT_PRESET, PRESETS, PRESET_IDS } from './workspace-presets.js';

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

export type { IdeProps };
type WorkerBoot = Awaited<ReturnType<typeof bootInWorker>>;

export default function Ide({
  title = 'Emception',
  manifestUrl = '/cdn/manifest.json',
  workspaceConfig,
  workspaceUrl,
  workspaceName,
  enableFileExplorer = true,
  enableTabs = true,
  enableTerminal = true,
  enableCanvas = true,
  enableDocking = true,
  enableWorkspace = true,
  fullscreen = false,
  onFullscreenChange,
  showHiddenFiles = false,
  showSolutionFiles = false,
  onStdout,
  onStderr,
  stdin: stdinProp,
  readOnly = false,
  theme = 'vs-dark',
}: IdeProps) {
  const storageKey = deriveStorageKey(workspaceName);
  const editorRef = useRef<Parameters<OnMount>[0] | null>(null);
  const monacoRef = useRef<Parameters<OnMount>[1] | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  /** Hidden holder div that keeps the <canvas> alive across dock group moves */
  const canvasHolderRef = useRef<HTMLDivElement | null>(null);
  /** The div inside whichever DockGroupPanel currently shows the canvas tab */
  const canvasHostElRef = useRef<HTMLDivElement | null>(null);
  const orchestratorRef = useRef<WorkerBoot | null>(null);
  const xtermRef = useRef<Terminal | null>(null);
  const terminalLogRef = useRef<HTMLPreElement | null>(null);
  /** Tracks blob URLs created for SDL output so they can be revoked on reset/unmount */
  const sdlBlobUrlsRef = useRef<string[]>([]);
  /** Tracks the injected SDL script element for cleanup on recompile/reset/unmount */
  const sdlScriptRef = useRef<HTMLScriptElement | null>(null);
  /** Tracks the live SDL3 Emscripten module so its RAF loop can be stopped */

  const sdlModuleRef = useRef<{ pauseMainLoop?: () => void } | null>(null);
  /** Active runtime error listener for canvas modules (removed on teardown) */
  const runtimeErrorHandlerRef = useRef<((event: ErrorEvent) => void) | null>(null);

  // Resolve the active workspace config: prop > fetched bundle > default preset
  const [activePresetId, setActivePresetId] = useState<string>(workspaceConfig?.id ?? DEFAULT_PRESET.id);
  const [fetchedConfig, setFetchedConfig] = useState<WorkspaceConfig | null>(null);
  const resolvedConfig = workspaceConfig ?? fetchedConfig ?? PRESETS[activePresetId] ?? DEFAULT_PRESET;
  const initialState = workspaceConfigToState(resolvedConfig);

  const [files, setFiles] = useState<Record<string, WorkspaceFile>>(initialState.files);
  const [selectedPath, setSelectedPath] = useState(initialState.activeTabId.startsWith('tab:') ? initialState.activeTabId.slice(4) : '');
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
  /** Set to true by handleStop so the catch block in handleCompile knows it was intentional */
  const stoppedRef = useRef(false);
  /** Tracks latest files for use in callbacks that can't close over state */
  const filesRef = useRef(files);
  filesRef.current = files;
  // Expose filesRef for e2e tests so Playwright can verify file content was updated
  (window as unknown as Record<string, unknown>).__emception_filesRef__ = filesRef;

  // Visibility filter: honour showHiddenFiles and showSolutionFiles props.
  const visiblePaths = Object.keys(files).filter((path) => {
    const name = path.split('/').pop() ?? '';
    if (!showHiddenFiles && name.startsWith('.')) return false;
    if (!showSolutionFiles && /\.solution\./.test(name)) return false;
    return true;
  });
  const fileTree = buildFileTree(visiblePaths);
  const activeTab = openTabs.find((t) => t.id === activeTabId) ?? openTabs[0] ?? null;
  const activeFile = activeTab && activeTab.type !== 'canvas' ? (files[activeTab.path] ?? null) : null;
  const activeFileName = activeTab?.type === 'canvas' ? 'Canvas' : (activeFile?.path.split('/').filter(Boolean).pop() ?? '');
  const groupTabs = (group: DockGroup) => openTabs.filter((t) => t.group === group);
  const hasRightGroup = groupTabs('right').length > 0;
  const hasBottomGroup = groupTabs('bottom').length > 0;

  useEffect(() => {
    try {
      const raw = enableWorkspace ? window.localStorage.getItem(storageKey) : null;
      if (!raw) return;
      const parsed = JSON.parse(raw) as {
        files?: Record<string, WorkspaceFile>;
        selectedPath?: string;
        expandedDirs?: string[];
        openTabs?: OpenTab[];
        activeTabId?: string;
      };
      if (parsed.files && Object.keys(parsed.files).length > 0) {
        const nextFiles = Object.fromEntries(Object.entries(parsed.files));
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
      // Persist workspace files
      const filesToSave = files;
      if (!enableWorkspace) return;
      window.localStorage.setItem(
        storageKey,
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
        setSelectedPath(state.activeTabId.startsWith('tab:') ? state.activeTabId.slice(4) : '');
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
      await client.writeFile(file.path, enc.encode(file.content));
      console.log(`${P} VFS sync: ${file.path}`);
    }
    console.log(`${P} VFS sync complete (${textFiles.length} files)`);
  }, []);

  // ── Switch workspace preset ───────────────────────────────────
  const switchWorkspace = useCallback(
    async (presetId: string) => {
      const preset = PRESETS[presetId];
      if (!preset) return;
      const P = '[Emception:IDE]';
      console.log(`${P} ===== WORKSPACE SWITCH: "${activePresetId}" → "${presetId}" =====`);

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
      sdlScriptRef.current?.remove();
      sdlScriptRef.current = null;
      sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
      sdlBlobUrlsRef.current = [];
      // Reset canvas state
      const canvas = canvasRef.current;
      if (canvas) {
        delete canvas.dataset.sdlRunning;
        canvas.style.display = 'none';
      }
      setCanvasIsRunning(false);
      setExecutionPhase('idle');
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

      stoppedRef.current = false;
      setActivePresetId(presetId);
      const state = workspaceConfigToState(preset);
      setFiles(state.files);
      setOpenTabs(state.openTabs);
      setActiveTabId(state.activeTabId);
      setExpandedDirs(state.expandedDirs);
      setSelectedPath(state.activeTabId.startsWith('tab:') ? state.activeTabId.slice(4) : '');

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
        orchestratorRef.current.tty.writeLine(`\x1b[32mSwitched to workspace: ${preset.label}\x1b[0m`);
        // Sync new workspace files into VFS so /home/user is populated immediately
        await syncFilesToVfs(state.files);
      }
      console.log(`${P} ===== WORKSPACE SWITCH COMPLETE =====`);
    },
    [activePresetId, syncFilesToVfs],
  );

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
        orchestratorRef.current = result;
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
    };
  }, [terminalReady, manifestUrl, doBootstrap]);

  // Revoke SDL blob URLs and remove injected script on unmount
  useEffect(() => {
    return () => {
      sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
      sdlScriptRef.current?.remove();
    };
  }, []);

  const ensureOpenTab = useCallback(
    (path: string, group: DockGroup = 'main') => {
      const file = files[path];
      if (!file) return;
      const id = `tab:${path}`;
      setOpenTabs((prev) => {
        const existing = prev.find((t) => t.id === id);
        if (existing) return prev.map((t) => (t.id === id ? ({ ...t, group } as OpenTab) : t));
        return [...prev, { id, path, type: file.type, group }];
      });
      setActiveTabId(id);
    },
    [files],
  );

  const ensureCanvasTab = useCallback((group: DockGroup = 'right') => {
    setOpenTabs((prev) => {
      const existing = prev.find((t) => t.id === 'canvas');
      if (existing) return prev.map((t) => (t.id === 'canvas' ? ({ ...t, group } as OpenTab) : t));
      return [...prev, { id: 'canvas', type: 'canvas', group }];
    });
    setActiveTabId('canvas');
  }, []);

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
    setOpenTabs((prev) => prev.map((t) => (t.id === tabId ? ({ ...t, group } as OpenTab) : t)));
    setActiveTabId(tabId);
  }, []);

  /** Reorder a tab by placing it just before `beforeTabId` in the openTabs array. */
  const reorderTab = useCallback((tabId: string, beforeTabId: string) => {
    setOpenTabs((prev) => {
      const srcIdx = prev.findIndex((t) => t.id === tabId);
      const dstIdx = prev.findIndex((t) => t.id === beforeTabId);
      if (srcIdx === -1 || dstIdx === -1 || srcIdx === dstIdx) return prev;
      const tab = { ...prev[srcIdx], group: prev[dstIdx].group } as OpenTab;
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

  const createFile = useCallback(
    (kind: 'text' | 'image') => {
      const baseDir = '/user';
      const defaultName = kind === 'image' ? 'new-image.svg' : 'new-file.cpp';
      const input = window.prompt(`Create new ${kind} file`, `${baseDir}/${defaultName}`);
      if (!input) return;
      const path = input.startsWith('/') ? input : `${baseDir}/${input}`;
      if (files[path]) {
        window.alert(`File already exists: ${path}`);
        return;
      }
      const content = kind === 'image' ? DEFAULT_IMAGE : kind === 'text' && path.endsWith('.h') ? '#pragma once\n\n' : '// New source file\n';
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
    setOpenTabs((prev) =>
      prev.map((tab) => (tab.type !== 'canvas' && tab.path === selectedPath ? ({ ...tab, path: norm, id: `tab:${norm}` } as OpenTab) : tab)),
    );
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
    setSelectedPath(state.activeTabId.startsWith('tab:') ? state.activeTabId.slice(4) : '');
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

    const ensureModel = (path: string) => {
      const file = filesRef.current[path];
      if (!file || file.type !== 'text') return;

      desiredPaths.add(path);
      const uri = monaco.Uri.file(path);
      const existing = monaco.editor.getModel(uri);

      if (!existing) {
        monaco.editor.createModel(file.content, inferLanguage(path), uri);
        return;
      }

      if (existing.getValue() !== file.content && activeTabId !== `tab:${path}`) {
        existing.setValue(file.content);
      }
    };

    for (const tab of openTabs) {
      if (tab.type !== 'canvas') ensureModel(tab.path);
    }
    if (selectedPath) ensureModel(selectedPath);

    const activePath = activeTabId.startsWith('tab:') ? activeTabId.slice(4) : null;
    for (const model of monaco.editor.getModels()) {
      if (model.uri.scheme !== 'file') continue;
      const modelPath = model.uri.path;
      if (!desiredPaths.has(modelPath) && modelPath !== activePath) {
        model.dispose();
      }
    }
  }, [activeTabId, openTabs, selectedPath]);

  useEffect(() => {
    syncMonacoModels();
  }, [files, syncMonacoModels]);

  // Expose for e2e tests so Playwright can update file content directly in React state
  (window as unknown as Record<string, unknown>).__setFileContent = handleEditorChange;

  const teardownSdlRuntime = useCallback(() => {
    const sdlMod = sdlModuleRef.current;
    if (!sdlMod && !sdlScriptRef.current && sdlBlobUrlsRef.current.length === 0) return false;

    if (runtimeErrorHandlerRef.current) {
      window.removeEventListener('error', runtimeErrorHandlerRef.current);
      runtimeErrorHandlerRef.current = null;
    }

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

    if (resolvedConfig.run.type === 'canvas' && executionPhase === 'running') {
      teardownSdlRuntime();
    }

    const textFiles = Object.values(currentFiles).filter((f) => f.type === 'text' && isTextFile(f.path));

    // Determine which source file to compile/run
    const cwd = resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`;
    const entryPointRel = resolvedConfig.compile.sourceDetect?.entryPoint;
    const entryPoint = entryPointRel ? resolveWsPath(cwd, entryPointRel) : undefined;
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
        await client.writeFile(file.path, enc.encode(file.content));
        console.log(`${P} Synced ${file.path}`);
      }

      const t0 = performance.now();
      const runType = resolvedConfig.run.type;

      // ── Python script path ──────────────────────────────────────
      if (runType === 'python-script') {
        const pyFile = compileTarget ?? entryPoint ?? `${cwd}/main.py`;
        const args = resolvedConfig.run.args ? resolveArgs(resolvedConfig.run.args, pyFile) : ['python3', pyFile];
        setStatus('Running Python...');
        tty.writeLine(`\x1b[36mRunning ${pyFile}...\x1b[0m`);
        setExecutionPhase('running');
        const lineBufferedStdin = makeLineBufferedStdin(tty);
        await client.run(args[0], args, {
          cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
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
          cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
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
          cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
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
          cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
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

      // ── Canvas path (SDL3 two-step OR generic emcc single-step) ─
      if (runType === 'canvas') {
        if (!compileTarget) {
          setExecutionPhase('idle');
          setStatus('No compilable source file found');
          tty.writeError('No .c/.cpp source file found in workspace.');
          return;
        }
        setStatus('Compiling...');
        tty.writeLine(`Compiling ${compileTarget}...`);

        const sourceFsPath = compileTarget;

        const { toolchain } = resolvedConfig.compile;
        const isSDL3 = toolchain?.startsWith('sdl') ?? false;
        const isAllegro = toolchain?.startsWith('allegro') ?? false;
        const isRaylib = toolchain?.startsWith('raylib') ?? false;

        if (!isSDL3 && !isRaylib && !isAllegro) {
          // ── Generic emcc single-step path (raylib, etc.) ────────
          tty.writeLine('\x1b[36mCanvas compile...\x1b[0m');
          const compileArgv = resolveArgs(resolvedConfig.compile.args, sourceFsPath);
          const canvasCompile = await client.run(compileArgv[0], compileArgv, {
            cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
            onStdout: (t: string) => {
              tty.writeLine(t);
            },
            onStderr: (t: string) => {
              tty.writeError(t);
            },
          });
          const canvasDuration = ((performance.now() - t0) / 1000).toFixed(2);
          if (canvasCompile.exitCode !== 0) {
            setExecutionPhase('idle');
            setStatus(`Canvas compilation failed (${canvasDuration}s)`);
            tty.writeLine(`\x1b[31mCanvas compile step failed (exit ${canvasCompile.exitCode})\x1b[0m`);
            return;
          }
          tty.writeLine(`\x1b[32mCompiled in ${canvasDuration}s \u2014 loading...\x1b[0m`);

          const jsOutPath = resolveWsPath(cwd, resolvedConfig.compile.output || 'main.js');
          const jsBytes = await client.getFile(jsOutPath);
          if (!jsBytes) {
            setExecutionPhase('idle');
            tty.writeError(`${jsOutPath} not found \u2014 emcc may have failed to produce output`);
            return;
          }

          setCanvasIsRunning(true);
          ensureCanvasTab('right');
          setActiveTabId(`tab:${compileTarget}`);
          await new Promise<void>((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));

          const canvas = canvasRef.current;
          if (!canvas) {
            setExecutionPhase('idle');
            tty.writeError('Canvas element not found \u2014 open the Canvas tab first');
            return;
          }
          canvas.dataset.sdlRunning = 'true';
          canvas.style.display = 'block';
          canvas.width = 800;
          canvas.height = 600;

          // Provide window.Module so emcc SINGLE_FILE output targets our canvas.
          (window as unknown as Record<string, unknown>)['Module'] = {
            canvas,
            print: (s: string) => tty.writeLine(s),
            printErr: (s: string) => tty.writeError(s),
          };

          sdlScriptRef.current?.remove();
          sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
          sdlBlobUrlsRef.current = [];

          const jsText = new TextDecoder().decode(jsBytes instanceof Uint8Array ? jsBytes : new Uint8Array(jsBytes as ArrayBuffer));
          const jsBlob = new Blob([jsText], { type: 'application/javascript' });
          const jsBlobUrl = URL.createObjectURL(jsBlob);
          sdlBlobUrlsRef.current = [jsBlobUrl];

          const script = document.createElement('script');
          script.src = jsBlobUrl;
          document.head.appendChild(script);
          sdlScriptRef.current = script;
          sdlModuleRef.current = null;

          setExecutionPhase('running');
          setStatus(`Canvas done (${((performance.now() - tTotal) / 1000).toFixed(1)}s) \u2014 running`);
          tty.writeLine('\x1b[32mCanvas rendering in canvas tab \u2192\x1b[0m');
          return;
        }

        // ── Two-step clang+wasm-ld path (SDL3, raylib, or Allegro) ──
        // All three use a MODULARIZE runtime mjs for WebGL/emscripten_set_main_loop.
        // SDL3 exports SDL_App* callbacks; raylib and Allegro export main() and
        // register their loop via emscripten_set_main_loop — callMain() handles all.
        const canvasLabel = isSDL3 ? 'SDL3' : isAllegro ? 'allegro' : 'raylib';
        const canvasPreset = EMCEPTION_PRESETS[toolchain!] as NativePreset;
        const runtimePath = isAllegro
          ? '/usr/lib/emscripten/allegro-runtime.mjs'
          : isRaylib
            ? '/usr/lib/emscripten/raylib-runtime.mjs'
            : '/usr/lib/emscripten/sdl3-runtime.mjs';
        tty.writeLine(`\x1b[36m${canvasLabel} detected \u2014 compiling object...\x1b[0m`);

        const sdlObjPath = '/tmp/emception-canvas-main.o';
        const compileCwd = resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`;
        const wasmPath = resolveWsPath(compileCwd, resolvedConfig.compile.output || 'main.wasm');

        const sdlPaths = { sourcePath: sourceFsPath, objectPath: sdlObjPath, wasmPath };
        // Map canvas preset name to the CDN bundle name used by the hints system.
        const canvasBundleName = isSDL3 ? 'sdl3' : isAllegro ? 'allegro' : 'raylib';
        const canvasRunHints = { bundlesNeeded: [canvasBundleName] };
        const sdlCompile = await client.run(canvasPreset.compileTool, canvasPreset.compileArgv(sdlPaths), {
          cwd: compileCwd,
          onStdout: (t: string) => {
            console.log(t);
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            console.error(t);
            tty.writeError(t);
          },
          hints: canvasRunHints,
        });

        const sdlDuration = ((performance.now() - t0) / 1000).toFixed(2);
        if (sdlCompile.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus(`${canvasLabel} compilation failed (${sdlDuration}s)`);
          tty.writeLine(`\x1b[31m${canvasLabel} compile step failed (exit ${sdlCompile.exitCode})\x1b[0m`);
          return;
        }

        tty.writeLine(`\x1b[36m${canvasLabel} linking (wasm-ld)...\x1b[0m`);

        const sdlLink = await client.run(canvasPreset.linkTool, canvasPreset.linkArgv(sdlPaths), {
          cwd: compileCwd,
          onStdout: (t: string) => {
            console.log(t);
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            console.error(t);
            tty.writeError(t);
          },
          hints: canvasRunHints,
        });

        if (sdlLink.exitCode !== 0) {
          setExecutionPhase('idle');
          setStatus(`${canvasLabel} compilation failed (${sdlDuration}s)`);
          tty.writeLine(`\x1b[31m${canvasLabel} link step failed (exit ${sdlLink.exitCode})\x1b[0m`);
          return;
        }

        tty.writeLine(`\x1b[32m${canvasLabel} compiled in ${sdlDuration}s \u2014 loading...\x1b[0m`);

        // Read the compiled WASM binary from the VFS.
        const wasmBytes = await client.getFile(wasmPath);
        if (!wasmBytes) {
          setExecutionPhase('idle');
          tty.writeError('main.wasm not found — emcc may have failed to produce it alongside main.js');
          return;
        }

        // Read the pre-built SDL3 JS runtime shell from the VFS
        // (used for both SDL3 and raylib - provides emscripten_set_main_loop & WebGL)
        const runtimeBytes = await client.getFile(runtimePath);
        if (!runtimeBytes) {
          setExecutionPhase('idle');
          tty.writeError(`${runtimePath} not found in VFS — rebuild the CDN bundle`);
          return;
        }

        // Mark canvas tab as SDL-active (keeps the canvas element visible)
        setCanvasIsRunning(true);
        ensureCanvasTab('right');
        setActiveTabId(`tab:${compileTarget}`);

        // Wait for React to flush + browser to paint so canvasRef.current is ready
        await new Promise<void>((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));

        const canvas = canvasRef.current;
        if (!canvas) {
          setExecutionPhase('idle');
          tty.writeError('SDL canvas element not found \u2014 open the SDL Canvas tab first');
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
        } else if (isSDL3) {
          tty.writeError('sdl3-runtime patch: keyEventHandlerFunc not found — keyboard may be captured globally');
        }

        // Patch: Catch WebAssembly.RuntimeError (e.g. memory access out of bounds) inside
        // callUserCallback so it never propagates to the browser's uncaught-error handler.
        // Without this, any WASM trap in the RAF loop causes Chromium to crash the tab.
        // We set ABORT=1 so subsequent loop iterations are skipped, then pause the main loop.
        const ORIG_CALL_USER_CB = 'var callUserCallback=func=>{if(ABORT){return}try{return func()}catch(e){handleException(e)}finally{maybeExit()}}';
        const PATCHED_CALL_USER_CB =
          'var callUserCallback=func=>{if(ABORT){return}try{return func()}catch(e){' +
          'if(e instanceof WebAssembly.RuntimeError){ABORT=1;try{Module.pauseMainLoop?.();}catch(_){}return;}' +
          'handleException(e)}finally{maybeExit()}}';
        if (runtimeText.includes(ORIG_CALL_USER_CB)) {
          runtimeText = runtimeText.replace(ORIG_CALL_USER_CB, PATCHED_CALL_USER_CB);
        } else {
          tty.writeError('sdl3-runtime patch: callUserCallback not found — WASM traps may crash the tab');
        }

        // Patch: Also intercept RuntimeError in handleException itself, which is called by
        // callMain and other paths, so any WASM trap outside the main loop is also contained.
        const ORIG_HANDLE_EX = 'var handleException=e=>{if(e instanceof ExitStatus||e=="unwind"){return EXITSTATUS}quit_(1,e)}';
        const PATCHED_HANDLE_EX =
          'var handleException=e=>{if(e instanceof ExitStatus||e=="unwind"){return EXITSTATUS}' +
          'if(e instanceof WebAssembly.RuntimeError){ABORT=1;try{Module.pauseMainLoop?.();}catch(_){}return EXITSTATUS}' +
          'quit_(1,e)}';
        if (runtimeText.includes(ORIG_HANDLE_EX)) {
          runtimeText = runtimeText.replace(ORIG_HANDLE_EX, PATCHED_HANDLE_EX);
        } else {
          tty.writeError('sdl3-runtime patch: handleException not found — WASM traps may crash the tab');
        }

        // Create a blob URL for the ES6 runtime module so we can dynamically import it
        const runtimeBlob = new Blob([new TextEncoder().encode(runtimeText)], { type: 'application/javascript' });
        const runtimeUrl = URL.createObjectURL(runtimeBlob);
        sdlBlobUrlsRef.current = [runtimeUrl];

        // Dynamically import the MODULARIZE ES6 factory and instantiate with WASM + canvas
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const { default: createModule } = await import(/* webpackIgnore: true */ /* @vite-ignore */ runtimeUrl as any);

        // Let the SDL3 runtime create its own memory with ALLOW_MEMORY_GROWTH.
        // The patched getHeapMax() in sdl3-runtime.mjs limits growth to 256MB.
        const wasmMemory = null;
        const wasiStubs = makeWasiStubs(
          () => wasmMemory,
          (s: string) => tty.writeLine(s),
        );

        let sdlLoadOk = true;
        let sdlCallbackFns: { init?: (appstate: number, argc: number, argv: number) => number; iterate?: (appstate: number) => number } | null = null;
        let wasmMemoryRef: WebAssembly.Memory | null = null;
        const missingRaylibImports = new Set<string>();
        const moduleTimeout = new Promise<never>((_, reject) => setTimeout(() => reject(new Error('SDL3 module load timeout (30s)')), 30_000));
        const sdlMod = await Promise.race([
          createModule({
            canvas: canvas,
            keyboardListeningElement: canvas,
            wasmBinary: wasmBytes,
            locateFile: (filename: string) => filename,
            // For raylib: prevent the runtime from calling main() internally
            // (via its own run() → callMain() path). Without this, main() fires
            // twice — once from the runtime and once from our explicit entry
            // invocation below — causing InitWindow + emscripten_set_main_loop
            // to execute twice, registering duplicate RAF loops → crash.
            // SDL3 is unaffected because its _main is a noop proxy.
            // Allegro also exports user main() — same double-call hazard.
            noInitialRun: isRaylib || isAllegro,
            // SDL3 only: override instantiateWasm so we can patch callback
            // lifecycle exports for callback-only apps. Raylib and Allegro have
            // their own runtime glue and should use the runtime's native
            // instantiate path.
            ...(!isRaylib &&
              !isAllegro && {
              // eslint-disable-next-line @typescript-eslint/no-explicit-any
              instantiateWasm(info: any, receiveInstance: (inst: WebAssembly.Instance) => void) {
                const envBase = {
                  ...info.env,
                  // Preserve runtime-provided memory growth handler when present.
                  // Overriding this with a no-op can leave stale HEAP views and
                  // lead to out-of-bounds traps after grow_memory.
                  emscripten_notify_memory_growth: info?.env?.emscripten_notify_memory_growth ?? (() => { }),
                  // Some builds import emscripten_asm_const_* helpers directly.
                  // Delegate to available runtime helpers when possible.
                  emscripten_asm_const_int: info?.env?.emscripten_asm_const_int ?? (() => 0),
                  emscripten_asm_const_double:
                    info?.env?.emscripten_asm_const_double ??
                    ((...args: unknown[]) => {
                      const fallback = info?.env?.emscripten_asm_const_int;
                      return typeof fallback === 'function' ? Number(fallback(...args)) : 0;
                    }),
                  // Some raylib/emscripten link variants import env.exit/_exit.
                  // Keep these as benign stubs so instantiation succeeds and the
                  // app can drive its main loop normally in the browser runtime.
                  exit: info?.env?.exit ?? (() => { }),
                  _exit: info?.env?._exit ?? (() => { }),
                  // _abort_js is the WASM import for C abort(). Newer Emscripten
                  // runtimes include it; if the stub WASM didn't use abort() the
                  // runtime omits it, but the user's WASM may still need it.
                  _abort_js:
                    info?.env?._abort_js ??
                    (() => {
                      throw new Error('abort()');
                    }),
                };
                const env = new Proxy(envBase, {
                  get(target, prop, receiver) {
                    const value = Reflect.get(target, prop, receiver);
                    if (typeof prop === 'string' && (prop.startsWith('gl') || prop.startsWith('emscripten_gl'))) {
                      // Raylib may import bare GL symbols (e.g. glViewport) while
                      // runtime glue commonly exposes emscripten_gl* wrappers.
                      // If a GL import is missing or non-callable, map to wrapper
                      // when present; otherwise provide a benign callable fallback
                      // so WebAssembly.instantiate() doesn't fail with
                      // "function import requires a callable".
                      const toCallable = (candidate: unknown): (() => number) | unknown => {
                        if (typeof candidate === 'function') return candidate;
                        return () => 0;
                      };

                      if (typeof value === 'function') return value;
                      const emscriptenName = prop.startsWith('emscripten_') ? prop : `emscripten_${prop}`;
                      const mapped = Reflect.get(target, emscriptenName, receiver);
                      return toCallable(mapped);
                    }

                    if (value !== undefined) return value;
                    if (typeof prop === 'string') {
                      // Allow benign no-op for selected optional symbols often
                      // imported by browser GL/runtime variants.
                      if (
                        prop === 'exit' ||
                        prop === '_exit' ||
                        prop.startsWith('gl') ||
                        prop.startsWith('emscripten_gl') ||
                        prop.startsWith('emscripten_asm_const_')
                      ) {
                        return () => 0;
                      }
                      // C assert() / IM_ASSERT — compiled into any library built
                      // without -DNDEBUG (e.g. Dear ImGui). Throw so the error
                      // surfaces in the terminal rather than silently crashing.
                      if (prop === '__assert_fail') {
                        return (_cond: number, _file: number, line: number) => {
                          let fileStr = '(unknown)';
                          if (wasmMemoryRef) {
                            try {
                              const heap = new Uint8Array(wasmMemoryRef.buffer);
                              const readStr = (ptr: number) => {
                                let end = ptr;
                                while (heap[end]) end++;
                                return new TextDecoder().decode(heap.subarray(ptr, end));
                              };
                              fileStr = readStr(_file);
                            } catch {
                              /* ignore decode errors */
                            }
                          }
                          throw new Error(`Assertion failed (${fileStr}:${line})`);
                        };
                      }
                      // Raylib + emscripten JS libs can import additional helper
                      // symbols (e.g. SetCanvasIdJs). Keep raylib/allegro permissive
                      // here to avoid hard load failures; strict mode remains for SDL3.
                      if (canvasLabel === 'raylib' || canvasLabel === 'allegro') {
                        if (!missingRaylibImports.has(prop)) {
                          missingRaylibImports.add(prop);
                          tty.writeLine(`\x1b[33m${canvasLabel} missing env import shimmed: ${prop}\x1b[0m`);
                        }
                        return () => 0;
                      }
                      // Fail fast for unknown imports to avoid masking linker
                      // mismatches and causing unstable runtime behavior.
                      throw new Error(`Missing WASM env import: ${prop}`);
                    }
                    throw new Error(`Missing WASM env import: ${String(prop)}`);
                  },
                });
                const imports = { ...info, env, wasi_snapshot_preview1: wasiStubs };
                tty.writeLine('\x1b[90mSDL3: instantiating WASM…\x1b[0m');
                WebAssembly.instantiate(new Uint8Array(wasmBytes as unknown as ArrayBuffer), imports)
                  .then((result) => {
                    tty.writeLine('\x1b[90mSDL3: WASM ok, patching exports…\x1b[0m');
                    const origExports = result.instance.exports;
                    if (origExports.memory instanceof WebAssembly.Memory) {
                      wasmMemoryRef = origExports.memory as WebAssembly.Memory;
                    }
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
                    tty.writeError(`${canvasLabel} WASM instantiation failed: ${err}`);
                    setStatus(`${canvasLabel} load failed`);
                  });
                return {};
              },
            }),
            print: (line: string) => tty.writeLine(line),
            printErr: (line: string) => tty.writeError(line),
          }),
          moduleTimeout,
        ]).catch((e: unknown) => {
          sdlLoadOk = false;
          tty.writeError(`${canvasLabel} module error: ${e}`);
          // Surface error to console so e2e tests / devtools can see the full message + stack.

          console.error(`[Emception:IDE] ${canvasLabel} module load error:`, e);
          setStatus(`${canvasLabel} load failed`);
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
            tty.writeError(`${canvasLabel} entry invocation error: ${msg}`);
          }
        }

        sdlModuleRef.current = sdlMod as { pauseMainLoop?: () => void } | null;
        if (runtimeErrorHandlerRef.current) {
          window.removeEventListener('error', runtimeErrorHandlerRef.current);
          runtimeErrorHandlerRef.current = null;
        }
        const runtimeErrHandler = (event: ErrorEvent) => {
          const msg = String(event.error?.message ?? event.message ?? '');
          if (!msg.includes('memory access out of bounds')) return;
          tty.writeError(`${canvasLabel} runtime trapped (memory out of bounds); stopping main loop.`);
          try {
            sdlModuleRef.current?.pauseMainLoop?.();
          } catch {
            /* ignore */
          }
          event.preventDefault?.();
        };
        runtimeErrorHandlerRef.current = runtimeErrHandler;
        window.addEventListener('error', runtimeErrHandler);
        setExecutionPhase('running');
        setStatus(`${canvasLabel} done (${((performance.now() - tTotal) / 1000).toFixed(1)}s) — running`);
        tty.writeLine(`\x1b[32m${canvasLabel} rendering in canvas tab →\x1b[0m`);
        return;
      } // end canvas

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
        const sourceFsPath = compileTarget;
        const objPath = '/tmp/emception-terminal-main.o';
        const wasmPath = resolveWsPath(cwd, resolvedConfig.compile.output || 'main.wasm');

        tty.writeLine('\x1b[36mDirect compile (clang -cc1)...\x1b[0m');
        // Use clang -cc1 directly (not driver mode). The driver tries to
        // posix_spawn cc1 as a subprocess which fails silently in the browser
        // (no fork/posix_spawn in emscripten libc), causing clang to exit 0
        // in ~35ms with no output. cc1_main is linked into clang.wasm so the
        // frontend runs in-process when invoked with -cc1 directly.
        // Detect language from source file extension to pick the right preset.
        // C source files (.c) use the C preset; everything else uses C++.
        const isC = compileTarget.endsWith('.c');
        const directPreset = isC ? (EMCEPTION_PRESETS.c as NativePreset) : (EMCEPTION_PRESETS.cpp as NativePreset);
        const presetPaths = { sourcePath: sourceFsPath, objectPath: objPath, wasmPath };
        const clangResult = await client.run(directPreset.compileTool, directPreset.compileArgv(presetPaths), {
          cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
          onStdout: (t: string) => {
            console.log(t);
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            console.error(t);
            tty.writeError(t);
          },
        });

        if (clangResult.exitCode !== 0) {
          const dur = ((performance.now() - t0) / 1000).toFixed(2);
          setExecutionPhase('idle');
          setStatus(`Compilation failed (${dur}s)`);
          tty.writeLine(`\x1b[31mCompilation failed (exit ${clangResult.exitCode})\x1b[0m`);
          return;
        }

        tty.writeLine('\x1b[36mLinking (wasm-ld)...\x1b[0m');
        const lldResult = await client.run(directPreset.linkTool, directPreset.linkArgv(presetPaths), {
          cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
          onStdout: (t: string) => {
            console.log(t);
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            console.error(t);
            tty.writeError(t);
          },
        });

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
          cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
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
          ? resolveArgs(resolvedConfig.compile.args, compileTarget)
          : ['emcc', compileTarget, '-o', resolveWsPath(cwd, resolvedConfig.compile.output || 'main.wasm'), '-O2'];
      const result = await client.run(compileArgs[0], compileArgs, {
        cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
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
      const wasmPath = resolveWsPath(cwd, resolvedConfig.compile.output || 'main.wasm');
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
        const sourceFsPath = compileTarget;

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
            cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
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
              cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
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
        cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
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
        await client.writeFile(file.path, enc.encode(file.content));
      }

      // Compile test if needed
      if (testConfig.compileArgs && testConfig.compileArgs.length > 0) {
        setStatus('Compiling tests...');
        const compileResult = await client.run(testConfig.tool, testConfig.compileArgs, {
          cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
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
        cwd: resolvedConfig.compile.cwd ?? `/home/user/${resolvedConfig.id}`,
        onStdout: (t: string) => {
          tty.write(t.replace(/\n/g, '\r\n'));
        },
        onStderr: (t: string) => {
          tty.write(`\x1b[31m${t.replace(/\n/g, '\r\n')}\x1b[0m`);
        },
        stdin: lineBufferedStdin,
      });

      setExecutionPhase('idle');
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
    setExecutionPhase('idle');
    setIsReady(false);
    setStatus('Stopped — rebooting...');
    xtermRef.current?.writeln('\x1b[33mExecution stopped.\x1b[0m');
    await doBootstrap();
    editorRef.current?.focus();
  };

  /** Shared resize-handle style for all Separator instances */
  const resizerStyle: React.CSSProperties = {
    width: 4,
    background: '#313244',
    cursor: 'col-resize',
    transition: 'background 0.15s',
  };
  const resizerVStyle: React.CSSProperties = { ...resizerStyle, width: '100%', height: 4, cursor: 'row-resize' };
  const canRecompileWhileRunning = executionPhase === 'running' && resolvedConfig.run.type === 'canvas';
  const canCompile = isReady && activeFile?.type === 'text' && (executionPhase === 'idle' || canRecompileWhileRunning);
  const showCompileButton = executionPhase !== 'running' || canRecompileWhileRunning;

  const ideContent = (
    <div
      className="emception-ide"
      style={{
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        width: '100%',
        fontFamily: 'system-ui, sans-serif',
        ...(fullscreen ? { position: 'fixed', inset: 0, zIndex: 9999, background: '#181825' } : {}),
      }}
    >
      {/* Hidden log for Playwright E2E assertions — not visible to users */}
      <pre data-testid="terminal" ref={terminalLogRef} hidden aria-hidden="true" style={{ display: 'none' }} />
      {/* Hidden holder keeps the SDL canvas alive across dock moves. Gated by enableCanvas. */}
      {enableCanvas && (
        <div ref={canvasHolderRef} style={{ position: 'absolute', width: 0, height: 0, overflow: 'hidden', pointerEvents: 'none' }}>
          <canvas id="canvas" data-testid="sdl-canvas" tabIndex={0} ref={canvasRef} style={{ width: '100%', height: '100%', display: 'none' }} />
        </div>
      )}
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
          {/* Workspace preset picker */}
          {!workspaceConfig && !workspaceUrl && (
            <select
              data-testid="workspace-picker"
              value={activePresetId}
              onChange={(e) => switchWorkspace(e.target.value)}
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
              {PRESET_IDS.map((id) => (
                <option key={id} value={id}>
                  {PRESETS[id].label}
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
      <Group orientation="horizontal" style={{ flex: 1, overflow: 'hidden' }}>
        {enableFileExplorer && (
          <>
            {/* Sidebar */}
            <Panel defaultSize="18" minSize="10" maxSize="40" style={{ overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
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
              />
            </Panel>
            <Separator style={resizerStyle} />
          </>
        )}

        {/* Editor + terminal column */}
        <Panel style={{ display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
          <Group orientation="vertical" style={{ flex: 1, overflow: 'hidden' }}>
            {/* Editor row: main (+ optional right panel) */}
            <Panel style={{ display: 'flex', overflow: 'hidden' }}>
              <Group orientation="horizontal" style={{ flex: 1, overflow: 'hidden' }}>
                <Panel minSize="20" style={{ overflow: 'hidden' }}>
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
                    <Separator style={resizerStyle} />
                    <Panel defaultSize="35" minSize="15" style={{ overflow: 'hidden' }}>
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
              </Group>
            </Panel>

            {hasBottomGroup && (
              <>
                <Separator style={resizerVStyle} />
                <Panel defaultSize="25" minSize="10" style={{ overflow: 'hidden' }}>
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

            {enableTerminal && (
              <>
                {/* Terminal */}
                <Separator style={resizerVStyle} />
                <Panel defaultSize="28" minSize="8" style={{ overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
                  <TerminalPanel
                    terminalTabs={terminalTabs}
                    activeTerminalId={activeTerminalId}
                    onSetActiveTerminal={setActiveTerminalId}
                    onNewTerminal={createTerminalTab}
                    onCloseTerminal={closeTerminalTab}
                    onBootTerminalReady={handleBootTerminalReady}
                  />
                </Panel>
              </>
            )}
          </Group>
        </Panel>
      </Group>

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
    </div>
  );
  return fullscreen && typeof document !== 'undefined' ? createPortal(ideContent, document.body) : ideContent;
}
