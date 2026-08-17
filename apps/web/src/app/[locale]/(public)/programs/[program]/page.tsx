import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import { getPublicCourseCatalog } from '@/lib/courses/services/course.service';
import { PUBLIC_PROGRAM_PACKAGES, getCoursesForProgram, getPublicProgramPackage } from '@/lib/courses/public-programs';
import { ArrowLeft, ArrowRight, BookOpen, CheckCircle2, Clock, GraduationCap, Layers3, Target } from 'lucide-react';
import Image from 'next/image';
import { notFound } from 'next/navigation';

interface ProgramDetailPageProps {
  readonly params: Promise<{ program: string }>;
}

function getCourseString(course: Record<string, unknown>, key: string): string | null {
  const value = course[key];
  return typeof value === 'string' && value.trim().length > 0 ? value : null;
}

export function generateStaticParams() {
  return PUBLIC_PROGRAM_PACKAGES.map((program) => ({ program: program.slug }));
}

export async function generateMetadata({ params }: ProgramDetailPageProps) {
  const { program: slug } = await params;
  const program = getPublicProgramPackage(slug);

  if (!program) {
    return {
      title: 'Program Not Found | GameGuild',
    };
  }

  return {
    title: `${program.title} | GameGuild Programs`,
    description: program.summary,
  };
}

