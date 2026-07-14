// Next.js 15 IDE embed — App Router pattern.
//
// The IDE requires the browser so the component must be a Client Component.
// All heavy imports are wrapped in a dynamic() boundary with ssr:false so
// Next.js does not try to render them on the server.
//
// File layout:
//   app/ide/page.tsx     — server component, sets metadata
//   components/IdePanel.tsx  ← this file (client component)

'use client';

import dynamic from 'next/dynamic';

// The `Ide` component renders only in the browser: it uses canvas, Web
// Workers, and SharedArrayBuffer. `ssr: false` prevents a hydration mismatch.
const Ide = dynamic(() => import('@gameguild/emception-ide').then((m) => m.Ide), {
    ssr: false,
    loading: () => <p className="p-4 text-sm text-gray-500">Loading IDE…</p>,
});

const STARTER_SOURCE = `#include <stdio.h>
int main() {
  printf("hello from Next.js IDE\\n");
  return 0;
}
`;

export function IdePanel() {
    return (
        <div className="h-[calc(100dvh-4rem)]">
            <Ide
                workspaceName="next-basic-demo"
                defaultFiles={{
                    'main.cpp': { content: STARTER_SOURCE, visibility: 'public' },
                }}
            // A root-level /coi-serviceworker.js must be registered in next.config.ts
            // / a layout script for SharedArrayBuffer to work.
            />
        </div>
    );
}

// --- Usage in page.tsx --------------------------------------------------
//
// import { IdePanel } from '@/components/IdePanel';
// export const metadata = { title: 'Online C/C++ IDE' };
// export default function IdePage() { return <IdePanel />; }
