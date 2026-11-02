import { Link } from '@/i18n/navigation';
import { ArrowRight, BookOpenCheck, ClipboardList, GraduationCap, PenLine, UsersRound, Video } from 'lucide-react';
import React from 'react';

const processSteps = [
  {
    title: '1. Apply',
    description:
      'Send your application with the content area you want to cover, a short portfolio, and the first course you would like to build.',
    icon: ClipboardList,
  },
  {
    title: '2. Set up your creator profile',
    description:
      'Once approved, you get a creator profile with your handle, bio, links, and the formats you prefer to publish in.',
    icon: UsersRound,
  },
  {
    title: '3. Build your first course',
    description:
      'Work with our editorial team to structure the course, record or write the lessons, and publish it to the community.',
    icon: GraduationCap,
  },
  {
    title: '4. Grow with the community',
    description:
      'Learners review and discuss your content, and you keep improving it with feedback from real game-dev students.',
    icon: BookOpenCheck,
  },
] as const;

const creatorTracks = [
  {
    title: 'Courses and programs',
    description: 'Structured learning paths with assignments, quizzes, and a final project learners can showcase.',
    icon: GraduationCap,
  },
  {
    title: 'Written content',
    description: 'Deep-dive articles, devlogs, and guides published on the GameGuild blog and content hub.',
    icon: PenLine,
  },
  {
    title: 'Workshops and videos',
    description: 'Live sessions, recorded lessons, and community talks that complement the course catalog.',
    icon: Video,
  },
] as const;

/**
 * "Be a content creator" process page (issue #105): explains what creating content
 * with GameGuild means and links to the multi-step application wizard.
 */
export default function ContentCreatorsPage(): React.JSX.Element {
  return (
    <main className="bg-slate-950 text-white">
      <section className="border-b border-white/10 bg-[radial-gradient(circle_at_16%_0%,rgba(56,189,248,0.14),transparent_32%),linear-gradient(180deg,#0f172a,#020617)]">
        <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-20 sm:px-6 lg:grid-cols-[1.1fr_0.9fr] lg:px-8">
          <div className="max-w-3xl space-y-6">
            <p className="text-sm font-semibold uppercase tracking-[0.24em] text-sky-300">Content creators</p>
            <h1 className="text-5xl font-semibold tracking-tight sm:text-6xl">Create game dev content with GameGuild</h1>
            <p className="text-lg leading-8 text-slate-300">
              GameGuild grows because experienced developers share what they know. As a content creator you publish
              courses, articles, and workshops for a community of learners who are building real games — with
              editorial support, review, and a clear onboarding process behind you.
            </p>
            <div className="flex flex-wrap gap-3">
              <Link
                href="/content-creators/apply"
                className="inline-flex items-center rounded-full bg-sky-300 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
              >
                Apply to become a creator
                <ArrowRight className="ml-2 size-4" aria-hidden="true" />
              </Link>
              <Link
                href="/courses"
                className="inline-flex items-center rounded-full border border-white/15 px-5 py-3 text-sm font-semibold text-white transition hover:bg-white/10"
              >
                Explore existing courses
              </Link>
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            {creatorTracks.map((track) => {
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

      <section className="mx-auto w-full max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
        <div className="mb-10 max-w-2xl">
          <h2 className="text-3xl font-semibold tracking-tight">The creator process</h2>
          <p className="mt-3 text-sm leading-6 text-slate-400">
            From application to your first published course, the process is structured so you always know what comes
            next. Every application is reviewed by the GameGuild team.
          </p>
        </div>

        <ol className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
          {processSteps.map((step) => {
            const Icon = step.icon;
            return (
              <li key={step.title} className="rounded-3xl border border-white/10 bg-slate-900/70 p-6">
                <div className="mb-5 flex size-11 items-center justify-center rounded-2xl bg-sky-300/10 text-sky-200">
                  <Icon className="size-5" aria-hidden="true" />
                </div>
                <h3 className="text-lg font-semibold text-white">{step.title}</h3>
                <p className="mt-3 text-sm leading-6 text-slate-400">{step.description}</p>
              </li>
            );
          })}
        </ol>
      </section>

      <section className="border-t border-white/10 bg-slate-900/40">
        <div className="mx-auto grid w-full max-w-7xl gap-8 px-4 py-16 sm:px-6 lg:grid-cols-3 lg:px-8">
          <div className="space-y-3">
            <h3 className="text-xl font-semibold text-white">What we look for</h3>
            <ul className="space-y-2 text-sm leading-6 text-slate-400">
              <li>Hands-on game development experience you can show.</li>
              <li>Clear communication — in writing, on video, or both.</li>
              <li>Willingness to iterate on content with learner feedback.</li>
              <li>Respect for beginners: patience and structured explanations.</li>
            </ul>
          </div>
          <div className="space-y-3">
            <h3 className="text-xl font-semibold text-white">What you get</h3>
            <ul className="space-y-2 text-sm leading-6 text-slate-400">
              <li>Editorial review and course-structure support.</li>
              <li>A public creator profile linked from every piece of content.</li>
              <li>Community feedback, ratings, and discussion threads.</li>
              <li>Visibility across the GameGuild catalog and community hub.</li>
            </ul>
          </div>
          <div className="space-y-3">
            <h3 className="text-xl font-semibold text-white">Ready to start?</h3>
            <p className="text-sm leading-6 text-slate-400">
              The application takes about ten minutes: introduce yourself, set up your creator profile, and sketch
              your first course. You can submit the three steps in one form.
            </p>
            <Link
              href="/content-creators/apply"
              className="inline-flex items-center rounded-full bg-sky-300 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
            >
              Start the application
              <ArrowRight className="ml-2 size-4" aria-hidden="true" />
            </Link>
          </div>
        </div>
      </section>
    </main>
  );
}
