import React from 'react';
import Link from 'next/link';
import { redirect } from 'next/navigation';
import { GalleryVerticalEnd } from 'lucide-react';
import { auth } from '@/auth';

export default async function Layout({ children }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  // Hard guard: redirect authenticated users away from auth pages
  const session = await auth();
  if (session) redirect('/');

  return (
    <div className="bg-muted flex min-h-svh flex-col items-center justify-center gap-6 p-6 md:p-10">
      <div className="flex w-full max-w-sm flex-col gap-6">
        <Link href="/" className="flex items-center gap-2 self-center font-medium">
          <div className="bg-primary text-primary-foreground flex size-6 items-center justify-center rounded-md">
            <GalleryVerticalEnd className="size-4" />
          </div>
          GameGuild
        </Link>
        {children}
      </div>
    </div>
  );
}
