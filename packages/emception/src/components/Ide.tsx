import type { OnMount } from '@monaco-editor/react';
import { Terminal } from '@xterm/xterm';
import { bootInWorker, type WorkerBootResult } from 'emception';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels';
import DockGroupPanel from './DockGroup';
import FileExplorer from './FileExplorer';
import type { DockGroup, OpenTab, TabType, TerminalTab, WorkspaceFile } from './ide-types';
import { DEFAULT_IMAGE, INITIAL_FILES, WORKSPACE_STORAGE_KEY } from './ide-types';
import { buildFileTree, buildSDL3Args, buildSDL3ArgsPort, detectsSDL, inferLanguage, isSourceFile, isTextFile, SDL3_JS_LIB_STUB, toWorkspaceFsPath } from './ide-utils';
import TerminalPanel from './TerminalPanel';

export interface IdeProps {
  title?: string;
  manifestUrl?: string;
}

export default function Ide({ title = 'WebAssembly C++ Toolchain', manifestUrl = '/cdn/manifest.json' }: IdeProps) {
  const editorRef = useRef<Parameters<OnMount>[0] | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const orchestratorRef = useRef<WorkerBootResult | null>(null);
  const xtermRef = useRef<Terminal | null>(null);
  /** Tracks blob URLs created for SDL output so they can be revoked on reset/unmount */
  const sdlBlobUrlsRef = useRef<string[]>([]);
  /** Tracks the injected SDL script element for cleanup on recompile/reset/unmount */
  const sdlScriptRef = useRef<HTMLScriptElement | null>(null);

  const [files, setFiles] = useState<Record<string, WorkspaceFile>>(INITIAL_FILES);
  const [selectedPath, setSelectedPath] = useState('/src/main.cpp');
  const [expandedDirs, setExpandedDirs] = useState<Set<string>>(new Set(['/src', '/assets', '/runtime']));
  const [openTabs, setOpenTabs] = useState<OpenTab[]>([
    { id: 'tab:/src/sdl-main.cpp', path: '/src/sdl-main.cpp', type: 'text', group: 'main' },
    { id: 'tab:/src/main.cpp', path: '/src/main.cpp', type: 'text', group: 'main' },
    { id: 'tab:/runtime/sdl-canvas', path: '/runtime/sdl-canvas', type: 'canvas', group: 'right' },
  ]);
  const [activeTabId, setActiveTabId] = useState('tab:/src/sdl-main.cpp');

  const [terminalTabs, setTerminalTabs] = useState<TerminalTab[]>([{ id: 'terminal-1', title: 'bash' }]);
  const [activeTerminalId, setActiveTerminalId] = useState('terminal-1');

  const [status, setStatus] = useState('Initializing...');
  const [isReady, setIsReady] = useState(false);
  const [terminalReady, setTerminalReady] = useState(false);

  const fileTree = buildFileTree(Object.keys(files));
  const activeTab = openTabs.find((t) => t.id === activeTabId) ?? openTabs[0] ?? null;
  const activeFile = activeTab ? files[activeTab.path] : null;
  const activeFileName = activeFile ? (activeFile.path.split('/').filter(Boolean).pop() ?? '') : '';
  const groupTabs = (group: DockGroup) => openTabs.filter((t) => t.group === group);
  const hasRightGroup = groupTabs('right').length > 0;
  const hasBottomGroup = groupTabs('bottom').length > 0;

  useEffect(() => {
    try {
      const raw = window.localStorage.getItem(WORKSPACE_STORAGE_KEY);
      if (!raw) return;
      const parsed = JSON.parse(raw) as {
        files?: Record<string, WorkspaceFile>;
        selectedPath?: string;
        expandedDirs?: string[];
        openTabs?: OpenTab[];
        activeTabId?: string;
      };
      if (parsed.files && Object.keys(parsed.files).length > 0) setFiles(parsed.files);
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
      // Clear canvas blob URLs (not valid across reloads)
      const filesToSave = Object.fromEntries(Object.entries(files).map(([k, v]) => [k, v.type === 'canvas' ? { ...v, content: '' } : v]));
      window.localStorage.setItem(
        WORKSPACE_STORAGE_KEY,
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

  const handleBootTerminalReady = useCallback((term: Terminal) => {
    xtermRef.current = term;
    (window as Window & { __xterm__?: Terminal }).__xterm__ = term;
    term.writeln('\x1b[32mWelcome to the Browser C/C++ Toolchain!\x1b[0m');
    term.writeln('Booting system...');
    setTerminalReady(true);
  }, []);

  useEffect(() => {
    if (!terminalReady || !xtermRef.current) return;
    let mounted = true;
    const xterm = xtermRef.current;
    const boot = async () => {
      try {
        setStatus('Booting toolchain...');
        const result = await bootInWorker(manifestUrl, xterm);
        if (mounted) {
          orchestratorRef.current = result;
          setStatus('Ready');
          setIsReady(true);
          xterm.writeln('\x1b[32mSystem Ready.\x1b[0m');
          xterm.writeln('Click \x1b[1mCompile & Run\x1b[0m to execute code.');
        }
      } catch (err) {
        if (mounted) {
          setStatus(`Error: ${err instanceof Error ? err.message : String(err)}`);
          xterm.writeln(`\x1b[31mBoot failed: ${err}\x1b[0m`);
        }
      }
    };
    boot();
    return () => {
      mounted = false;
      orchestratorRef.current = null;
    };
  }, [terminalReady, manifestUrl]);

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
        if (existing) return prev.map((t) => (t.id === id ? { ...t, group } : t));
        return [...prev, { id, path, type: file.type, group }];
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

  const createFile = useCallback(
    (kind: TabType) => {
      const baseDir = kind === 'canvas' ? '/runtime' : kind === 'image' ? '/assets' : '/src';
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

  const resetWorkspace = useCallback(() => {
    if (!window.confirm('Reset the workspace to the default demo files and layout?')) return;
    // Revoke any outstanding SDL blob URLs and remove injected script
    sdlScriptRef.current?.remove();
    sdlScriptRef.current = null;
    sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
    sdlBlobUrlsRef.current = [];
    setFiles(INITIAL_FILES);
    setSelectedPath('/src/sdl-main.cpp');
    setExpandedDirs(new Set(['/src', '/assets', '/runtime']));
    setOpenTabs([
      { id: 'tab:/src/sdl-main.cpp', path: '/src/sdl-main.cpp', type: 'text', group: 'main' },
      { id: 'tab:/src/main.cpp', path: '/src/main.cpp', type: 'text', group: 'main' },
      { id: 'tab:/runtime/sdl-canvas', path: '/runtime/sdl-canvas', type: 'canvas', group: 'right' },
    ]);
    setActiveTabId('tab:/src/sdl-main.cpp');
    setTerminalTabs([{ id: 'terminal-1', title: 'bash' }]);
    setActiveTerminalId('terminal-1');
    if (orchestratorRef.current) {
      orchestratorRef.current.tty.clear();
      orchestratorRef.current.tty.writeLine('\x1b[32mWorkspace reset.\x1b[0m');
    } else {
      xtermRef.current?.clear();
      xtermRef.current?.writeln('\x1b[32mWorkspace reset.\x1b[0m');
    }
  }, []);

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

  const handleEditorDidMount: OnMount = (editor) => {
    editorRef.current = editor;
  };

  const handleEditorChange = useCallback((path: string, value: string) => {
    setFiles((prev) => ({ ...prev, [path]: { ...prev[path], content: value } }));
  }, []);

  const handleCompile = async () => {
    if (!orchestratorRef.current || !activeFile || activeFile.type !== 'text') return;
    setActiveTerminalId('terminal-1');
    const P = '[Emception:IDE]';
    const tTotal = performance.now();
    const { client, tty } = orchestratorRef.current;
    const textFiles = Object.values(files).filter((f) => f.type === 'text' && isTextFile(f.path));
    const compileTarget = isSourceFile(activeFile.path)
      ? activeFile.path
      : Object.keys(files).includes('/src/main.cpp')
        ? '/src/main.cpp'
        : textFiles.find((f) => isSourceFile(f.path))?.path;
    if (!compileTarget) {
      setStatus('No compilable source file found');
      tty.writeError('No .c/.cpp source file found in workspace.');
      return;
    }
    setStatus('Compiling...');
    tty.clear();
    tty.writeLine(`Compiling ${compileTarget}...`);
    try {
      const enc = new TextEncoder();
      for (const file of textFiles) {
        const fsPath = toWorkspaceFsPath(file.path);
        await client.writeFile(fsPath, enc.encode(file.content));
        console.log(`${P} Synced ${file.path} -> ${fsPath}`);
      }

      // ── SDL3 path ─────────────────────────────────────────────────
      // Detect SDL3 includes — links against precompiled /usr/lib/libSDL3.a.
      // Output to main.js (SINGLE_FILE=1) so we can wrap it in our own
      // canvas HTML rather than using emscripten's default template.
      const t0 = performance.now();
      if (detectsSDL(files)) {
        tty.writeLine('\x1b[36mSDL3 detected \u2014 compiling...\x1b[0m');

        // ── Strategy 1: emscripten SDL3 port (-sUSE_SDL=3) ─────────────────
        // The emscripten port is built cleanly without camera/sensor modules so
        // there are no pthread EM_ASM undefined-symbol issues.  Try this first;
        // it works when the port is cached in the emception sysroot.
        tty.writeLine('\x1b[90m[1/2] Trying emscripten SDL3 port (-sUSE_SDL=3)...\x1b[0m');
        let sdlResult = await client.run('emcc', buildSDL3ArgsPort(toWorkspaceFsPath(compileTarget)), {
          cwd: '/home/user',
          onStdout: (t: string) => {
            console.log(t);
            tty.writeLine(t);
          },
          onStderr: (t: string) => {
            console.error(t);
            tty.writeError(t);
          },
        });

        if (sdlResult.exitCode !== 0) {
          // ── Strategy 2: precompiled /usr/lib/libSDL3.a ─────────────────────
          // The CDN libSDL3.a contains camera/sensor .o files with EM_ASM blocks
          // that reference pthread-only symbols.  Two flags are required:
          //   [wasm-ld]     -Wl,--unresolved-symbols=ignore-all
          //   [compiler.js] --js-library __sdl_lib.js (mergeInto stub)
          // NOTE: --pre-js is NOT sufficient — it is appended after compiler.js
          // finishes and cannot prevent the "FORWARDED_DATA" assertion failure.
          tty.writeLine('\x1b[90m[2/2] Port unavailable — falling back to /usr/lib/libSDL3.a + stubs...\x1b[0m');
          await client.writeFile('/home/user/__sdl_lib.js', new TextEncoder().encode(SDL3_JS_LIB_STUB));
          sdlResult = await client.run('emcc', buildSDL3Args(toWorkspaceFsPath(compileTarget)), {
            cwd: '/home/user',
            onStdout: (t: string) => {
              console.log(t);
              tty.writeLine(t);
            },
            onStderr: (t: string) => {
              console.error(t);
              tty.writeError(t);
            },
          });
        }
        const sdlDuration = ((performance.now() - t0) / 1000).toFixed(2);
        if (sdlResult.exitCode !== 0) {
          setStatus(`SDL3 compilation failed (${sdlDuration}s)`);
          tty.writeLine(`\x1b[31mSDL3 compilation failed (exit ${sdlResult.exitCode})\x1b[0m`);
          return;
        }
        tty.writeLine(`\x1b[32mSDL3 compiled in ${sdlDuration}s — loading...\x1b[0m`);

        // Read the self-contained JS (SINGLE_FILE=1 embeds wasm as base64)
        const jsBytes = await client.getFile('/home/user/main.js');
        const jsContent = jsBytes ? new TextDecoder().decode(jsBytes) : '';

        // Mark canvas tab as SDL-active (stops demo animation, keeps the canvas element visible)
        setFiles((prev) => ({
          ...prev,
          '/runtime/sdl-canvas': { ...prev['/runtime/sdl-canvas'], content: 'sdl' },
        }));
        ensureOpenTab('/runtime/sdl-canvas', 'right');

        // Wait for React to flush + browser to paint so canvasRef.current is ready
        await new Promise<void>((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));

        const canvas = canvasRef.current;
        if (!canvas) {
          tty.writeError('SDL canvas element not found — open the SDL Canvas tab first');
          return;
        }

        // Reset canvas to clear any previous SDL frame (also destroys old WebGL context)
        canvas.width = 800;
        canvas.height = 600;

        // Remove previous SDL script tag and blobs
        sdlScriptRef.current?.remove();
        sdlBlobUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
        sdlBlobUrlsRef.current = [];

        // Point emscripten Module at the IDE canvas tab element
        (window as Window & { Module?: unknown }).Module = {
          canvas,
          print: (line: string) => tty.writeLine(line),
          printErr: (line: string) => tty.writeError(line),
        };

        const jsBlob = new Blob([jsContent], { type: 'text/javascript' });
        const jsBlobUrl = URL.createObjectURL(jsBlob);
        sdlBlobUrlsRef.current = [jsBlobUrl];

        const script = document.createElement('script');
        script.src = jsBlobUrl;
        sdlScriptRef.current = script;
        document.body.appendChild(script);

        setStatus(`SDL3 done (${((performance.now() - tTotal) / 1000).toFixed(1)}s)`);
        tty.writeLine('\x1b[32mSDL3 rendering in canvas tab →\x1b[0m');
        return;
      }

      // ── Standard WASI path ────────────────────────────────────────
      const result = await client.run('emcc', ['emcc', toWorkspaceFsPath(compileTarget), '-o', '/home/user/main.wasm', '-O2'], {
        cwd: '/home/user',
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
        setStatus('Compilation failed');
        tty.writeLine(`\x1b[31mCompilation failed (exit ${result.exitCode})\x1b[0m`);
        return;
      }
      setStatus(`Compiled (${duration}s)`);
      tty.writeLine(`\x1b[32mCompilation successful in ${duration}s\x1b[0m`);
      tty.writeLine('Running...');
      const lineQueue: number[] = [];
      let lineBuf = '';
      let lineCursor = 0;
      const lineBufferedStdin = async (): Promise<number> => {
        if (lineQueue.length > 0) return lineQueue.shift()!;
        while (true) {
          const raw = tty.readByteExclusive();
          const byte: number = typeof (raw as Promise<number>).then === 'function' ? await (raw as Promise<number>) : (raw as number);
          if (byte === -1) return -1;
          if (byte === 127 || byte === 8) {
            if (lineCursor > 0) {
              lineBuf = lineBuf.slice(0, lineCursor - 1) + lineBuf.slice(lineCursor);
              lineCursor--;
            }
            continue;
          }
          if (byte === 13 || byte === 10) {
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
      await client.run('wasi-run', ['wasi-run', '/home/user/main.wasm'], {
        cwd: '/home/user',
        onStdout: (t: string) => {
          tty.write(t.replace(/\n/g, '\r\n'));
        },
        onStderr: (t: string) => {
          tty.write(`\x1b[31m${t.replace(/\n/g, '\r\n')}\x1b[0m`);
        },
        stdin: lineBufferedStdin,
      });
      setStatus(`Done (${((performance.now() - tTotal) / 1000).toFixed(1)}s)`);
    } catch (e) {
      console.error(`${P} Exception:`, e);
      setStatus('Error during execution');
      tty.writeError(String(e));
    }
  };

  /** Shared resize-handle style for all PanelResizeHandle instances */
  const resizerStyle: React.CSSProperties = {
    width: 4,
    background: '#313244',
    cursor: 'col-resize',
    transition: 'background 0.15s',
  };
  const resizerVStyle: React.CSSProperties = { ...resizerStyle, width: '100%', height: 4, cursor: 'row-resize' };

  return (
    <div className="emception-ide" style={{ display: 'flex', flexDirection: 'column', height: '100%', width: '100%', fontFamily: 'system-ui, sans-serif' }}>
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
          <span data-testid="status" style={{ fontSize: '0.72rem', color: '#a6adc8' }}>
            {status}
          </span>
          <button
            data-testid="compile-button"
            onClick={handleCompile}
            disabled={!isReady || activeFile?.type !== 'text'}
            style={{
              height: 24,
              padding: '0 0.75rem',
              fontSize: '0.8rem',
              fontWeight: 500,
              borderRadius: 4,
              border: 'none',
              cursor: isReady && activeFile?.type === 'text' ? 'pointer' : 'not-allowed',
              background: isReady && activeFile?.type === 'text' ? '#a6e3a1' : '#313244',
              color: isReady && activeFile?.type === 'text' ? '#11111b' : '#585b70',
            }}
          >
            ▶ Compile &amp; Run
          </button>
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
                    group="main"
                    tabs={groupTabs('main')}
                    activeTabId={activeTabId}
                    files={files}
                    canvasRef={canvasRef}
                    onSetActiveTab={setActiveTabId}
                    onCloseTab={closeTab}
                    onMoveTab={moveTabToGroup}
                    onEditorMount={handleEditorDidMount}
                    onEditorChange={handleEditorChange}
                  />
                </Panel>

                {hasRightGroup && (
                  <>
                    <PanelResizeHandle style={resizerStyle} />
                    <Panel defaultSize={35} minSize={15} style={{ overflow: 'hidden' }}>
                      <DockGroupPanel
                        group="right"
                        tabs={groupTabs('right')}
                        activeTabId={activeTabId}
                        files={files}
                        canvasRef={canvasRef}
                        onSetActiveTab={setActiveTabId}
                        onCloseTab={closeTab}
                        onMoveTab={moveTabToGroup}
                        onEditorMount={handleEditorDidMount}
                        onEditorChange={handleEditorChange}
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
                    group="bottom"
                    tabs={groupTabs('bottom')}
                    activeTabId={activeTabId}
                    files={files}
                    canvasRef={canvasRef}
                    onSetActiveTab={setActiveTabId}
                    onCloseTab={closeTab}
                    onMoveTab={moveTabToGroup}
                    onEditorMount={handleEditorDidMount}
                    onEditorChange={handleEditorChange}
                  />
                </Panel>
              </>
            )}

            {/* Terminal */}
            <PanelResizeHandle style={resizerVStyle} />
            <Panel defaultSize={28} minSize={8} style={{ overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
              <TerminalPanel
                terminalTabs={terminalTabs}
                activeTerminalId={activeTerminalId}
                onSetActiveTerminal={setActiveTerminalId}
                onNewTerminal={createTerminalTab}
                onCloseTerminal={closeTerminalTab}
                onBootTerminalReady={handleBootTerminalReady}
              />
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
    </div>
  );
}
