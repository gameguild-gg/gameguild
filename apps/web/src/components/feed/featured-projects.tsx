import { getPublishedProjects } from '@/lib/projects/public-projects';
import { ArrowRight } from 'lucide-react';
import Link from 'next/link';
import React from 'react';

export async function FeaturedProjects(): Promise<React.JSX.Element> {
  const projects = (await getPublishedProjects()).slice(0, 5);

  return (
    <div className="rounded-3xl border border-white/10 bg-slate-900/70 p-6">
      <h2 className="text-xl font-semibold">Featured projects</h2>
      <div className="mt-5 space-y-3">
        {projects.length === 0 ? (
          <p className="text-sm text-slate-400">Published projects will appear here.</p>
        ) : (
          projects.map((project) => (
            <Link
              key={project.slug}
              href={`/projects/${project.slug}`}
              className="group flex items-center justify-between gap-4 rounded-2xl border border-white/10 bg-white/[0.03] p-4 transition hover:border-white/20"
            >
              <span className="text-sm font-semibold text-white">{project.title}</span>
              <ArrowRight className="size-4 text-sky-200 transition group-hover:translate-x-1" aria-hidden="true" />
            </Link>
          ))
        )}
      </div>
    </div>
  );
}
