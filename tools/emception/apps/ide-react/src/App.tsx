import type { WorkspaceConfig } from '@gameguild/emception-ide';
import { Ide, PRESETS } from '@gameguild/emception-ide';

// Vite injects BASE_URL from the `base` config (e.g. '/gameguild/' on GitHub Pages).
// Use it to build the manifest URL so the CDN is found regardless of deploy path.
const manifestUrl = import.meta.env.VITE_EMCEPTION_MANIFEST_URL || 'https://gameguild-gg.github.io/gameguild/cdn/manifest.json';

function getWorkspaceFromUrl(): WorkspaceConfig | undefined {
  const params = new URLSearchParams(window.location.search);
  const wsId = params.get('workspace');
  if (wsId && PRESETS[wsId]) return PRESETS[wsId];
  return undefined;
}

export default function App() {
  const workspaceConfig = getWorkspaceFromUrl();
  return (
    <main style={{ height: '100vh', width: '100vw', background: '#1e1e2e', color: '#cdd6f4', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
      <Ide title="Emception (React)" manifestUrl={manifestUrl} workspaceConfig={workspaceConfig} />
    </main>
  );
}
