import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import { publicProjects } from '@/lib/community/public-community';
import { getPublicCourseCatalog } from '@/lib/courses/services/course.service';
import { PUBLIC_PROGRAM_PACKAGES, getCoursesForProgram } from '@/lib/courses/public-programs';
import { ArrowRight, BookOpen, CheckCircle2, Clock, FlaskConical, GraduationCap, Target } from 'lucide-react';
import Image from 'next/image';

/** Curated program-packages view rendered by /courses?type=program. */
export async function ProgramsCatalogView() {
  const catalog = await getPublicCourseCatalog();
  const courses = catalog.data;

  return (
    <main className="min-h-screen overflow-hidden bg-[#070a12] text-white">
      <section className="relative border-b border-white/10">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_18%_12%,rgba(14,165,233,0.18),transparent_32%),radial-gradient(circle_at_75%_18%,rgba(139,92,246,0.18),transparent_30%),linear-gradient(180deg,#0a0f1c,#070a12)]" />
        <div className="container relative mx-auto grid min-h-[680px] gap-12 px-4 py-20 lg:grid-cols-[0.9fr_1.1fr] lg:items-center">
          <div className="flex max-w-3xl flex-col gap-8">
            <h1 className="text-5xl font-semibold leading-[0.98] tracking-tight md:text-7xl">
              Choose a production path, then build toward a portfolio result.
            </h1>
            <p className="max-w-2xl text-lg leading-8 text-slate-300">
              GameGuild programs group courses into composable learning packages. Each package has a practical audience, a clear outcome, and a project result students can show.
            </p>
            <div className="flex flex-wrap gap-3">
              <Button asChild size="lg" className="bg-white text-slate-950 hover:bg-slate-200">
                <Link href="#programs">
                  Browse programs
                  <ArrowRight />
                </Link>
              </Button>
              <Button asChild size="lg" variant="outline" className="border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
                <Link href="/courses?type=course">
                  View all courses
                  <BookOpen />
                </Link>
              </Button>
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            {PUBLIC_PROGRAM_PACKAGES.slice(0, 4).map((program, index) => (
              <Link
                key={program.slug}
                href={`/programs/${program.slug}`}
                className={`group overflow-hidden rounded-[2rem] border border-white/10 bg-white/[0.045] shadow-2xl shadow-black/20 transition hover:-translate-y-1 hover:border-white/20 ${index === 0 ? 'md:row-span-2' : ''}`}
              >
                <div className={`relative overflow-hidden ${index === 0 ? 'min-h-[430px]' : 'aspect-[4/3]'}`}>
                  <Image src={program.image} alt={program.title} fill className="object-cover opacity-95 brightness-110 saturate-110 transition duration-500 group-hover:scale-105" sizes="(min-width: 1024px) 25vw, 100vw" />
                  <div className="absolute inset-0 bg-gradient-to-t from-[#070a12]/95 via-[#070a12]/20 to-transparent" />
                  <div className="absolute inset-x-0 bottom-0 flex flex-col gap-3 p-5">
                    <Badge variant="outline" className="w-fit border-white/20 bg-black/35 text-white backdrop-blur">
                      {program.duration}
                    </Badge>
                    <h2 className="text-2xl font-semibold tracking-tight">{program.shortTitle}</h2>
                    <p className="line-clamp-3 text-sm leading-6 text-slate-300">{program.summary}</p>
                  </div>
                </div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      <section className="border-y border-white/10 bg-white/[0.03]">
        <div className="container mx-auto grid gap-8 px-4 py-14 lg:grid-cols-[0.85fr_1.15fr]">
          <div className="max-w-xl">
            <h2 className="text-4xl font-semibold tracking-tight">Programs lead into projects, testing, and launch.</h2>
            <p className="mt-4 text-base leading-7 text-slate-400">
              Each package is designed around a visible outcome. Students can move from curriculum into the project
              showcase and Testing Lab without guessing what to build next.
            </p>
            <Button asChild variant="outline" className="mt-6 w-fit border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
              <Link href="/testing-lab">
                Open Testing Lab
                <FlaskConical />
              </Link>
            </Button>
          </div>
          <div className="grid gap-4 md:grid-cols-3">
            {publicProjects.map((project) => (
              <Link key={project.slug} href={`/projects/${project.slug}`} className="group rounded-3xl border border-white/10 bg-slate-950/70 p-5 transition hover:-translate-y-1 hover:border-white/20">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-sky-200">{project.coursePath}</p>
                <h3 className="mt-3 text-xl font-semibold text-white">{project.title}</h3>
                <p className="mt-3 text-sm leading-6 text-slate-400">{project.summary}</p>
                <span className="mt-5 inline-flex items-center text-sm font-semibold text-sky-200">
                  View outcome
                  <ArrowRight className="ml-2 size-4 transition group-hover:translate-x-1" aria-hidden="true" />
                </span>
              </Link>
            ))}
          </div>
        </div>
      </section>

      <section id="programs" className="container mx-auto flex flex-col gap-8 px-4 py-16">
        <div className="flex flex-col justify-between gap-6 lg:flex-row lg:items-end">
          <div className="max-w-2xl">
            <h2 className="text-4xl font-semibold tracking-tight md:text-5xl">Programs and packages</h2>
            <p className="mt-4 text-base leading-7 text-slate-400">
              These are not degree tracks or locked cohorts. They are practical packages that help students understand what to take, why it matters, and what proof they should produce.
            </p>
          </div>
          <div className="grid grid-cols-3 gap-3 text-center">
            <div className="rounded-2xl border border-white/10 bg-white/[0.045] px-5 py-4">
              <p className="text-2xl font-semibold">{PUBLIC_PROGRAM_PACKAGES.length}</p>
              <p className="text-xs text-slate-500">Packages</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-white/[0.045] px-5 py-4">
              <p className="text-2xl font-semibold">{courses.length}</p>
              <p className="text-xs text-slate-500">Courses</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-white/[0.045] px-5 py-4">
              <p className="text-2xl font-semibold">{courses.reduce((total, course) => total + (course.estimatedHours ?? 0), 0)}h</p>
              <p className="text-xs text-slate-500">Study time</p>
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-8">
          {PUBLIC_PROGRAM_PACKAGES.map((program) => {
            const programCourses = getCoursesForProgram(program, courses);

            return (
              <article key={program.slug} className="grid overflow-hidden rounded-[2rem] border border-white/10 bg-white/[0.045] shadow-2xl shadow-black/20 lg:grid-cols-[0.9fr_1.1fr]">
                <div className="relative min-h-[360px] overflow-hidden">
                  <Image src={program.image} alt={program.title} fill className="object-cover brightness-110 saturate-110" sizes="(min-width: 1024px) 42vw, 100vw" />
                  <div className="absolute inset-0 bg-gradient-to-t from-[#070a12]/95 via-[#070a12]/22 to-transparent" />
                  <div className="absolute left-6 right-6 top-6 flex flex-wrap gap-2">
                    <Badge variant="outline" className="border-white/20 bg-black/35 text-white backdrop-blur">
                      {program.level}
                    </Badge>
                    <Badge variant="outline" className="border-white/20 bg-black/35 text-white backdrop-blur">
                      {programCourses.length} courses
                    </Badge>
                  </div>
                  <div className="absolute inset-x-0 bottom-0 p-6">
                    <h3 className="text-4xl font-semibold tracking-tight">{program.title}</h3>
                    <p className="mt-3 max-w-xl text-sm leading-6 text-slate-300">{program.audience}</p>
                  </div>
                </div>

                <div className="flex flex-col gap-7 p-6 md:p-8">
                  <div className="flex flex-col gap-3">
                    <p className="text-lg leading-8 text-slate-300">{program.longDescription}</p>
                    <div className="flex flex-wrap gap-2">
                      {program.tools.map((tool) => (
                        <Badge key={tool} variant="secondary" className="bg-white/10 text-white">
                          {tool}
                        </Badge>
                      ))}
                    </div>
                  </div>

                  <div className="grid gap-4 md:grid-cols-3">
                    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
                      <Clock className="mb-3 text-sky-200" />
                      <p className="font-semibold">{program.duration}</p>
                      <p className="mt-1 text-xs text-slate-500">Estimated path</p>
                    </div>
                    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
                      <GraduationCap className="mb-3 text-violet-200" />
                      <p className="font-semibold">{program.level}</p>
                      <p className="mt-1 text-xs text-slate-500">Level range</p>
                    </div>
                    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
                      <Target className="mb-3 text-emerald-200" />
                      <p className="font-semibold">Portfolio result</p>
                      <p className="mt-1 text-xs text-slate-500">Output-driven</p>
                    </div>
                  </div>

                  <div className="grid gap-4 lg:grid-cols-2">
                    <div>
                      <h4 className="mb-3 text-sm font-semibold uppercase tracking-[0.16em] text-slate-500">Outcomes</h4>
                      <ul className="flex flex-col gap-3">
                        {program.outcomes.map((outcome) => (
                          <li key={outcome} className="flex gap-3 text-sm leading-6 text-slate-300">
                            <CheckCircle2 className="mt-1 shrink-0 text-emerald-200" />
                            <span>{outcome}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                    <div>
                      <h4 className="mb-3 text-sm font-semibold uppercase tracking-[0.16em] text-slate-500">Included courses</h4>
                      <div className="flex flex-col gap-2">
                        {programCourses.map((course) => (
                          <Link key={course.slug ?? course.id} href={`/courses/${course.slug}`} className="group flex items-center justify-between gap-3 rounded-2xl border border-white/10 bg-black/20 px-4 py-3 text-sm text-slate-300 transition hover:border-white/20 hover:text-white">
                            <span>{course.title}</span>
                            <ArrowRight className="opacity-60 transition group-hover:translate-x-1 group-hover:opacity-100" />
                          </Link>
                        ))}
                      </div>
                    </div>
                  </div>

                  <div className="flex flex-wrap items-center justify-between gap-4 rounded-2xl border border-white/10 bg-black/20 p-4">
                    <div>
                      <p className="text-sm font-semibold text-white">Portfolio result</p>
                      <p className="mt-1 text-sm text-slate-400">{program.portfolioResult}</p>
                    </div>
                    <Button asChild className="bg-white text-slate-950 hover:bg-slate-200">
                      <Link href={`/programs/${program.slug}`}>
                        Open program
                        <ArrowRight />
                      </Link>
                    </Button>
                  </div>
                </div>
              </article>
            );
          })}
        </div>
      </section>
    </main>
  );
}
