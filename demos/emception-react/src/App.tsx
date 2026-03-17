import { Ide } from '@gameguild/emception-ui';

// Vite injects BASE_URL from the `base` config (e.g. '/gameguild/' on GitHub Pages).
// Use it to build the manifest URL so the CDN is found regardless of deploy path.
const manifestUrl = `${import.meta.env.BASE_URL}cdn/manifest.json`;

export default function App() {
    return (
        <main style={{ height: '100vh', width: '100vw', background: '#1e1e2e', color: '#cdd6f4', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
            <Ide title="WebAssembly C++ Toolchain (React)" manifestUrl={manifestUrl} />
        </main>
    );
}
