'use client';

import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import type { CourseProjectShowcase } from '@/lib/courses/public-programs';
import { ArrowLeft, ArrowRight, CheckCircle2, Layers3 } from 'lucide-react';
import Image from 'next/image';
import { useMemo, useState } from 'react';

interface CourseProjectCarouselProps {
  readonly courseTitle: string;
  readonly projects: CourseProjectShowcase[];
}

export function CourseProjectCarousel({ courseTitle, projects }: CourseProjectCarouselProps) {
  const [selectedIndex, setSelectedIndex] = useState(0);
  const selectedProject = projects[selectedIndex];
  const projectCount = projects.length;
  const translateX = useMemo(() => `translateX(-${selectedIndex * 100}%)`, [selectedIndex]);

  if (!selectedProject || projectCount === 0) {
    return null;
  }

  const showPreviousProject = () => {
    setSelectedIndex((current) => (current === 0 ? projectCount - 1 : current - 1));
  };

  const showNextProject = () => {
    setSelectedIndex((current) => (current + 1) % projectCount);
  };

  return (
    <section aria-label="Project gallery" className="overflow-hidden rounded-[2rem] border border-white/10 bg-[#070b14] shadow-2xl shadow-black/30">
      <div className="grid gap-0 lg:grid-cols-[minmax(0,1fr)_360px]">
        <div className="relative overflow-hidden">
          <div className="flex transition-transform duration-500 ease-out motion-reduce:transition-none" style={{ transform: translateX }}>
            {projects.map((project, index) => (
              <article key={project.title} className="min-w-full">
                <div className="grid min-h-[560px] lg:grid-cols-[0.9fr_1.1fr]">
                  <div className="relative min-h-[280px] overflow-hidden lg:min-h-full">
                    <Image src={project.image} alt={`${project.title} project preview`} fill className="object-cover" sizes="(min-width: 1024px) 42vw, 100vw" />
                    <div className="absolute inset-0 bg-gradient-to-t from-[#05070d]/82 via-[#05070d]/8 to-transparent" />
                    <div className="absolute left-5 top-5 rounded-full border border-white/15 bg-black/35 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-white backdrop-blur">
                      {project.moduleLabel}
                    </div>
                  </div>

                  <div className="flex flex-col justify-center p-7 md:p-10">
                    <p className="text-xs font-semibold uppercase tracking-[0.24em] text-sky-200/80">Portfolio project {String(index + 1).padStart(2, '0')}</p>
                    <h3 className="mt-5 text-3xl font-semibold leading-tight tracking-tight text-white md:text-5xl">{project.title}</h3>
                    <p className="mt-5 max-w-xl text-base leading-8 text-slate-300">{project.summary}</p>

                    <div className="mt-8 rounded-[1.25rem] border border-white/10 bg-white/[0.04] p-5">
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Deliverable</p>
                      <p className="mt-3 text-sm leading-7 text-slate-300">{project.deliverable}</p>
                    </div>

                    <div className="mt-6 flex flex-wrap gap-2">
                      {project.skills.map((skill) => (
                        <span
                          key={skill}
                          className="inline-flex items-center gap-2 rounded-full border border-sky-300/15 bg-sky-400/10 px-3 py-1.5 text-sm text-sky-100"
                        >
                          <CheckCircle2 className="size-3.5" />
                          {skill}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>
              </article>
            ))}
          </div>

          <div className="absolute bottom-5 right-5 flex gap-2">
            <Button
              type="button"
              size="icon"
              variant="outline"
              onClick={showPreviousProject}
              className="rounded-full border-white/15 bg-black/35 text-white backdrop-blur hover:bg-white/10 hover:text-white"
              aria-label="Show previous project"
            >
              <ArrowLeft className="size-4" />
            </Button>
            <Button
              type="button"
              size="icon"
              variant="outline"
              onClick={showNextProject}
              className="rounded-full border-white/15 bg-black/35 text-white backdrop-blur hover:bg-white/10 hover:text-white"
              aria-label="Show next project"
            >
              <ArrowRight className="size-4" />
            </Button>
          </div>
        </div>

        <aside className="border-t border-white/10 bg-white/[0.035] p-5 lg:border-l lg:border-t-0">
          <div className="flex h-full flex-col">
            <div className="rounded-[1.5rem] border border-white/10 bg-black/20 p-5">
              <div className="flex items-center gap-3">
                <span className="grid size-10 place-items-center rounded-2xl bg-sky-400/10 text-sky-200">
                  <Layers3 className="size-5" />
                </span>
                <div>
                  <p className="text-sm font-semibold text-white">{courseTitle} builds</p>
                  <p className="text-xs text-slate-500">{projectCount} project checkpoints</p>
                </div>
              </div>
              <p className="mt-5 text-sm leading-7 text-slate-400">
                Move through each project as a proof point: first make the system visible, then make the behavior explainable, then package the work for review.
              </p>
            </div>

            <div className="mt-5 grid gap-3">
              {projects.map((project, index) => (
                <button
                  key={project.title}
                  type="button"
                  onClick={() => setSelectedIndex(index)}
                  aria-current={selectedIndex === index ? 'true' : undefined}
                  className={cn(
                    'group rounded-[1.25rem] border p-3 text-left transition',
                    selectedIndex === index
                      ? 'border-sky-300/35 bg-sky-300/10 text-white'
                      : 'border-white/10 bg-black/15 text-slate-400 hover:border-white/20 hover:bg-white/[0.04] hover:text-white',
                  )}
                >
                  <span className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">{project.moduleLabel}</span>
                  <span className="mt-1 block text-sm font-semibold">{project.title}</span>
                </button>
              ))}
            </div>
          </div>
        </aside>
      </div>
    </section>
  );
}
