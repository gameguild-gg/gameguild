import { CourseHighlightCarousel } from '@/components/courses/course-highlight-carousel';
import { PublicCourseCatalog } from '@/components/courses/public-course-catalog';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import { publicPlaytests } from '@/lib/community/public-community';
import { getPublicCourseCatalog } from '@/lib/courses/services/course.service';
import { ArrowRight, FlaskConical, Layers3 } from 'lucide-react';

export default async function CoursesPage() {
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
                <Link href="/courses">
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
              <Link href="/projects">
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
          </div>
        </div>
      </section>
    </main>
  );
}
