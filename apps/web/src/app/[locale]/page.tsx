import { Link } from '@/i18n';
import React from 'react';

export default async function Page({ params }: PageProps<'/[locale]'>): Promise<React.JSX.Element> {
  return (
    <main className="container mx-auto flex min-h-svh max-w-4xl flex-col justify-center gap-8 px-4 py-16">
      <div className="space-y-4">
        <p className="text-sm font-medium uppercase tracking-[0.2em] text-muted-foreground">Game Guild</p>
        <h1 className="text-4xl font-semibold tracking-tight sm:text-5xl">Home</h1>
        <p className="max-w-2xl text-lg text-muted-foreground">
          Temporary public home while proxy auth is disabled. You can browse the public feed, learn more about the
          platform, or sign in when you need access to protected areas.
        </p>
      </div>

      <div className="flex flex-wrap gap-3">
        <Link
          href="/feed"
          className="inline-flex items-center rounded-md bg-foreground px-4 py-2 text-sm font-medium text-background"
        >
          Open feed
        </Link>
        <Link
          href="/about"
          className="inline-flex items-center rounded-md border border-border px-4 py-2 text-sm font-medium"
        >
          About
        </Link>
        <Link
          href="/sign-in"
          className="inline-flex items-center rounded-md border border-border px-4 py-2 text-sm font-medium"
        >
          Sign in
        </Link>
      </div>
    </main>
  );
}
