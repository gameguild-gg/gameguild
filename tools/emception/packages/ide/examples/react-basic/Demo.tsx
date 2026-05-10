/**
 * @example React basic embed
 *
 * Minimal <Ide> usage inside a React 19 / Next.js app.
 * Run this file via a Vite or Next.js dev server; it does NOT compile standalone.
 *
 * Prerequisites:
 *   npm install @gameguild/emception-ide @gameguild/emception-browser react react-dom
 *
 * Serve your sysroot bundles under /cdn/ (see @emception/cli cdn-export).
 */
import { Ide } from '@gameguild/emception-ide';
import { createRoot } from 'react-dom/client';

function App() {
    return (
        <div style={{ width: '100vw', height: '100vh' }}>
            <Ide
                title="C++ Playground"
                manifestUrl="/cdn/manifest.json"
                workspaceName="react-basic-demo"
                enableFileExplorer={true}
                enableTerminal={true}
                enableCanvas={true}
            />
        </div>
    );
}

// Standalone mount — remove if embedding inside an existing app.
const root = createRoot(document.getElementById('root')!);
root.render(<App />);
