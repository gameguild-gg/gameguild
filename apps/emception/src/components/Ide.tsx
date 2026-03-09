'use client';

import Editor, { OnMount } from '@monaco-editor/react';
import { FitAddon } from '@xterm/addon-fit';
import { Terminal } from '@xterm/xterm';
import '@xterm/xterm/css/xterm.css';
import { bootInWorker, type WorkerBootResult } from 'emception';
import { useEffect, useRef, useState } from 'react';

const DEFAULT_CODE = `#include <iostream>
#include <string>
int main() {
  std::string name;
  std::cout << "Enter your name: ";
  std::getline(std::cin, name);
  std::cout << "Hello, " << name << "! Welcome to WebAssembly!" << std::endl;
  return 0;
}
`;

export default function Ide() {
    const editorRef = useRef<any>(null);
    const terminalRef = useRef<HTMLDivElement>(null);
    const xtermRef = useRef<Terminal | null>(null);
    const fitAddonRef = useRef<FitAddon | null>(null);
    const orchestratorRef = useRef<WorkerBootResult | null>(null);

    const [status, setStatus] = useState('Initializing...');
    const [isReady, setIsReady] = useState(false);

    useEffect(() => {
        let mounted = true;
        const ts = performance.now();

        const initTerminal = () => {
            if (!terminalRef.current || xtermRef.current) return;

            const term = new Terminal({
                cursorBlink: true,
                scrollback: 10000,
                theme: {
                    background: '#181825',
                    foreground: '#cdd6f4',
                    cursor: '#f5e0dc',
                    selectionBackground: '#585b70',
                },
                fontFamily: 'Menlo, Monaco, "Courier New", monospace',
                fontSize: 14,
            });

            const fitAddon = new FitAddon();
            term.loadAddon(fitAddon);

            term.open(terminalRef.current);

            // Delay fit to ensure DOM is ready and container has dimensions
            const fitTerminal = () => {
                try {
                    if (terminalRef.current && terminalRef.current.clientWidth > 0) {
                        fitAddon.fit();
                    }
                } catch (e) {
                    console.warn('Failed to fit terminal:', e);
                }
            };

            // Initial fit with delay
            setTimeout(fitTerminal, 200);

            xtermRef.current = term;
            fitAddonRef.current = fitAddon;

            // Expose for e2e tests (read terminal buffer reliably)
            (window as any).__xterm__ = term;

            // Handle resize
            window.addEventListener('resize', fitTerminal);

            term.writeln('\x1b[32mWelcome to the Browser C/C++ Toolchain!\x1b[0m');
            term.writeln('Booting system...');

            return () => {
                window.removeEventListener('resize', fitTerminal);
                term.dispose();
                xtermRef.current = null;
                fitAddonRef.current = null;
            };
        };

        const cleanup = initTerminal();

        const startOrchestrator = async () => {
            try {
                if (!terminalRef.current || !xtermRef.current) return;

                // Manifest URL points to public/cdn/manifest.json
                // In Next.js, public/cdn is served at /cdn
                const manifestUrl = '/cdn/manifest.json';

                setStatus('Booting toolchain...');
                const t0 = performance.now();
                // Pass the existing Terminal instance so TTYBridge reuses it
                // instead of creating a second one.
                const result = await bootInWorker(manifestUrl, xtermRef.current);
                console.log(`[Ide] Boot completed in ${(performance.now() - t0).toFixed(1)}ms, mounted=${mounted}`);

                if (mounted) {
                    console.log('[Ide] Setting orchestrator and status to Ready');
                    orchestratorRef.current = result;
                    setStatus('Ready');
                    setIsReady(true);
                    xtermRef.current?.writeln('\x1b[32mSystem Ready.\x1b[0m');
                    xtermRef.current?.writeln('Click "Compile & Run" to execute code.');
                    console.log('[Ide] Status and ready state set, elapsed:', (performance.now() - ts).toFixed(1) + 'ms');
                } else {
                    console.log('[Ide] Component unmounted, skipping status update');
                }
            } catch (err) {
                console.error('Failed to boot:', err);
                if (mounted) {
                    const errMsg = err instanceof Error ? err.message : String(err);
                    console.log('[Ide] Setting status to Error:', errMsg);
                    setStatus(`Error: ${errMsg}`);
                    xtermRef.current?.writeln(`\x1b[31mBoot failed: ${err}\x1b[0m`);
                }
            }
        };

        startOrchestrator();

        return () => {
            mounted = false;
            if (cleanup) cleanup();
            orchestratorRef.current = null;
        };
    }, []);

    const handleEditorDidMount: OnMount = (editor, monaco) => {
        editorRef.current = editor;
    };

    const handleCompile = async () => {
        if (!orchestratorRef.current || !editorRef.current || !xtermRef.current) return;

        const P = '[Emception:IDE]';
        const tTotal = performance.now();
        console.log(`${P} ===== COMPILE & RUN START =====`);

        const code = editorRef.current.getValue();
        const { client, tty } = orchestratorRef.current;

        setStatus('Compiling...');
        tty.clear();
        tty.writeLine('Compiling main.cpp...');
        console.log(`${P} Source code: ${code.length} chars, ${code.split('\n').length} lines`);

        try {
            const enc = new TextEncoder();

            // Write code to the virtual filesystem via the Worker proxy.
            const tWrite = performance.now();
            await client.writeFile('/home/user/main.cpp', enc.encode(code));
            console.log(`${P} Source written to VFS in ${(performance.now() - tWrite).toFixed(1)}ms`);

            const startTime = performance.now();
            console.log(`${P} Step 1/2: Running emcc...`);

            // Compile to standalone WASM (avoids the need for compiler.mjs JS
            // generation, which requires a full Node.js runtime not available in
            // the browser). The resulting WASM uses pure WASI imports and can be
            // executed directly with a minimal WASI runtime.
            const result = await client.run('emcc', [
                'emcc', '/home/user/main.cpp', '-o', '/home/user/main.wasm',
                '-O2',
            ], {
                cwd: '/home/user',
                onStdout: (t) => {
                    console.log(t);
                    tty.writeLine(t);
                },
                onStderr: (t) => {
                    console.error(t);
                    tty.writeError(t);
                }
            });

            const endTime = performance.now();
            const duration = ((endTime - startTime) / 1000).toFixed(2);
            console.log(`${P} emcc finished: exitCode=${result.exitCode}, duration=${duration}s`);

            if (result.exitCode === 0) {
                setStatus(`Compilation successful (${duration}s)`);
                tty.writeLine(`\x1b[32mCompilation successful in ${duration}s\x1b[0m`);

                // Check output file
                const wasmFile = await client.getFile('/home/user/main.wasm');
                console.log(`${P} Compilation output: main.wasm=${wasmFile ? `${(wasmFile.length / 1024).toFixed(1)}KB` : 'MISSING'}`);

                tty.writeLine('Running...');
                console.log(`${P} Step 2/2: Running compiled WASM with WASI runtime...`);
                const tRun = performance.now();

                // Run the standalone WASM directly using the built-in WASI runner.
                // Use tty.write (raw) instead of tty.writeLine — the WASM program
                // already includes its own newlines; convert LF→CRLF for xterm.
                // WorkerClient.feedStdin() handles exclusive stdin and echo
                // automatically when a stdin provider is set.
                await client.run('wasi-run', ['wasi-run', '/home/user/main.wasm'], {
                    cwd: '/home/user',
                    onStdout: (t) => { tty.write(t.replace(/\n/g, '\r\n')); },
                    onStderr: (t) => { tty.write(`\x1b[31m${t.replace(/\n/g, '\r\n')}\x1b[0m`); },
                    stdin: () => tty.readByteExclusive(),
                });

                console.log(`${P} Execution finished in ${((performance.now() - tRun) / 1000).toFixed(2)}s`);

            } else {
                setStatus('Compilation failed');
                tty.writeLine(`\x1b[31mCompilation failed (exit code ${result.exitCode})\x1b[0m`);
                console.error(`${P} Compilation FAILED: exitCode=${result.exitCode}`);
                if (result.stderr) console.error(`${P} stderr: ${result.stderr}`);
            }

            console.log(`${P} ===== COMPILE & RUN COMPLETE in ${((performance.now() - tTotal) / 1000).toFixed(2)}s =====`);

        } catch (e) {
            console.error(`${P} ❌ Exception during compile & run:`, e);
            setStatus('Error during execution');
            tty.writeError(String(e));
        }
    };

    return (
        <div className="flex flex-col h-full w-full">
            <header className="flex items-center justify-between px-4 py-2 bg-[#181825] border-b border-[#313244]">
                <h1 className="text-sm font-semibold text-[#cdd6f4]">WebAssembly C++ Toolchain (Next.js)</h1>
                <div className="flex items-center gap-4">
                    <span data-testid="status" className="text-xs text-[#a6adc8]">{status}</span>
                    <button
                        data-testid="compile-button"
                        onClick={handleCompile}
                        disabled={!isReady}
                        className={`px-3 py-1 text-sm font-medium rounded transition-colors ${isReady
                            ? 'bg-[#a6e3a1] text-[#11111b] hover:opacity-90'
                            : 'bg-[#313244] text-[#585b70] cursor-not-allowed'
                            }`}
                    >
                        Compile & Run
                    </button>
                </div>
            </header>

            <div className="flex-1 flex flex-col md:flex-row overflow-hidden">
                {/* Editor Pane */}
                <div data-testid="editor-pane" className="flex-1 min-h-[300px] md:h-full border-b md:border-b-0 md:border-r border-[#313244]">
                    <Editor
                        height="100%"
                        defaultLanguage="cpp"
                        defaultValue={DEFAULT_CODE}
                        theme="vs-dark"
                        onMount={handleEditorDidMount}
                        options={{
                            minimap: { enabled: false },
                            fontSize: 14,
                            fontFamily: '"Fira Code", monospace',
                            scrollBeyondLastLine: false,
                            automaticLayout: true,
                        }}
                    />
                </div>

                {/* Terminal Pane */}
                <div className="flex-1 min-h-[300px] md:h-full bg-[#1e1e2e] p-2 overflow-hidden flex flex-col">
                    <div className="text-xs text-[#a6adc8] mb-2 px-2">Terminal</div>
                    <div data-testid="terminal" ref={terminalRef} className="flex-1 overflow-hidden rounded bg-[#181825] p-2">
                    </div>
                </div>
            </div>
        </div>
    );
}
