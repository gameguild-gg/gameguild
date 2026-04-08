'use client';

import { PRESETS } from '@gameguild/emception-ui';
import dynamic from 'next/dynamic';
import { useSearchParams } from 'next/navigation';

const Ide = dynamic(() => import('@gameguild/emception-ui').then((m) => ({ default: m.Ide })), { ssr: false });

export default function Home() {
  const params = useSearchParams();
  const wsId = params.get('workspace');
  const workspaceConfig = wsId && PRESETS[wsId] ? PRESETS[wsId] : undefined;
  return (
    <main className="h-screen w-screen bg-[#1e1e2e] text-[#cdd6f4] flex flex-col overflow-hidden">
      <Ide title="Emception (Next.js)" workspaceConfig={workspaceConfig} />
    </main>
  );
}
