import { Link } from '@/i18n/navigation';
import { publicActivities, publicPlaytests, publicProjects } from '@/lib/community/public-community';
import { ArrowRight, CalendarDays, MessageSquare } from 'lucide-react';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <main className="bg-slate-950 text-white">
      <section className="border-b border-white/10 bg-[linear-gradient(180deg,#0f172a,#020617)]">
        <div className="mx-auto w-full max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
          <h1 className="text-5xl font-semibold tracking-tight">Community feed</h1>
          <p className="mt-5 max-w-2xl text-lg leading-8 text-slate-300">
            Follow public project activity, testing sessions, and member updates from the GameGuild community.
          </p>
        </div>
      </section>

      <section className="mx-auto grid w-full max-w-7xl gap-8 px-4 py-14 sm:px-6 lg:grid-cols-[1fr_0.8fr] lg:px-8">
        <div className="space-y-4">
          <div className="mb-2 flex items-center gap-3">
            <MessageSquare className="size-5 text-sky-200" aria-hidden="true" />
            <h2 className="text-2xl font-semibold">Latest updates</h2>
          </div>
          {publicActivities.map((activity) => (
            <Link
              key={`${activity.actor}-${activity.target}`}
              href={activity.href}
              className="block rounded-3xl border border-white/10 bg-slate-900/70 p-5 transition hover:border-white/20"
            >
              <p className="text-sm leading-6 text-slate-300">
                <span className="font-semibold text-white">{activity.actor}</span> {activity.action}{' '}
                <span className="font-semibold text-sky-200">{activity.target}</span>
              </p>
            </Link>
          ))}
        </div>

        <aside className="space-y-5">
          <div className="rounded-3xl border border-white/10 bg-slate-900/70 p-6">
            <div className="mb-5 flex items-center gap-3">
              <CalendarDays className="size-5 text-sky-200" aria-hidden="true" />
              <h2 className="text-xl font-semibold">Upcoming playtests</h2>
            </div>
            <div className="space-y-3">
              {publicPlaytests.map((playtest) => (
                <Link key={playtest.title} href={playtest.href} className="block rounded-2xl border border-white/10 bg-white/[0.03] p-4 transition hover:border-white/20">
                  <p className="font-semibold text-white">{playtest.title}</p>
                  <p className="mt-1 text-sm text-slate-400">{playtest.date}</p>
                </Link>
              ))}
            </div>
          </div>

          <div className="rounded-3xl border border-white/10 bg-slate-900/70 p-6">
            <h2 className="text-xl font-semibold">Featured projects</h2>
            <div className="mt-5 space-y-3">
              {publicProjects.map((project) => (
                <Link key={project.slug} href={`/projects/${project.slug}`} className="group flex items-center justify-between gap-4 rounded-2xl border border-white/10 bg-white/[0.03] p-4 transition hover:border-white/20">
                  <span className="text-sm font-semibold text-white">{project.title}</span>
                  <ArrowRight className="size-4 text-sky-200 transition group-hover:translate-x-1" aria-hidden="true" />
                </Link>
              ))}
            </div>
          </div>
        </aside>
      </section>
    </main>
  );
}
