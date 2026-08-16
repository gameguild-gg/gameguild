import { CourseHighlightCarousel } from '@/components/courses/course-highlight-carousel';
import { PublicCourseCatalog } from '@/components/courses/public-course-catalog';
import { ProgramsCatalogView } from '@/components/courses/programs-catalog-view';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import { publicPlaytests, publicProjects } from '@/lib/community/public-community';
import { getPublicCourseCatalog } from '@/lib/courses/services/course.service';
import { PUBLIC_PROGRAM_PACKAGES } from '@/lib/courses/public-programs';
import { ArrowRight, FlaskConical, Layers3 } from 'lucide-react';
import Image from 'next/image';

type CoursesPageProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

/** Unified public catalog: courses by default, program packages via ?type=program. */
export default async function CoursesPage({ searchParams }: CoursesPageProps) {
  const query = (await searchParams) ?? {};
  if (query?.type === 'program') return <ProgramsCatalogView />;
  const catalog = await getPublicCourseCatalog();
  const courses = catalog.data;
  const courseCount = courses.length;
  const openEnrollmentCount = courses.filter((course) => course.isEnrollmentOpen).length;
  const totalEstimatedHours = courses.reduce((total, course) => total + (course.estimatedHours ?? 0), 0);

  return (
    <main className="min-h-screen overflow-hidden bg-[#070a12] text-white">
      <section className="relative border-b border-white/10">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_18%_12%,rgba(59,130,246,0.2),transparent_30%),radial-gradient(circle_at_82%_18%,rgba(124,58,237,0.18),transparent_32%),linear-gradient(180deg,rgba(15,23,42,0.2),rgba(7,10,18,1))]" />
        <div className="absolute inset-x-0 bottom-0 h-40 bg-gradient-to-t from-[#070a12] to-transparent" />

        <div className="container relative mx-auto grid min-h-[760px] gap-12 px-4 py-20 lg:grid-cols-[0.95fr_1.05fr] lg:items-center">
          <div className="flex max-w-3xl flex-col gap-8">
            <div className="flex flex-col gap-5">
              <h1 className="text-5xl font-semibold leading-[0.98] tracking-tight md:text-7xl">
                Build the game development portfolio you want to be known for.
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-slate-300 md:text-xl">
                GameGuild courses are organized into focused production packages: programming foundations, AI systems, launch operations, portfolio presentation, and data-informed game decisions.
              </p>
            </div>

            <div className="flex flex-wrap gap-3">
              <Button asChild size="lg" className="bg-white text-slate-950 hover:bg-slate-200">
                <Link href="#catalog">
                  Explore courses
                  <ArrowRight />
                </Link>
              </Button>
              <Button asChild size="lg" variant="outline" className="border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
                <Link href="/courses?type=program">
                  View programs
                  <Layers3 />
                </Link>
              </Button>
            </div>

            <div className="grid max-w-2xl grid-cols-3 gap-3">
              <div className="rounded-3xl border border-white/10 bg-white/[0.045] p-5 backdrop-blur">
                <p className="text-3xl font-semibold">{courseCount}</p>
                <p className="mt-1 text-sm text-slate-400">Courses</p>
              </div>
              <div className="rounded-3xl border border-white/10 bg-white/[0.045] p-5 backdrop-blur">
                <p className="text-3xl font-semibold">{openEnrollmentCount}</p>
                <p className="mt-1 text-sm text-slate-400">Open seats</p>
              </div>
              <div className="rounded-3xl border border-white/10 bg-white/[0.045] p-5 backdrop-blur">
                <p className="text-3xl font-semibold">{totalEstimatedHours}h</p>
                <p className="mt-1 text-sm text-slate-400">Study time</p>
              </div>
            </div>

          </div>

          <CourseHighlightCarousel courses={courses} />
        </div>
      </section>

      <section className="bg-[#070a12] py-16">
        <div className="container mx-auto flex flex-col gap-8 px-4">
          <div className="flex flex-col justify-between gap-6 md:flex-row md:items-end">
            <div className="max-w-2xl">
              <h2 className="text-4xl font-semibold tracking-tight">Course packages</h2>
              <p className="mt-4 text-base leading-7 text-slate-400">
                Programs are built as composable packages, so students understand what to take first, what they will produce, and how the work connects to a portfolio.
              </p>
            </div>
            <Button asChild variant="outline" className="w-fit border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
              <Link href="/courses?type=program">
                Browse all programs
                <ArrowRight />
              </Link>
            </Button>
          </div>

          <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-5">
            {PUBLIC_PROGRAM_PACKAGES.map((program) => (
              <Link
                key={program.slug}
                href={`/programs/${program.slug}`}
                className="group overflow-hidden rounded-3xl border border-white/10 bg-white/[0.045] transition hover:-translate-y-1 hover:border-white/20"
              >
                <div className="relative aspect-[4/3] overflow-hidden">
                  <Image src={program.image} alt={program.title} fill loading="eager" className="object-cover transition duration-500 group-hover:scale-105" sizes="(min-width: 1280px) 20vw, (min-width: 768px) 50vw, 100vw" />
                  <div className="absolute inset-0 bg-gradient-to-t from-[#070a12] via-[#070a12]/40 to-transparent" />
                </div>
                <div className="flex flex-col gap-3 p-5">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">{program.duration}</p>
                  <h3 className="text-xl font-semibold tracking-tight">{program.shortTitle}</h3>
                  <p className="line-clamp-3 text-sm leading-6 text-slate-400">{program.summary}</p>
                </div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      <section id="catalog" className="scroll-mt-16 bg-[#070a12]">
        <PublicCourseCatalog initialCourses={courses} />
      </section>

      <section className="bg-[#070a12] py-16">
        <div className="container mx-auto grid gap-8 px-4 lg:grid-cols-[0.8fr_1.2fr]">
          <div className="max-w-xl">
            <h2 className="text-4xl font-semibold tracking-tight">From course work to public proof</h2>
            <p className="mt-4 text-base leading-7 text-slate-400">
              Courses connect into community outcomes: projects enter the showcase, testing sessions produce feedback,
              and launch-ready work becomes portfolio evidence.
            </p>
            <Button asChild variant="outline" className="mt-6 w-fit border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
              <Link href="/showcase">
                View student projects
                <ArrowRight />
              </Link>
            </Button>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="rounded-3xl border border-white/10 bg-white/[0.045] p-6">
              <FlaskConical className="mb-5 text-sky-200" />
              <h3 className="text-xl font-semibold">Testing Lab handoff</h3>
              <p className="mt-3 text-sm leading-6 text-slate-400">
                {publicPlaytests[0]?.title ?? 'Playtest sessions'} gives students a structured next step after lessons,
                assignments, and prototype milestones.
              </p>
            </div>
            <div className="rounded-3xl border border-white/10 bg-white/[0.045] p-6">
              <h3 className="text-xl font-semibold">Featured project outcomes</h3>
              <div className="mt-5 space-y-3">
                {publicProjects.slice(0, 3).map((project) => (
                  <Link key={project.slug} href={`/projects/${project.slug}`} className="group flex items-center justify-between gap-4 rounded-2xl border border-white/10 bg-black/20 px-4 py-3 text-sm text-slate-300 transition hover:border-white/20 hover:text-white">
                    <span>{project.title}</span>
                    <ArrowRight className="opacity-60 transition group-hover:translate-x-1 group-hover:opacity-100" />
                  </Link>
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>
    </main>
  );
}
