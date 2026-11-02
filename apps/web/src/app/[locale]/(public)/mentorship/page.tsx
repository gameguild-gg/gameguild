import { Link } from '@/i18n/navigation';
import { ArrowRight, CalendarClock, Compass, Sparkles, UserRound } from 'lucide-react';
import React from 'react';
import { listMentors } from '@/lib/mentorship/actions';

const roleLabels: Record<number, string> = {
  0: 'Mentor',
  1: 'Advisor',
  2: 'Matchmaker',
};

export const dynamic = 'force-dynamic';

/**
 * Public mentorship directory (issue #102): advisees find published mentors and
 * advisors by skill and book an introductory meeting.
 */
export default async function MentorshipDirectoryPage({
    searchParams,
}: {
    searchParams: Promise<{ skill?: string; search?: string }>;
}): Promise<React.JSX.Element> {
    const { skill, search } = await searchParams;
    const mentors = await listMentors({ skill, search });

    return (
        <main className="bg-slate-950 text-white">
            <section className="border-b border-white/10 bg-[radial-gradient(circle_at_20%_0%,rgba(56,189,248,0.14),transparent_32%),linear-gradient(180deg,#0f172a,#020617)]">
                <div className="mx-auto w-full max-w-7xl space-y-6 px-4 py-16 sm:px-6 lg:px-8">
                    <p className="text-sm font-semibold uppercase tracking-[0.24em] text-sky-300">Mentorship program</p>
                    <h1 className="max-w-3xl text-4xl font-semibold tracking-tight sm:text-5xl">
                        Find a mentor or advisor who has shipped what you are building
                    </h1>
                    <p className="max-w-3xl text-base leading-7 text-slate-300">
                        Mentors and advisors publish their availability; you book an introductory conversation —
                        including a free 10-15 minute call to learn how GameGuild works. Filter by skill to find the
                        right match, or book time with a matchmaker who will pair you up.
                    </p>
                    <form className="flex max-w-xl gap-2" action="/mentorship" method="get">
                        <div className="relative flex-1">
                            <Compass className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-500" aria-hidden="true" />
                            <input
                                type="search"
                                name="skill"
                                defaultValue={skill ?? ''}
                                placeholder="Filter by skill, e.g. shaders, level design, pitching"
                                className="h-11 w-full rounded-full border border-white/15 bg-white/5 pl-9 pr-4 text-sm text-white placeholder:text-slate-500 focus:border-sky-300 focus:outline-none"
                            />
                        </div>
                        <button
                            type="submit"
                            className="inline-flex h-11 items-center rounded-full bg-sky-300 px-5 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
                        >
                            Search
                        </button>
                    </form>
                </div>
            </section>

            <section className="mx-auto w-full max-w-7xl px-4 py-14 sm:px-6 lg:px-8">
                <div className="mb-8 flex items-end justify-between gap-4">
                    <h2 className="text-2xl font-semibold tracking-tight">
                        {mentors.length} {mentors.length === 1 ? 'mentor' : 'mentors'} available
                        {skill ? ` for “${skill}”` : ''}
                    </h2>
                    <Link
                        href="/community"
                        className="inline-flex items-center gap-1 text-sm font-medium text-sky-200 hover:text-sky-100"
                    >
                        Community hub
                        <ArrowRight className="size-4" aria-hidden="true" />
                    </Link>
                </div>

                {mentors.length === 0 ? (
                    <div className="rounded-3xl border border-white/10 bg-slate-900/70 p-10 text-center">
                        <UserRound className="mx-auto mb-4 size-10 text-slate-500" aria-hidden="true" />
                        <h3 className="text-lg font-semibold text-white">No mentors match yet</h3>
                        <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-400">
                            Try a broader skill filter, or check back soon — new mentors and advisors publish
                            availability as the program grows.
                        </p>
                    </div>
                ) : (
                    <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
                        {mentors.map((mentor) => (
                            <article
                                key={mentor.id}
                                className="flex flex-col rounded-3xl border border-white/10 bg-slate-900/70 p-6"
                            >
                                <div className="mb-3 flex flex-wrap gap-2">
                                    <span className="rounded-full bg-sky-300/10 px-3 py-1 text-xs font-semibold text-sky-200">
                                        {roleLabels[mentor.role] ?? 'Mentor'}
                                    </span>
                                    {mentor.offersIntroductoryCall ? (
                                        <span className="inline-flex items-center gap-1 rounded-full bg-emerald-300/10 px-3 py-1 text-xs font-semibold text-emerald-200">
                                            <Sparkles className="size-3" aria-hidden="true" />
                                            Free {mentor.introductoryCallMinutes}-min intro
                                        </span>
                                    ) : null}
                                    {mentor.bookingMode === 1 ? (
                                        <span className="inline-flex items-center gap-1 rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-slate-300">
                                            <CalendarClock className="size-3" aria-hidden="true" />
                                            Matchmaker-managed
                                        </span>
                                    ) : null}
                                </div>
                                <h3 className="text-xl font-semibold text-white">{mentor.displayName}</h3>
                                {mentor.headline ? (
                                    <p className="mt-1 text-sm font-medium text-slate-300">{mentor.headline}</p>
                                ) : null}
                                {mentor.bio ? <p className="mt-3 line-clamp-3 text-sm leading-6 text-slate-400">{mentor.bio}</p> : null}
                                {mentor.skills.length > 0 ? (
                                    <ul className="mt-4 flex flex-wrap gap-1.5" aria-label="Skills">
                                        {mentor.skills.map((skillTag) => (
                                            <li
                                                key={skillTag.id}
                                                className="rounded-full border border-white/10 px-2.5 py-0.5 text-xs text-slate-300"
                                            >
                                                {skillTag.skill}
                                            </li>
                                        ))}
                                    </ul>
                                ) : null}
                                <div className="mt-6 flex-1" />
                                <Link
                                    href={`/mentorship/${mentor.id}`}
                                    className="inline-flex items-center text-sm font-semibold text-sky-200 hover:text-sky-100"
                                >
                                    View profile and availability
                                    <ArrowRight className="ml-1.5 size-4" aria-hidden="true" />
                                </Link>
                            </article>
                        ))}
                    </div>
                )}
            </section>
        </main>
    );
}
