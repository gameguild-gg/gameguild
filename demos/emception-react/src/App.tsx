import { Ide } from '@gameguild/emception-ui';

export default function App() {
    return (
        <main style={{ height: '100vh', width: '100vw', background: '#1e1e2e', color: '#cdd6f4', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
            <Ide title="WebAssembly C++ Toolchain (React)" />
        </main>
    );
}