export default async function ProgramDetailPage({ params }: ProgramDetailPageProps) {
  const { program: slug } = await params;
  const program = getPublicProgramPackage(slug);

  if (!program) {
    notFound();
  }

  const catalog = await getPublicCourseCatalog();
  const courses = getCoursesForProgram(program, catalog.data);

  return (
    <main className="min-h-screen overflow-hidden bg-[#070a12] text-white">
      <section className="relative">
        <div className="absolute inset-0">
          <Image src={program.image} alt={program.title} fill className="object-cover" sizes="100vw" priority />
          <div className="absolute inset-0 bg-gradient-to-r from-[#070a12] via-[#070a12]/88 to-[#070a12]/30" />
          <div className="absolute inset-0 bg-gradient-to-t from-[#070a12] via-transparent to-[#070a12]/20" />
        </div>

        <div className="container relative mx-auto min-h-[720px] px-4 py-10">
          <Button asChild variant="ghost" className="mb-16 text-slate-300 hover:bg-white/10 hover:text-white">
            <Link href="/programs">
              <ArrowLeft />
              Back to programs
            </Link>
          </Button>

          <div className="grid gap-12 lg:grid-cols-[1fr_420px] lg:items-end">
            <div className="flex max-w-4xl flex-col gap-8">
              <div className="flex flex-wrap gap-2">
                <Badge variant="outline" className="border-white/20 bg-black/35 text-white backdrop-blur">
                  {program.level}
                </Badge>
                <Badge variant="outline" className="border-white/20 bg-black/35 text-white backdrop-blur">
                  {program.duration}
                </Badge>
                <Badge variant="outline" className="border-white/20 bg-black/35 text-white backdrop-blur">
                  {courses.length} courses
                </Badge>
              </div>

              <div className="flex flex-col gap-6">
                <h1 className="text-5xl font-semibold leading-[0.98] tracking-tight md:text-7xl">{program.title}</h1>
                <p className="max-w-3xl text-lg leading-8 text-slate-300 md:text-xl">{program.longDescription}</p>
              </div>

              <div className="flex flex-wrap gap-3">
                <Button asChild size="lg" className="bg-white text-slate-950 hover:bg-slate-200">
                  <Link href="#courses">
                    See courses
                    <ArrowRight />
                  </Link>
                </Button>
                <Button asChild size="lg" variant="outline" className="border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
                  <Link href="/courses">
                    Full catalog
                    <BookOpen />
                  </Link>
                </Button>
              </div>
            </div>

            <aside className="rounded-[2rem] border border-white/10 bg-white/[0.055] p-6 shadow-2xl shadow-black/30 backdrop-blur">
              <h2 className="text-xl font-semibold">Best for</h2>
              <p className="mt-3 text-sm leading-6 text-slate-300">{program.audience}</p>
              <div className="mt-6 grid gap-3">
                <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
                  <Clock className="mb-3 text-sky-200" />
                  <p className="font-semibold">{program.duration}</p>
                  <p className="mt-1 text-xs text-slate-500">Estimated total path</p>
                </div>
                <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
                  <Target className="mb-3 text-emerald-200" />
                  <p className="font-semibold">Portfolio result</p>
                  <p className="mt-1 text-xs leading-5 text-slate-400">{program.portfolioResult}</p>
                </div>
              </div>
            </aside>
          </div>
        </div>
      </section>

      <section className="container mx-auto grid gap-6 px-4 py-14 md:grid-cols-3">
        <div className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-6">
          <Layers3 className="mb-5 text-sky-200" />
          <h2 className="text-xl font-semibold">Learning format</h2>
          <p className="mt-3 text-sm leading-6 text-slate-400">{program.format}</p>
        </div>
        <div className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-6">
          <GraduationCap className="mb-5 text-violet-200" />
          <h2 className="text-xl font-semibold">Level</h2>
          <p className="mt-3 text-sm leading-6 text-slate-400">{program.level}</p>
        </div>
        <div className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-6">
          <BookOpen className="mb-5 text-amber-200" />
          <h2 className="text-xl font-semibold">Included courses</h2>
          <p className="mt-3 text-sm leading-6 text-slate-400">{courses.length} imported courses from the GameGuild course library.</p>
        </div>
      </section>

      <section className="container mx-auto grid gap-10 px-4 py-14 lg:grid-cols-[0.72fr_1.28fr]">
        <div>
          <h2 className="text-4xl font-semibold tracking-tight">What students should be able to show</h2>
          <p className="mt-4 text-base leading-7 text-slate-400">
            The program is framed around proof. Students should leave with artifacts that communicate what they built, how they made decisions, and where they can go next.
          </p>
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          {program.outcomes.map((outcome) => (
            <div key={outcome} className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-5">
              <CheckCircle2 className="mb-4 text-emerald-200" />
              <p className="text-sm leading-6 text-slate-300">{outcome}</p>
            </div>
          ))}
        </div>
      </section>

      <section id="courses" className="container mx-auto flex flex-col gap-8 px-4 py-14">
        <div className="flex flex-col justify-between gap-5 md:flex-row md:items-end">
          <div>
            <h2 className="text-4xl font-semibold tracking-tight">Courses in this package</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">Courses remain independent and can be taken individually, but the package shows how they fit into a larger production path.</p>
          </div>
          <Button asChild variant="outline" className="w-fit border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
            <Link href={`/courses?program=${program.slug}`}>
              Filter catalog
              <ArrowRight />
            </Link>
          </Button>
        </div>

        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {courses.map((course) => {
            const courseRecord = course as Record<string, unknown>;
            const courseThumbnail = getCourseString(courseRecord, 'thumbnail');
            const courseTitle = getCourseString(courseRecord, 'title') ?? 'Course';
            const courseSlug = getCourseString(courseRecord, 'slug');
            const courseDescription = getCourseString(courseRecord, 'description') ?? 'Open the course landing page to review outcomes, curriculum, and enrollment details.';

            return (
              <Link key={courseSlug ?? String(course.id ?? courseTitle)} href={courseSlug ? `/courses/${courseSlug}` : '/courses'} className="group overflow-hidden rounded-[2rem] border border-white/10 bg-white/[0.045] transition hover:-translate-y-1 hover:border-white/20">
                <div className="relative aspect-video overflow-hidden">
                  {courseThumbnail ? (
                    <Image src={courseThumbnail} alt={courseTitle} fill unoptimized={courseThumbnail.endsWith('.svg')} className="object-cover transition duration-500 group-hover:scale-105" sizes="(min-width: 1280px) 33vw, (min-width: 768px) 50vw, 100vw" />
                  ) : (
                    <div className="h-full bg-[radial-gradient(circle_at_24%_18%,rgba(56,189,248,0.26),transparent_32%),linear-gradient(135deg,#020617,#111827_52%,#1e1b4b)]" />
                  )}
                  <div className="absolute inset-0 bg-gradient-to-t from-[#070a12] via-[#070a12]/30 to-transparent" />
                </div>
                <div className="flex flex-col gap-4 p-5">
                  <h3 className="text-2xl font-semibold tracking-tight">{courseTitle}</h3>
                  <p className="line-clamp-3 text-sm leading-6 text-slate-400">{courseDescription}</p>
                  <div className="flex items-center justify-between text-sm text-slate-500">
                    <span>{course.estimatedHours ?? 0}h</span>
                    <span className="inline-flex items-center gap-2 text-slate-300">
                      View course
                      <ArrowRight className="transition group-hover:translate-x-1" />
                    </span>
                  </div>
                </div>
              </Link>
            );
          })}
        </div>
      </section>
    </main>
  );
}
