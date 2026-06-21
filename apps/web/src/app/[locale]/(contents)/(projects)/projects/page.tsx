import { Link } from '@/i18n/navigation';
import { publicProjects } from '@/lib/community/public-community';
import { ArrowRight, FlaskConical, Search, Tags } from 'lucide-react';
import Image from 'next/image';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <main className="bg-slate-950 text-white">
      <section className="border-b border-white/10 bg-[radial-gradient(circle_at_20%_0%,rgba(56,189,248,0.16),transparent_34%),linear-gradient(180deg,#0f172a,#020617)]">
        <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-20 sm:px-6 lg:grid-cols-[0.9fr_1.1fr] lg:px-8">
          <div className="max-w-3xl space-y-6">
            <h1 className="text-5xl font-semibold tracking-tight sm:text-6xl">Project showcase</h1>
            <p className="text-lg leading-8 text-slate-300">
              Explore student and member projects moving through learning, critique, playtesting, and launch preparation.
              Each project shows what is being tested, who is building it, and where the community can help.
            </p>
            <div className="flex flex-wrap gap-3">
              <Link
                href="/testing-lab"
                className="inline-flex items-center rounded-full bg-sky-300 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
              >
                Submit to Testing Lab
                <FlaskConical className="ml-2 size-4" aria-hidden="true" />
              </Link>
              <Link
                href="/community"
                className="inline-flex items-center rounded-full border border-white/15 px-5 py-3 text-sm font-semibold text-white transition hover:bg-white/10"
              >
                Meet the community
                <ArrowRight className="ml-2 size-4" aria-hidden="true" />
              </Link>
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-3">
            <div className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
              <p className="text-3xl font-semibold">{publicProjects.length}</p>
              <p className="mt-1 text-sm text-slate-400">Showcase projects</p>
            </div>
            <div className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
              <p className="text-3xl font-semibold">3</p>
              <p className="mt-1 text-sm text-slate-400">Review states</p>
            </div>
            <div className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
              <p className="text-3xl font-semibold">25+</p>
              <p className="mt-1 text-sm text-slate-400">Feedback signals</p>
            </div>
          </div>
        </div>
      </section>

      <section className="mx-auto w-full max-w-7xl px-4 py-14 sm:px-6 lg:px-8">
        <div className="mb-8 flex flex-col justify-between gap-4 lg:flex-row lg:items-end">
          <div>
            <h2 className="text-3xl font-semibold tracking-tight">Featured projects</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">
              Browse work by status, course path, and feedback need. Project detail pages give testers enough context to
              provide useful feedback.
            </p>
          </div>
          <div className="flex flex-wrap gap-2 text-sm text-slate-300">
            <span className="inline-flex items-center rounded-full border border-white/10 px-3 py-2">
              <Search className="mr-2 size-4" aria-hidden="true" />
              Playtest-ready
            </span>
            <span className="inline-flex items-center rounded-full border border-white/10 px-3 py-2">
              <Tags className="mr-2 size-4" aria-hidden="true" />
              Portfolio proof
            </span>
          </div>
        </div>

        <div className="grid gap-5 lg:grid-cols-3">
          {publicProjects.map((project) => (
            <article key={project.slug} className="overflow-hidden rounded-3xl border border-white/10 bg-slate-900/70">
              <Link href={`/projects/${project.slug}`} className="group block">
                <div className="relative h-48 overflow-hidden">
                  <Image
                    src={project.previewImage}
                    alt={`${project.title} project preview`}
                    fill
                    className="object-cover opacity-90 transition duration-500 group-hover:scale-105"
                    sizes="(min-width: 1024px) 33vw, 100vw"
                  />
                  <div className={`absolute inset-0 bg-gradient-to-t ${project.accent}`} />
                  <div className="absolute inset-0 bg-gradient-to-t from-slate-950/80 via-slate-950/10 to-transparent" />
                </div>
              </Link>
              <div className="space-y-5 p-6">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-sky-200">{project.status}</p>
                  <h3 className="mt-2 text-2xl font-semibold text-white">{project.title}</h3>
                  <p className="mt-1 text-sm text-slate-400">
                    {project.creator} - {project.creatorRole}
                  </p>
                </div>
                <p className="text-sm leading-6 text-slate-300">{project.summary}</p>
                <div className="flex flex-wrap gap-2">
                  {project.tags.map((tag) => (
                    <span key={tag} className="rounded-full bg-white/10 px-3 py-1 text-xs font-medium text-slate-200">
                      {tag}
                    </span>
                  ))}
                </div>
                <Link
                  href={`/projects/${project.slug}`}
                  className="inline-flex items-center text-sm font-semibold text-sky-200"
                >
                  View project
                  <ArrowRight className="ml-2 size-4" aria-hidden="true" />
                </Link>
              </div>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}
