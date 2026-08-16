import { Link } from '@/i18n/navigation';
import { publicProjects } from '@/lib/community/public-community';
import { ArrowRight, CalendarCheck2, CheckCircle2, ClipboardList, Rocket, Store, Target } from 'lucide-react';
import Image from 'next/image';
import React from 'react';

const launchSteps = [
  {
    title: 'Package the proof',
    description: 'Collect the playable build, trailer, screenshots, platform notes, and project promise in one place.',
    icon: ClipboardList,
  },
  {
    title: 'Review readiness',
    description: 'Run the release checklist with peers before the project moves from showcase to public launch.',
    icon: CheckCircle2,
  },
  {
    title: 'Plan the channels',
    description: 'Prepare the store page, community announcement, tester follow-up, and post-launch metrics.',
    icon: Store,
  },
] as const;

const launchSignals = ['Store page clarity', 'Trailer and screenshots', 'Known issues', 'Support plan', 'Launch metrics'];

export default async function LaunchPadPublicPage(): Promise<React.JSX.Element> {
  const launchProject = publicProjects.find((project) => project.status === 'Showcase ready') ?? publicProjects[0];
  const preparingProjects = publicProjects.filter((project) => project.slug !== launchProject.slug);

  return (
    <main className="bg-slate-950 text-white">
      <section className="border-b border-white/10 bg-[radial-gradient(circle_at_20%_0%,rgba(56,189,248,0.18),transparent_34%),radial-gradient(circle_at_78%_8%,rgba(168,85,247,0.14),transparent_30%),linear-gradient(180deg,#0f172a,#020617)]">
        <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-20 sm:px-6 lg:grid-cols-[0.9fr_1.1fr] lg:px-8">
          <div className="max-w-3xl space-y-6">
            <div className="inline-flex items-center rounded-full border border-white/10 bg-white/[0.04] px-4 py-2 text-sm font-semibold text-sky-200">
              <Rocket className="mr-2 size-4" aria-hidden="true" />
              Launch support for student projects
            </div>
            <h1 className="text-5xl font-semibold tracking-tight sm:text-6xl">Launch Pad</h1>
            <p className="text-lg leading-8 text-slate-300">
              A community launch room for turning course projects and tested builds into public releases with stronger
              positioning, launch assets, and release checklists.
            </p>
            <div className="flex flex-wrap gap-3">
              <Link
                href="/launch-pad/events"
                className="inline-flex items-center rounded-full bg-sky-300 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
              >
                Discover Launch Pad events
                <ArrowRight className="ml-2 size-4" aria-hidden="true" />
              </Link>
              <Link
                href="/testing-lab"
                className="inline-flex items-center rounded-full border border-white/15 px-5 py-3 text-sm font-semibold text-white transition hover:bg-white/10"
              >
                Validate a build first
              </Link>
            </div>
          </div>

          <div className="overflow-hidden rounded-[2rem] border border-white/10 bg-slate-900/70 shadow-2xl shadow-sky-950/30">
            <div className="relative h-72 overflow-hidden">
              <Image
                src={launchProject.previewImage}
                alt={`${launchProject.title} launch candidate preview`}
                fill
                className="object-cover opacity-90"
                sizes="(min-width: 1024px) 50vw, 100vw"
                priority
              />
              <div className={`absolute inset-0 bg-gradient-to-t ${launchProject.accent}`} />
              <div className="absolute inset-0 bg-gradient-to-t from-slate-950 via-slate-950/25 to-transparent" />
              <div className="absolute bottom-0 left-0 right-0 p-6">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-sky-200">Launch candidate</p>
                <h2 className="mt-2 text-3xl font-semibold text-white">{launchProject.title}</h2>
                <p className="mt-2 max-w-xl text-sm leading-6 text-slate-300">{launchProject.summary}</p>
              </div>
            </div>
            <div className="grid gap-3 p-5 sm:grid-cols-3">
              {launchProject.metrics.map((metric) => (
                <div key={metric.label} className="rounded-2xl border border-white/10 bg-white/[0.04] p-4">
                  <p className="text-2xl font-semibold text-white">{metric.value}</p>
                  <p className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-400">{metric.label}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="mx-auto w-full max-w-7xl px-4 py-14 sm:px-6 lg:px-8">
        <div className="mb-8 max-w-2xl">
          <h2 className="text-3xl font-semibold tracking-tight">From project to public release</h2>
          <p className="mt-3 text-sm leading-6 text-slate-400">
            Launch Pad sits after learning and playtesting. The goal is not to publish everything; it is to make the
            right projects easier to understand, test, announce, and support.
          </p>
        </div>
        <div className="grid gap-4 lg:grid-cols-3">
          {launchSteps.map((step) => {
            const Icon = step.icon;

            return (
              <article key={step.title} className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
                <div className="mb-5 flex size-11 items-center justify-center rounded-2xl bg-sky-300/10 text-sky-200">
                  <Icon className="size-5" aria-hidden="true" />
                </div>
                <h3 className="text-xl font-semibold text-white">{step.title}</h3>
                <p className="mt-3 text-sm leading-6 text-slate-400">{step.description}</p>
              </article>
            );
          })}
        </div>
      </section>

      <section className="border-y border-white/10 bg-white/[0.03]">
        <div className="mx-auto grid w-full max-w-7xl gap-8 px-4 py-14 sm:px-6 lg:grid-cols-[0.8fr_1.2fr] lg:px-8">
          <div>
            <h2 className="text-3xl font-semibold tracking-tight">Readiness signals</h2>
            <p className="mt-3 text-sm leading-6 text-slate-400">
              The launch checklist keeps the community review specific, practical, and tied to the actual release
              surface.
            </p>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            {launchSignals.map((signal) => (
              <div key={signal} className="flex items-center gap-3 rounded-2xl border border-white/10 bg-slate-900/70 p-4">
                <CheckCircle2 className="size-5 text-emerald-300" aria-hidden="true" />
                <span className="text-sm font-medium text-slate-200">{signal}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="mx-auto w-full max-w-7xl px-4 py-14 sm:px-6 lg:px-8">
        <div className="mb-8 flex flex-col justify-between gap-4 lg:flex-row lg:items-end">
          <div>
            <h2 className="text-3xl font-semibold tracking-tight">Projects preparing for launch</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">
              Candidates usually move from course work to Testing Lab, then into launch planning when the project has
              enough proof for a public audience.
            </p>
          </div>
          <Link href="/community" className="inline-flex items-center text-sm font-semibold text-sky-200">
            Join the launch desk
            <ArrowRight className="ml-2 size-4" aria-hidden="true" />
          </Link>
        </div>
        <div className="grid gap-5 lg:grid-cols-2">
          {preparingProjects.map((project) => (
            <Link
              key={project.slug}
              href={`/projects/${project.slug}`}
              className="group grid overflow-hidden rounded-3xl border border-white/10 bg-slate-900/70 transition hover:-translate-y-1 hover:border-white/20 sm:grid-cols-[220px_1fr]"
            >
              <div className="relative min-h-56 overflow-hidden sm:min-h-full">
                <Image
                  src={project.previewImage}
                  alt={`${project.title} launch preparation preview`}
                  fill
                  className="object-cover opacity-90 transition duration-500 group-hover:scale-105"
                  sizes="(min-width: 1024px) 220px, 100vw"
                />
                <div className="absolute inset-0 bg-gradient-to-t from-slate-950/80 via-slate-950/20 to-transparent" />
              </div>
              <div className="space-y-4 p-6">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-sky-200">{project.status}</p>
                <h3 className="text-2xl font-semibold text-white">{project.title}</h3>
                <p className="text-sm leading-6 text-slate-400">{project.feedbackGoal}</p>
                <div className="flex items-center gap-3 text-sm text-slate-300">
                  <Target className="size-4 text-sky-200" aria-hidden="true" />
                  {project.coursePath}
                </div>
              </div>
            </Link>
          ))}
        </div>
      </section>

      <section className="border-t border-white/10 bg-slate-900/60">
        <div className="mx-auto flex w-full max-w-7xl flex-col gap-5 px-4 py-12 sm:px-6 lg:flex-row lg:items-center lg:justify-between lg:px-8">
          <div>
            <h2 className="text-3xl font-semibold tracking-tight">Ready to prepare a release?</h2>
            <p className="mt-2 text-sm text-slate-400">Apply with an accessible Team Project or register individually for an event.</p>
          </div>
          <Link
            href="/launch-pad/participation"
            className="inline-flex items-center rounded-full bg-sky-300 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
          >
            Your participation
            <CalendarCheck2 className="ml-2 size-4" aria-hidden="true" />
          </Link>
        </div>
      </section>
    </main>
  );
}
