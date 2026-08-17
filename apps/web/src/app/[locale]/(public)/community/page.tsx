import { Link } from '@/i18n/navigation';
import {
  communityGroups,
  publicActivities,
  publicMembers,
  publicPlaytests,
  publicProjects,
} from '@/lib/community/public-community';
import { ArrowRight, CalendarDays, MessageSquare, Sparkles, Users } from 'lucide-react';
import React from 'react';

export default async function CommunityPage(): Promise<React.JSX.Element> {
  return (
    <main className="bg-slate-950 text-white">
      <section className="border-b border-white/10 bg-[radial-gradient(circle_at_12%_0%,rgba(14,165,233,0.16),transparent_34%),radial-gradient(circle_at_88%_12%,rgba(168,85,247,0.14),transparent_30%),linear-gradient(180deg,#111827,#020617)]">
        <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-20 sm:px-6 lg:grid-cols-[0.95fr_1.05fr] lg:px-8">
          <div className="max-w-3xl space-y-6">
            <h1 className="text-5xl font-semibold tracking-tight sm:text-6xl">Community hub</h1>
            <p className="text-lg leading-8 text-slate-300">
              Meet builders, follow project progress, join critique rooms, and move from course exercises to public
              project outcomes with people who can test and review the work.
            </p>
            <div className="flex flex-wrap gap-3">
              <Link
                href="/projects"
                className="inline-flex items-center rounded-full bg-sky-300 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
              >
                Explore projects
                <ArrowRight className="ml-2 size-4" aria-hidden="true" />
              </Link>
              <Link
                href="/testing-lab"
                className="inline-flex items-center rounded-full border border-white/15 px-5 py-3 text-sm font-semibold text-white transition hover:bg-white/10"
              >
                Join a playtest
              </Link>
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <article className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
              <Users className="mb-5 size-6 text-sky-200" aria-hidden="true" />
              <p className="text-3xl font-semibold">{publicMembers.length}</p>
              <p className="mt-1 text-sm text-slate-400">Highlighted members</p>
            </article>
            <article className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
              <Sparkles className="mb-5 size-6 text-violet-200" aria-hidden="true" />
              <p className="text-3xl font-semibold">{publicProjects.length}</p>
              <p className="mt-1 text-sm text-slate-400">Featured projects</p>
            </article>
            <article className="rounded-3xl border border-white/10 bg-white/[0.04] p-5 md:col-span-2">
              <CalendarDays className="mb-5 size-6 text-emerald-200" aria-hidden="true" />
              <p className="text-3xl font-semibold">{publicPlaytests.length}</p>
              <p className="mt-1 text-sm text-slate-400">Upcoming critique and playtest sessions</p>
            </article>
          </div>
        </div>
      </section>

      <section className="mx-auto grid w-full max-w-7xl gap-8 px-4 py-14 sm:px-6 lg:grid-cols-[0.9fr_1.1fr] lg:px-8">
        <div>
          <h2 className="text-3xl font-semibold tracking-tight">Member spotlights</h2>
          <p className="mt-3 max-w-xl text-sm leading-6 text-slate-400">
            Spotlighted members make the community useful by hosting critique, publishing notes, and helping students
            frame their work as professional proof.
          </p>
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          {publicMembers.map((member) => (
            <article key={member.handle} className="rounded-3xl border border-white/10 bg-slate-900/70 p-5">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-sky-200">{member.handle}</p>
              <h3 className="mt-3 text-xl font-semibold text-white">{member.name}</h3>
              <p className="mt-1 text-sm text-slate-400">
                {member.role} - {member.focus}
              </p>
              <p className="mt-4 text-sm leading-6 text-slate-300">{member.contribution}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="border-y border-white/10 bg-white/[0.03]">
        <div className="mx-auto grid w-full max-w-7xl gap-8 px-4 py-14 sm:px-6 lg:grid-cols-3 lg:px-8">
          {communityGroups.map((group) => (
            <article key={group.name} className="rounded-3xl border border-white/10 bg-slate-900/70 p-6">
              <h2 className="text-xl font-semibold text-white">{group.name}</h2>
              <p className="mt-3 text-sm leading-6 text-slate-400">{group.description}</p>
              <p className="mt-5 text-sm font-semibold text-sky-200">{group.members}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="mx-auto grid w-full max-w-7xl gap-8 px-4 py-14 sm:px-6 lg:grid-cols-[0.85fr_1.15fr] lg:px-8">
        <div className="max-w-xl">
          <div className="mb-4 flex items-center gap-3 text-sky-200">
            <MessageSquare className="size-5" aria-hidden="true" />
            <h2 className="text-3xl font-semibold tracking-tight text-white">Recent activity</h2>
          </div>
          <p className="text-sm leading-6 text-slate-400">
            Public activity helps visitors understand that GameGuild is not just a catalog. It is a working community
            where project updates, testing notes, and portfolio changes are visible.
          </p>
        </div>
        <div className="space-y-3">
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
      </section>
    </main>
  );
}
