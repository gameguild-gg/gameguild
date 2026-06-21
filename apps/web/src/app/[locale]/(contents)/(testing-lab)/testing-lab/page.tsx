import { Link } from '@/i18n/navigation';
import { publicPlaytests, publicProjects } from '@/lib/community/public-community';
import { ArrowRight, BarChart3, ClipboardList, FlaskConical, Target } from 'lucide-react';
import Image from 'next/image';
import React from 'react';

const testingSteps = [
  {
    title: 'Submit a build',
    description: 'Share a playable build, video walkthrough, or project page with the context reviewers need.',
    icon: FlaskConical,
  },
  {
    title: 'Define test goals',
    description: 'Pick the questions that matter: onboarding, controls, clarity, difficulty, pacing, or market signal.',
    icon: Target,
  },
  {
    title: 'Collect a feedback report',
    description: 'Turn player notes into a concise feedback report with issues, patterns, and next actions.',
    icon: BarChart3,
  },
] as const;

export default async function TestingLabPage(): Promise<React.JSX.Element> {
  return (
    <main className="bg-slate-950 text-white">
      <section className="border-b border-white/10 bg-[radial-gradient(circle_at_18%_0%,rgba(56,189,248,0.18),transparent_34%),radial-gradient(circle_at_82%_14%,rgba(34,197,94,0.12),transparent_30%),linear-gradient(180deg,#0f172a,#020617)]">
        <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-20 sm:px-6 lg:grid-cols-[0.9fr_1.1fr] lg:px-8">
          <div className="max-w-3xl space-y-6">
            <h1 className="text-5xl font-semibold tracking-tight sm:text-6xl">Testing Lab</h1>
            <p className="text-lg leading-8 text-slate-300">
              A public entry point for students and members to validate game projects with structured playtests,
              reviewer notes, and launch-readiness evidence.
            </p>
            <div className="flex flex-wrap gap-3">
              <Link
                href="/projects"
                className="inline-flex items-center rounded-full bg-sky-300 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
              >
                Browse testable projects
                <ArrowRight className="ml-2 size-4" aria-hidden="true" />
              </Link>
              <Link
                href="/community"
                className="inline-flex items-center rounded-full border border-white/15 px-5 py-3 text-sm font-semibold text-white transition hover:bg-white/10"
              >
                Find reviewers
              </Link>
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            {testingSteps.map((step) => {
              const Icon = step.icon;

              return (
                <article key={step.title} className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
                  <div className="mb-5 flex size-11 items-center justify-center rounded-2xl bg-sky-300/10 text-sky-200">
                    <Icon className="size-5" aria-hidden="true" />
                  </div>
                  <h2 className="text-lg font-semibold text-white">{step.title}</h2>
                  <p className="mt-3 text-sm leading-6 text-slate-400">{step.description}</p>
                </article>
              );
            })}
          </div>
        </div>
      </section>

      <section className="mx-auto grid w-full max-w-7xl gap-8 px-4 py-14 sm:px-6 lg:grid-cols-[0.8fr_1.2fr] lg:px-8">
        <div className="max-w-xl">
          <h2 className="text-3xl font-semibold tracking-tight">Active testing queue</h2>
          <p className="mt-3 text-sm leading-6 text-slate-400">
            Each queue item links to a real project page so visitors can understand the build, creator, test goal, and
            expected output before joining.
          </p>
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          {publicProjects.map((project) => (
            <Link
              key={project.slug}
              href={`/projects/${project.slug}`}
              className="group overflow-hidden rounded-3xl border border-white/10 bg-slate-900/70 transition hover:-translate-y-1 hover:border-white/20"
            >
              <div className="relative h-40 overflow-hidden">
                <Image
                  src={project.previewImage}
                  alt={`${project.title} testing preview`}
                  fill
                  className="object-cover opacity-90 transition duration-500 group-hover:scale-105"
                  sizes="(min-width: 1024px) 33vw, 100vw"
                />
                <div className="absolute inset-0 bg-gradient-to-t from-slate-950 via-slate-950/20 to-transparent" />
              </div>
              <div className="p-5">
                <ClipboardList className="mb-5 size-6 text-sky-200" aria-hidden="true" />
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-sky-200">{project.status}</p>
                <h3 className="mt-3 text-xl font-semibold text-white">{project.title}</h3>
                <p className="mt-3 text-sm leading-6 text-slate-400">{project.feedbackGoal}</p>
                <span className="mt-5 inline-flex items-center text-sm font-semibold text-sky-200">
                  Open project
                  <ArrowRight className="ml-2 size-4 transition group-hover:translate-x-1" aria-hidden="true" />
                </span>
              </div>
            </Link>
          ))}
        </div>
      </section>

      <section className="border-y border-white/10 bg-white/[0.03]">
        <div className="mx-auto w-full max-w-7xl px-4 py-14 sm:px-6 lg:px-8">
          <div className="mb-8 max-w-2xl">
            <h2 className="text-3xl font-semibold tracking-tight">Upcoming playtest sessions</h2>
            <p className="mt-3 text-sm leading-6 text-slate-400">
              Public sessions create clear expectations before a tester signs up and help teams gather comparable
              feedback instead of scattered comments.
            </p>
          </div>
          <div className="grid gap-4 lg:grid-cols-3">
            {publicPlaytests.map((playtest) => (
              <Link
                key={playtest.title}
                href={playtest.href}
                className="rounded-3xl border border-white/10 bg-slate-900/70 p-6 transition hover:border-white/20"
              >
                <p className="text-sm font-semibold text-sky-200">{playtest.format}</p>
                <h3 className="mt-3 text-xl font-semibold text-white">{playtest.title}</h3>
                <p className="mt-3 text-sm text-slate-400">{playtest.date}</p>
                <p className="mt-1 text-sm text-slate-400">{playtest.seats}</p>
              </Link>
            ))}
          </div>
        </div>
      </section>
    </main>
  );
}
