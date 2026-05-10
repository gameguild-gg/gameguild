/**
 * @example Injected API — headless I/O
 *
 * Shows how to pass a pre-booted EmceptionAPI into <Ide> so the component
 * skips its own boot phase. Useful when you need to share one API instance
 * across multiple components or when you want to intercept stdout/stderr.
 *
 * Prerequisites:
 *   npm install @gameguild/emception-ide @gameguild/emception-browser react react-dom
 */
import { createEmception } from '@gameguild/emception-browser';
import { Ide, type InjectedEmceptionAPI } from '@gameguild/emception-ide';
import { useEffect, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';

function App() {
    const [api, setApi] = useState<InjectedEmceptionAPI | null>(null);
    const [log, setLog] = useState<string[]>([]);
    const logRef = useRef(log);
    logRef.current = log;

    useEffect(() => {
        let disposed = false;
        createEmception({
            manifestUrl: '/cdn/manifest.json',
            tty: 'none',
        }).then((em) => {
            if (disposed) {
                em.dispose();
                return;
            }
            setApi(em);
        });
        return () => {
            disposed = true;
        };
    }, []);

    if (!api) return <div>Booting…</div>;

    return (
        <div style={{ width: '100vw', height: '100vh' }}>
            <Ide
                api={api}
                title="Injected API Demo"
                enableTerminal={false}
                onStdout={(line) => setLog((prev) => [...prev, `[out] ${line}`])}
                onStderr={(line) => setLog((prev) => [...prev, `[err] ${line}`])}
            />
            <pre
                style={{
                    position: 'fixed',
                    bottom: 0,
                    left: 0,
                    right: 0,
                    maxHeight: '30vh',
                    overflow: 'auto',
                    background: '#111',
                    color: '#ccc',
                    padding: '0.5rem',
                    margin: 0,
                }}
            >
                {log.join('\n')}
            </pre>
        </div>
    );
}

const root = createRoot(document.getElementById('root')!);
root.render(<App />);
