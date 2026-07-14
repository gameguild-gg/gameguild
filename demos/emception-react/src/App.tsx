import type { WorkspaceConfig } from '@game-guild/emception-ui';
import { Ide, PRESETS } from '@game-guild/emception-ui';

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
      <Ide title="Emception (React)" workspaceConfig={workspaceConfig} />
    </main>
  );
}
