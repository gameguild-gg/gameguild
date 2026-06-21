import { Link } from '@/i18n/navigation';
import { getPublicProject, publicProjects } from '@/lib/community/public-community';
import { ArrowRight, CheckCircle2, ClipboardList, FlaskConical, UserRound } from 'lucide-react';
import Image from 'next/image';
import { notFound } from 'next/navigation';
import React from 'react';

export async function generateStaticParams() {
  return publicProjects.map((project) => ({ project: project.slug }));
}

export async function generateMetadata({ params }: { params: Promise<{ project: string }> }) {
  const { project: slug } = await params;
  const project = getPublicProject(slug);

  return {
    title: project ? `${project.title} | GameGuild Projects` : 'Project Not Found | GameGuild',
    description: project?.summary,
  };
}

export default async function Page({ params }: { readonly params: Promise<{ project: string }> }): Promise<React.JSX.Element> {
  const { project: slug } = await params;
  const project = getPublicProject(slug);

  if (!project) notFound();

  return (
    <main className="bg-slate-950 text-white">
      <section className="relative border-b border-white/10">
        <div className="absolute inset-0">
          <Image src={project.previewImage} alt={`${project.title} project preview`} fill priority className="object-cover" sizes="100vw" />
          <div className={`absolute inset-0 bg-gradient-to-br ${project.accent}`} />
          <div className="absolute inset-0 bg-gradient-to-r from-slate-950 via-slate-950/88 to-slate-950/30" />
          <div className="absolute inset-0 bg-gradient-to-t from-slate-950 via-transparent to-slate-950/20" />
        </div>
        <div className="relative mx-auto grid w-full max-w-7xl gap-10 px-4 py-20 sm:px-6 lg:grid-cols-[1fr_0.8fr] lg:px-8">
          <div className="max-w-3xl space-y-6">
            <p className="text-sm font-semibold uppercase tracking-[0.18em] text-sky-100">{project.status}</p>
            <h1 className="text-5xl font-semibold tracking-tight sm:text-6xl">{project.title}</h1>
            <p className="text-lg leading-8 text-slate-200">{project.description}</p>
            <div className="flex flex-wrap gap-3">
              <Link
                href="/testing-lab"
                className="inline-flex items-center rounded-full bg-white px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-100"
              >
                Join this playtest
                <FlaskConical className="ml-2 size-4" aria-hidden="true" />
              </Link>
              <Link
                href="/projects"
                className="inline-flex items-center rounded-full border border-white/20 px-5 py-3 text-sm font-semibold text-white transition hover:bg-white/10"
              >
                Back to projects
                <ArrowRight className="ml-2 size-4" aria-hidden="true" />
              </Link>
            </div>
          </div>

          <aside className="rounded-3xl border border-white/15 bg-slate-950/55 p-6 backdrop-blur">
            <div className="mb-6 flex items-center gap-3">
              <UserRound className="size-5 text-sky-200" aria-hidden="true" />
              <div>
                <p className="text-sm text-slate-400">Creator</p>
                <p className="font-semibold text-white">{project.creator}</p>
              </div>
            </div>
            <div className="grid gap-3">
              {project.metrics.map((metric) => (
                <div key={metric.label} className="rounded-2xl border border-white/10 bg-white/[0.04] p-4">
                  <p className="text-2xl font-semibold">{metric.value}</p>
                  <p className="text-sm text-slate-400">{metric.label}</p>
                </div>
              ))}
            </div>
          </aside>
        </div>
      </section>

      <section className="mx-auto grid w-full max-w-7xl gap-8 px-4 py-14 sm:px-6 lg:grid-cols-[0.8fr_1.2fr] lg:px-8">
        <div className="space-y-5">
          <h2 className="text-3xl font-semibold tracking-tight">Playtest brief</h2>
          <p className="text-base leading-7 text-slate-400">{project.feedbackGoal}</p>
          <div className="flex flex-wrap gap-2">
            {project.tags.map((tag) => (
              <span key={tag} className="rounded-full bg-white/10 px-3 py-1 text-xs font-medium text-slate-200">
                {tag}
              </span>
            ))}
          </div>
        </div>

        <div className="grid gap-4 md:grid-cols-2">
          {project.media.map((item) => (
            <article key={item.label} className="rounded-3xl border border-white/10 bg-slate-900/70 p-6">
              <ClipboardList className="mb-5 size-6 text-sky-200" aria-hidden="true" />
              <h3 className="text-xl font-semibold text-white">{item.label}</h3>
              <p className="mt-3 text-sm leading-6 text-slate-400">{item.detail}</p>
            </article>
          ))}
          <article className="rounded-3xl border border-white/10 bg-slate-900/70 p-6 md:col-span-2">
            <CheckCircle2 className="mb-5 size-6 text-emerald-200" aria-hidden="true" />
            <h3 className="text-xl font-semibold text-white">Course path</h3>
            <p className="mt-3 text-sm leading-6 text-slate-400">
              This project connects back to the {project.coursePath} path, so reviewers can see how learning work becomes
              portfolio proof.
            </p>
          </article>
        </div>
      </section>
    </main>
  );
}
