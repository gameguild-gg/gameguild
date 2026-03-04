'use client';

import dynamic from 'next/dynamic';

const Ide = dynamic(() => import('@/components/Ide'), { ssr: false });

export default function Home() {
  return (
    <main className="h-screen w-screen bg-[#1e1e2e] text-[#cdd6f4] flex flex-col overflow-hidden">
      <Ide />
    </main>
  );
}
