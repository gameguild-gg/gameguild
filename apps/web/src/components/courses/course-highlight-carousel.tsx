'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import type { Program } from '@/lib/api/generated';
import { getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import { getCourseShowcase, getProgramForCourse } from '@/lib/courses/public-programs';
import { ArrowRight, ChevronLeft, ChevronRight } from 'lucide-react';
import Image from 'next/image';
import { useMemo, useState } from 'react';

interface CourseHighlightCarouselProps {
  readonly courses: Program[];
}

function getString(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value : null;
}

function getNumber(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

export function CourseHighlightCarousel({ courses }: CourseHighlightCarouselProps) {
  const highlights = useMemo(() => courses.slice(0, 5), [courses]);
  const [activeIndex, setActiveIndex] = useState(0);
  const activeCourse = highlights[activeIndex] ?? highlights[0];

  if (!activeCourse) {
    return null;
  }

  const title = getString(activeCourse.title) ?? 'GameGuild course';
  const slug = getString(activeCourse.slug);
  const description = getString(activeCourse.description) ?? 'Explore a GameGuild course landing page.';
  const image = getString(activeCourse.thumbnail) ?? 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop';
  const level = getCourseLevelConfig(activeCourse.difficulty as string | number | null | undefined).name;
  const category = getCourseCategoryName(activeCourse.category as string | number | null | undefined);
  const hours = getNumber(activeCourse.estimatedHours);
  const program = getProgramForCourse(slug);
  const showcase = getCourseShowcase(slug);

  function move(delta: number) {
    setActiveIndex((current) => (current + delta + highlights.length) % highlights.length);
  }

  return (
    <div className="relative">
      <div className="absolute -inset-8 rounded-[3rem] bg-gradient-to-br from-sky-500/10 via-violet-500/10 to-transparent blur-3xl" />
      <section className="relative overflow-hidden rounded-[2rem] border border-white/10 bg-white/[0.055] shadow-2xl shadow-black/40 backdrop-blur">
        <div className="relative min-h-[520px]">
          <Image
            key={image}
            src={image}
            alt={title}
            fill
            unoptimized={image.endsWith('.svg')}
            className="object-cover transition duration-500"
            sizes="(min-width: 1024px) 48vw, 100vw"
            priority
          />
          <div className="absolute inset-0 bg-gradient-to-t from-[#070a12]/82 via-[#070a12]/10 to-transparent" />
          <div className="absolute inset-0 bg-gradient-to-r from-[#070a12]/30 via-transparent to-transparent" />

          <div className="absolute left-5 right-5 top-5 flex items-center justify-between gap-3">
            <div className="flex flex-wrap gap-2">
              <Badge variant="outline" className="border-white/20 bg-black/35 text-white backdrop-blur">
                {program?.shortTitle ?? category}
              </Badge>
              <Badge variant="outline" className="border-white/20 bg-black/35 text-white backdrop-blur">
                {level}
              </Badge>
              {hours ? (
                <Badge variant="outline" className="border-white/20 bg-black/35 text-white backdrop-blur">
                  {hours}h
                </Badge>
              ) : null}
            </div>

            <div className="flex gap-2">
              <Button type="button" size="icon" variant="outline" aria-label="Previous highlight" onClick={() => move(-1)} className="h-9 w-9 border-white/20 bg-black/30 text-white hover:bg-white/10 hover:text-white">
                <ChevronLeft />
              </Button>
              <Button type="button" size="icon" variant="outline" aria-label="Next highlight" onClick={() => move(1)} className="h-9 w-9 border-white/20 bg-black/30 text-white hover:bg-white/10 hover:text-white">
                <ChevronRight />
              </Button>
            </div>
          </div>

          <div className="absolute inset-x-0 bottom-0 flex flex-col gap-6 p-7">
            <div className="max-w-2xl">
              <h2 className="text-4xl font-semibold leading-tight tracking-tight md:text-5xl">{title}</h2>
              <p className="mt-4 line-clamp-3 text-sm leading-6 text-slate-300">
                {showcase?.headline ?? description}
              </p>
            </div>

            <div className="grid gap-3 sm:grid-cols-[1fr_auto] sm:items-end">
              <div className="flex gap-2">
                {highlights.map((course, index) => {
                  const courseTitle = getString(course.title) ?? `Course ${index + 1}`;

                  return (
                    <button
                      key={getString(course.slug) ?? String(course.id ?? index)}
                      type="button"
                      aria-label={`Show ${courseTitle}`}
                      aria-current={index === activeIndex}
                      onClick={() => setActiveIndex(index)}
                      className={`h-2.5 rounded-full transition-all ${index === activeIndex ? 'w-10 bg-white' : 'w-2.5 bg-white/35 hover:bg-white/60'}`}
                    />
                  );
                })}
              </div>

              <div className="flex flex-wrap gap-3">
                {slug ? (
                  <Button asChild className="bg-white text-slate-950 hover:bg-slate-200">
                    <Link href={`/courses/${slug}`}>
                      Open course
                      <ArrowRight />
                    </Link>
                  </Button>
                ) : null}
                {program ? (
                  <Button asChild variant="outline" className="border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
                    <Link href={`/programs/${program.slug}`}>View package</Link>
                  </Button>
                ) : null}
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
