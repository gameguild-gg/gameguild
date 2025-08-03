import React, { PropsWithChildren } from 'react';
import { redirect } from 'next/navigation';
import { GalleryVerticalEnd } from 'lucide-react';
import { auth } from '@/auth';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  const session = await auth();

  if (session) redirect('/');

  return (
    <div className="flex flex-col flex-1 items-center justify-center gap-6 p-6 md:p-10 min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900 relative overflow-hidden">
      {/* Background decorative elements */}
      <div className="absolute inset-0 overflow-hidden">
        {/* Large background glow */}
        <div className="absolute bottom-0 left-1/2 transform -translate-x-1/2 translate-y-1/2">
          <div className="w-96 h-96 rounded-full bg-gradient-to-t from-blue-600/20 via-purple-500/10 to-transparent blur-3xl"></div>
        </div>

        {/* Top edge glow */}
        <div className="absolute top-0 left-0 w-full h-px bg-gradient-to-r from-transparent via-blue-400/30 to-transparent"></div>
      </div>

      <div className="flex-col flex w-full max-w-sm gap-6 relative z-10">
        <a href="#" className="flex items-center gap-2 self-center font-medium text-white hover:scale-105 transition-transform">
          {/*TODO Change to the community logo*/}
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-blue-500 to-purple-500 text-white shadow-lg">
            <GalleryVerticalEnd className="size-5" />
          </div>
          <span className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Game Guild.</span>
        </a>
        {children}
      </div>
    </div>
  );
}
