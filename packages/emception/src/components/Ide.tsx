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

export interface IdeProps {
    /** Title displayed in the header bar. */
    title?: string;
    /** URL to the CDN manifest.json file. Defaults to '/cdn/manifest.json'. */
    manifestUrl?: string;
}

export default function Ide({ title = 'WebAssembly C++ Toolchain', manifestUrl = '/cdn/manifest.json' }: IdeProps) {
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

            const fitTerminal = () => {
                try {
                    if (terminalRef.current && terminalRef.current.clientWidth > 0) {
                        fitAddon.fit();
                    }
                } catch (e) {
                    console.warn('Failed to fit terminal:', e);
                }
            };

            setTimeout(fitTerminal, 200);

            xtermRef.current = term;
            fitAddonRef.current = fitAddon;

            // Expose for e2e tests
            (window as any).__xterm__ = term;

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

                setStatus('Booting toolchain...');
                const t0 = performance.now();
                const result = await bootInWorker(manifestUrl, xtermRef.current);
                console.log(`[Ide] Boot completed in ${(performance.now() - t0).toFixed(1)}ms, mounted=${mounted}`);

                if (mounted) {
                    orchestratorRef.current = result;
                    setStatus('Ready');
                    setIsReady(true);
                    xtermRef.current?.writeln('\x1b[32mSystem Ready.\x1b[0m');
                    xtermRef.current?.writeln('Click "Compile & Run" to execute code.');
                    console.log('[Ide] Status and ready state set, elapsed:', (performance.now() - ts).toFixed(1) + 'ms');
                }
            } catch (err) {
                console.error('Failed to boot:', err);
                if (mounted) {
                    const errMsg = err instanceof Error ? err.message : String(err);
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
    }, [manifestUrl]);

    const handleEditorDidMount: OnMount = (editor) => {
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

            const tWrite = performance.now();
            await client.writeFile('/home/user/main.cpp', enc.encode(code));
            console.log(`${P} Source written to VFS in ${(performance.now() - tWrite).toFixed(1)}ms`);

            const startTime = performance.now();
            console.log(`${P} Step 1/2: Running emcc...`);

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

                const wasmFile = await client.getFile('/home/user/main.wasm');
                console.log(`${P} Compilation output: main.wasm=${wasmFile ? `${(wasmFile.length / 1024).toFixed(1)}KB` : 'MISSING'}`);

                tty.writeLine('Running...');
                console.log(`${P} Step 2/2: Running compiled WASM with WASI runtime...`);
                const tRun = performance.now();

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
        <div className="emception-ide" style={{ display: 'flex', flexDirection: 'column', height: '100%', width: '100%' }}>
            <header style={{
                display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                padding: '0.5rem 1rem', background: '#181825', borderBottom: '1px solid #313244',
            }}>
                <h1 style={{ fontSize: '0.875rem', fontWeight: 600, color: '#cdd6f4', margin: 0 }}>{title}</h1>
                <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                    <span data-testid="status" style={{ fontSize: '0.75rem', color: '#a6adc8' }}>{status}</span>
                    <button
                        data-testid="compile-button"
                        onClick={handleCompile}
                        disabled={!isReady}
                        style={{
                            padding: '0.25rem 0.75rem', fontSize: '0.875rem', fontWeight: 500,
                            borderRadius: '0.25rem', border: 'none', cursor: isReady ? 'pointer' : 'not-allowed',
                            background: isReady ? '#a6e3a1' : '#313244',
                            color: isReady ? '#11111b' : '#585b70',
                            transition: 'opacity 0.15s',
                        }}
                    >
                        Compile & Run
                    </button>
                </div>
            </header>

            <div style={{ flex: 1, display: 'flex', flexDirection: 'row', overflow: 'hidden' }}>
                {/* Editor Pane */}
                <div data-testid="editor-pane" style={{ flex: 1, minHeight: 300, borderRight: '1px solid #313244' }}>
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
                <div style={{ flex: 1, minHeight: 300, background: '#1e1e2e', padding: '0.5rem', overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
                    <div style={{ fontSize: '0.75rem', color: '#a6adc8', marginBottom: '0.5rem', paddingLeft: '0.5rem' }}>Terminal</div>
                    <div data-testid="terminal" ref={terminalRef} style={{ flex: 1, overflow: 'hidden', borderRadius: '0.25rem', background: '#181825', padding: '0.5rem' }}>
                    </div>
                </div>
            </div>
        </div>
    );
}
