'use client';

import dynamic from 'next/dynamic';

const Ide = dynamic(() => import('@gameguild/emception-ui').then(m => ({ default: m.Ide })), { ssr: false });

export default function Home() {
    return (
        <main className="h-screen w-screen bg-[#1e1e2e] text-[#cdd6f4] flex flex-col overflow-hidden">
            <Ide title="WebAssembly C++ Toolchain (Next.js)" />
        </main>
    );
}
