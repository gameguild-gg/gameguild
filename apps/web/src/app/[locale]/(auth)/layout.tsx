import { auth } from '@/auth';
import { FlaskConical, GraduationCap, MessageSquare, Sparkles, UsersRound } from 'lucide-react';
import Link from 'next/link';
import { redirect } from 'next/navigation';
import React from 'react';

const communitySignals = [
  {
    title: 'Learn with structure',
    description: 'Courses and programs stay connected to real project outcomes.',
    icon: GraduationCap,
  },
  {
    title: 'Test with peers',
    description: 'Bring playable builds into review sessions and focused feedback loops.',
    icon: FlaskConical,
  },
  {
    title: 'Build in public',
    description: 'Share progress with members, mentors, and launch-minded creators.',
    icon: UsersRound,
  },
] as const;

export default async function Layout({ children, params }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  const { locale } = await params;

  // Hard guard: redirect authenticated users away from auth pages
  const session = await auth();
  if (session) redirect(`/${locale}`);

  return (
    <div className="relative flex min-h-svh flex-col overflow-hidden bg-slate-950 text-white">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_18%_8%,rgba(56,189,248,0.18),transparent_32%),radial-gradient(circle_at_84%_18%,rgba(168,85,247,0.14),transparent_34%),linear-gradient(180deg,#020617,#0f172a)]" />
      <header className="relative z-10 mx-auto flex w-full max-w-7xl items-center justify-between px-6 py-5">
        <Link href={`/${locale}`} className="flex items-center gap-3 font-semibold">
          <span className="flex size-9 items-center justify-center rounded-xl bg-white text-slate-950">
            <GraduationCap className="size-5" />
          </span>
          GameGuild
        </Link>
        <Link href={`/${locale}/community`} className="hidden rounded-full border border-white/15 px-4 py-2 text-sm font-semibold text-slate-200 transition hover:bg-white/10 sm:inline-flex">
          Community hub
        </Link>
      </header>

      <div className="relative z-10 mx-auto grid w-full max-w-7xl flex-1 items-center gap-10 px-6 py-10 lg:grid-cols-[1.1fr_0.9fr] lg:px-8">
        <section className="hidden max-w-2xl space-y-8 lg:block">
          <div className="space-y-5">
            <div className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/5 px-3 py-2 text-sm text-sky-100">
              <Sparkles className="size-4" />
              Game development community
            </div>
            <h2 className="text-5xl font-semibold leading-tight tracking-tight">
              Join builders turning lessons into playable projects.
            </h2>
            <p className="text-lg leading-8 text-slate-300">
              GameGuild combines learning paths, community critique, Testing Lab feedback, and launch support for people
              who want to make and ship better games.
            </p>
          </div>

          <div className="grid gap-4">
            {communitySignals.map((signal) => {
              const Icon = signal.icon;

              return (
                <article key={signal.title} className="flex gap-4 rounded-3xl border border-white/10 bg-white/[0.04] p-5">
                  <div className="flex size-11 shrink-0 items-center justify-center rounded-2xl bg-sky-300/10 text-sky-200">
                    <Icon className="size-5" />
                  </div>
                  <div>
                    <h2 className="font-semibold text-white">{signal.title}</h2>
                    <p className="mt-1 text-sm leading-6 text-slate-400">{signal.description}</p>
                  </div>
                </article>
              );
            })}
          </div>
        </section>

        <section className="mx-auto flex w-full max-w-md flex-col gap-6">
          {children}
          <div className="rounded-3xl border border-white/10 bg-white/[0.04] p-5 text-sm leading-6 text-slate-300">
            <div className="mb-3 flex items-center gap-2 font-semibold text-white">
              <MessageSquare className="size-4 text-sky-200" />
              Community first
            </div>
            Your account connects the public website, learning dashboard, project showcase, and testing workflows.
          </div>
        </section>
      </div>
    </div>
  );
}
