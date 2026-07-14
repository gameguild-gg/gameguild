'use client';

import { PRESETS } from '@game-guild/emception-ui';
import dynamic from 'next/dynamic';
import { useSearchParams } from 'next/navigation';
import { Suspense } from 'react';

const Ide = dynamic(() => import('@game-guild/emception-ui').then((m) => ({ default: m.Ide })), { ssr: false });

function EmceptionDemo() {
  const params = useSearchParams();
  const wsId = params.get('workspace');
  const workspaceConfig = wsId && PRESETS[wsId] ? PRESETS[wsId] : undefined;
  return (
    <main className="h-screen w-screen bg-[#1e1e2e] text-[#cdd6f4] flex flex-col overflow-hidden">
      <Ide title="Emception (Next.js)" workspaceConfig={workspaceConfig} />
    </main>
  );
}

export default function Home() {
  return (
    <Suspense fallback={<main className="h-screen w-screen bg-[#1e1e2e]" />}>
      <EmceptionDemo />
    </Suspense>
  );
}
