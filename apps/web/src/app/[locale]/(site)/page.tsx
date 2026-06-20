import { publicWebsiteHighlights } from '@/components/site/public-website-shell';
import { Link } from '@/i18n/navigation';
import { ArrowRight, Sparkles } from 'lucide-react';
import React from 'react';

export default async function Page({ params }: PageProps<'/[locale]'>): Promise<React.JSX.Element> {
  await params;

  return (
    <main className="bg-slate-950 text-white">
      <section className="relative overflow-hidden">
        <div className="absolute inset-x-0 top-[-20%] h-96 bg-[radial-gradient(circle_at_center,rgba(56,189,248,0.18),transparent_58%)]" />
        <div className="mx-auto grid w-full max-w-7xl items-center gap-12 px-4 py-20 sm:px-6 sm:py-24 lg:grid-cols-[1.05fr_0.95fr] lg:px-8 lg:py-28">
          <div className="relative z-10 max-w-3xl space-y-8">
            <div className="space-y-5">
              <h1 className="max-w-4xl text-balance text-5xl font-semibold tracking-tight text-white sm:text-6xl lg:text-7xl">
                Learn, Build & Connect
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-slate-300 sm:text-xl">
                Master game development through practical courses, community critique, testing workflows, and launch
                support designed for builders who want to ship.
              </p>
            </div>

            <div className="flex flex-col gap-3 sm:flex-row">
              <Link
                href="/courses"
                className="inline-flex items-center justify-center rounded-full bg-sky-300 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
              >
                Start Learning
                <ArrowRight className="ml-2 size-4" aria-hidden="true" />
              </Link>
              <Link
                href="/programs"
                className="inline-flex items-center justify-center rounded-full border border-white/15 px-5 py-3 text-sm font-semibold text-white transition hover:border-white/30 hover:bg-white/10"
              >
                Explore Programs
              </Link>
            </div>
          </div>

          <div className="relative z-10">
            <div className="rounded-[2rem] border border-white/10 bg-white/[0.04] p-4 shadow-2xl shadow-sky-950/40 backdrop-blur">
              <div className="rounded-[1.5rem] border border-white/10 bg-slate-900/90 p-5">
                <div className="mb-6 flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-slate-400">Learning path</p>
                    <h2 className="mt-1 text-2xl font-semibold text-white">From course to shipped project</h2>
                  </div>
                  <Sparkles className="size-6 text-sky-300" aria-hidden="true" />
                </div>

                <div className="space-y-3">
                  {['Study core systems', 'Build a playable prototype', 'Test with peers', 'Prepare launch assets'].map(
                    (step, index) => (
                      <div
                        key={step}
                        className="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/[0.03] px-4 py-3"
                      >
                        <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-sky-300/15 text-sm font-semibold text-sky-200">
                          {index + 1}
                        </span>
                        <span className="text-sm font-medium text-slate-200">{step}</span>
                      </div>
                    ),
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="border-y border-white/10 bg-white/[0.03]">
        <div className="mx-auto w-full max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
          <div className="max-w-2xl space-y-3">
            <h2 className="text-3xl font-semibold tracking-tight text-white sm:text-4xl">
              Everything You Need to Succeed
            </h2>
            <p className="text-base leading-7 text-slate-400">
              A compact ecosystem for building game skills, validating work, and moving from learning into public launch
              with less friction.
            </p>
          </div>

          <div className="mt-10 grid gap-4 md:grid-cols-3">
            {publicWebsiteHighlights.map((feature) => {
              const Icon = feature.icon;

              return (
                <article key={feature.title} className="rounded-3xl border border-white/10 bg-slate-900/70 p-6">
                  <div className="mb-6 flex size-11 items-center justify-center rounded-2xl bg-sky-300/10 text-sky-200">
                    <Icon className="size-5" aria-hidden="true" />
                  </div>
                  <h3 className="text-lg font-semibold text-white">{feature.title}</h3>
                  <p className="mt-3 text-sm leading-6 text-slate-400">{feature.description}</p>
                </article>
              );
            })}
          </div>
        </div>
      </section>
    </main>
  );
}
