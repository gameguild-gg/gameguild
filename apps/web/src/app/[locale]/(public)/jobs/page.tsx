import { Link } from '@/i18n/navigation';
import { communityOpportunities } from '@/lib/community/public-community';
import { ArrowRight, BriefcaseBusiness, ClipboardCheck, MessageSquareText, UsersRound } from 'lucide-react';
import React from 'react';

const contributionTracks = [
  {
    title: 'Mentor builders',
    description: 'Help students turn rough prototypes into readable, testable project milestones.',
    icon: UsersRound,
  },
  {
    title: 'Review projects',
    description: 'Give structured notes on playability, portfolio clarity, and launch readiness.',
    icon: ClipboardCheck,
  },
  {
    title: 'Support community sessions',
    description: 'Facilitate feedback rooms, async testing rounds, and critique summaries.',
    icon: MessageSquareText,
  },
] as const;

export default async function JobsPage(): Promise<React.JSX.Element> {
  return (
    <main className="bg-slate-950 text-white">
      <section className="border-b border-white/10 bg-[radial-gradient(circle_at_16%_0%,rgba(56,189,248,0.14),transparent_32%),linear-gradient(180deg,#0f172a,#020617)]">
        <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-20 sm:px-6 lg:grid-cols-[0.85fr_1.15fr] lg:px-8">
          <div className="max-w-3xl space-y-6">
            <h1 className="text-5xl font-semibold tracking-tight sm:text-6xl">Community opportunities</h1>
            <p className="text-lg leading-8 text-slate-300">
              GameGuild grows through mentors, reviewers, maintainers, and launch-support contributors. These roles
              are practical ways to help members improve projects while building visible professional credibility.
            </p>
            <div className="flex flex-wrap gap-3">
              <Link
                href="/community"
                className="inline-flex items-center rounded-full bg-sky-300 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
              >
                Visit community hub
                <ArrowRight className="ml-2 size-4" aria-hidden="true" />
              </Link>
              <Link
                href="/projects"
                className="inline-flex items-center rounded-full border border-white/15 px-5 py-3 text-sm font-semibold text-white transition hover:bg-white/10"
              >
                Review projects
              </Link>
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            {contributionTracks.map((track) => {
              const Icon = track.icon;

              return (
                <article key={track.title} className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
                  <div className="mb-5 flex size-11 items-center justify-center rounded-2xl bg-sky-300/10 text-sky-200">
                    <Icon className="size-5" aria-hidden="true" />
                  </div>
                  <h2 className="text-lg font-semibold text-white">{track.title}</h2>
                  <p className="mt-3 text-sm leading-6 text-slate-400">{track.description}</p>
                </article>
              );
            })}
          </div>
        </div>
      </section>

      <section className="mx-auto w-full max-w-7xl px-4 py-14 sm:px-6 lg:px-8">
        <div className="mb-8 flex flex-col justify-between gap-4 lg:flex-row lg:items-end">
          <div className="max-w-2xl">
            <h2 className="text-3xl font-semibold tracking-tight">Open contribution paths</h2>
            <p className="mt-3 text-sm leading-6 text-slate-400">
              These are community roles, not generic job-board listings. Each path has a clear commitment, visible
              contribution type, and a connection to learning, testing, or launch operations.
            </p>
          </div>
          <div className="inline-flex items-center gap-2 rounded-full border border-white/10 px-4 py-2 text-sm text-slate-300">
            <BriefcaseBusiness className="size-4 text-sky-200" aria-hidden="true" />
            Community-first roles
          </div>
        </div>

        <div className="grid gap-5 lg:grid-cols-3">
          {communityOpportunities.map((opportunity) => (
            <article key={opportunity.title} className="rounded-3xl border border-white/10 bg-slate-900/70 p-6">
              <div className="mb-5 flex flex-wrap gap-2">
                <span className="rounded-full bg-sky-300/10 px-3 py-1 text-xs font-semibold text-sky-200">
                  {opportunity.type}
                </span>
                <span className="rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-slate-300">
                  {opportunity.commitment}
                </span>
              </div>
              <h3 className="text-2xl font-semibold text-white">{opportunity.title}</h3>
              <p className="mt-4 text-sm leading-6 text-slate-400">{opportunity.description}</p>
              <Link href="/contact" className="mt-6 inline-flex items-center text-sm font-semibold text-sky-200">
                Register interest
                <ArrowRight className="ml-2 size-4" aria-hidden="true" />
              </Link>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}
