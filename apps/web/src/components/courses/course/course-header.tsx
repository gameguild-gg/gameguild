import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import type { Program } from '@/lib/api/generated';
import type { Product } from '@/lib/courses/actions/enrollment.actions';
import type { CourseViewerAccess } from '@/lib/courses/services/course-viewer-access';
import { getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import { getCourseShowcase, getProgramForCourse } from '@/lib/courses/public-programs';
import { ArrowLeft, ArrowRight, BookOpen, Clock, Layers3, Play, Users } from 'lucide-react';
import Image from 'next/image';
import { CourseCheckoutButton } from './course-checkout-button';
import { shouldUseUnoptimizedCourseImage } from './course-image';
import { CourseSelfEnrollButton } from './course-self-enroll-button';

interface CourseHeaderProps {
  readonly course: Program;
  readonly viewerAccess?: CourseViewerAccess;
  readonly products?: Product[];
}

function getPrimaryCta(
  courseSlug: string | null,
  isEnrollmentOpen: boolean | null | undefined,
  hasProducts: boolean,
  viewerAccess?: CourseViewerAccess,
): { label: string; href?: string; kind: 'link' | 'checkout' | 'enroll' | 'disabled' } {
  const signInHref = courseSlug ? `/sign-in?redirectTo=${encodeURIComponent(`/courses/${courseSlug}`)}` : '/sign-in';

  if (viewerAccess?.state === 'has-access' && courseSlug) {
    return { label: 'Continue learning', href: `/courses/${courseSlug}/content`, kind: 'link' };
  }

  if (viewerAccess?.state === 'signed-out') {
    return { label: 'Sign in to enroll', href: signInHref, kind: 'link' };
  }

  if (viewerAccess?.state === 'no-access' && isEnrollmentOpen && courseSlug && hasProducts) {
    return { label: 'Checkout', kind: 'checkout' };
  }

  if (viewerAccess?.state === 'no-access' && isEnrollmentOpen && courseSlug) {
    return { label: 'Enroll now', kind: 'enroll' };
  }

  if (viewerAccess?.state === 'unavailable') {
    return { label: 'Access temporarily unavailable', kind: 'disabled' };
  }

  return { label: 'Sign in to enroll', href: signInHref, kind: 'link' };
}

export function CourseHeader({ course, viewerAccess, products = [] }: CourseHeaderProps) {
  const thumbnailSrc = typeof course.thumbnail === 'string' && course.thumbnail.length > 0 ? course.thumbnail : null;
  const courseTitle = typeof course.title === 'string' && course.title.length > 0 ? course.title : 'Course';
  const courseSlug = typeof course.slug === 'string' && course.slug.length > 0 ? course.slug : null;
  const courseDescription = typeof course.description === 'string' ? course.description.trim() : '';
  const { name: levelName } = getCourseLevelConfig(course.difficulty as string | number | null | undefined);
  const categoryName = getCourseCategoryName(course.category as string | number | null | undefined);
  const program = getProgramForCourse(courseSlug);
  const showcase = getCourseShowcase(courseSlug);
  const heroImage = thumbnailSrc || program?.image;
  const isEnrollmentOpen = course.isEnrollmentOpen === true;
  const primaryCta = getPrimaryCta(courseSlug, isEnrollmentOpen, products.length > 0, viewerAccess);

  return (
    <section className="relative min-h-[780px] overflow-hidden border-b border-white/10">
      <div className="absolute inset-0">
        {heroImage ? (
          <Image
            src={heroImage}
            alt={courseTitle}
            fill
            unoptimized={shouldUseUnoptimizedCourseImage(heroImage)}
            className="object-cover"
            priority
            loading="eager"
            sizes="100vw"
          />
        ) : (
          <div className="h-full bg-[radial-gradient(circle_at_24%_18%,rgba(56,189,248,0.24),transparent_32%),linear-gradient(135deg,#020617,#111827_52%,#1e1b4b)]" />
        )}
        <div className="absolute inset-0 bg-gradient-to-r from-[#070a12] via-[#070a12]/90 to-[#070a12]/38" />
        <div className="absolute inset-0 bg-gradient-to-t from-[#070a12] via-transparent to-[#070a12]/25" />
      </div>

      <div className="container relative mx-auto flex min-h-[780px] flex-col px-4 py-10">
        <Button asChild variant="ghost" className="w-fit text-slate-300 hover:bg-white/10 hover:text-white">
          <Link href="/courses">
            <ArrowLeft />
            Back to catalog
          </Link>
        </Button>

        <div className="grid flex-1 gap-12 py-16 lg:grid-cols-[1fr_420px] lg:items-end">
          <div className="flex max-w-4xl flex-col gap-8">
            <div className="flex flex-wrap gap-2">
              <Badge variant="outline" className="border-white/20 bg-black/35 text-white backdrop-blur">
                {categoryName}
              </Badge>
              <Badge variant="outline" className="border-white/20 bg-black/35 text-white backdrop-blur">
                {levelName}
              </Badge>
              {program ? (
                <Badge variant="outline" className="border-white/20 bg-black/35 text-white backdrop-blur">
                  {program.shortTitle} package
                </Badge>
              ) : null}
            </div>

            <div className="flex flex-col gap-6">
              <h1 className="text-5xl font-semibold leading-[0.98] tracking-tight md:text-7xl">{courseTitle}</h1>
              <p className="max-w-3xl text-lg leading-8 text-slate-300 md:text-xl">{courseDescription || showcase?.headline}</p>
            </div>

            <div className="flex flex-wrap gap-3">
              {primaryCta.kind === 'checkout' && courseSlug ? (
                <CourseCheckoutButton
                  courseSlug={courseSlug}
                  products={products}
                  buttonClassName="h-10 px-6 text-sm font-medium md:h-11"
                />
              ) : primaryCta.kind === 'enroll' && courseSlug ? (
                <CourseSelfEnrollButton
                  courseSlug={courseSlug}
                  buttonClassName="h-10 bg-white px-6 text-sm font-medium text-slate-950 hover:bg-slate-200 md:h-11"
                />
              ) : primaryCta.kind === 'link' && primaryCta.href ? (
                <Button asChild size="lg" className="bg-white text-slate-950 hover:bg-slate-200">
                  <Link href={primaryCta.href}>
                    {primaryCta.label}
                    <ArrowRight />
                  </Link>
                </Button>
              ) : (
                <Button size="lg" disabled className="bg-white/20 text-white">
                  {primaryCta.label}
                </Button>
              )}
              {course.videoShowcaseUrl ? (
                <Button asChild size="lg" variant="outline" className="border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
                  <Link href={course.videoShowcaseUrl}>
                    Watch preview
                    <Play />
                  </Link>
                </Button>
              ) : null}
              <Button asChild size="lg" variant="outline" className="border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
                <Link href="#curriculum">
                  View curriculum
                  <Layers3 />
                </Link>
              </Button>
            </div>
          </div>

          <aside className="rounded-[2rem] border border-white/10 bg-white/[0.055] p-6 shadow-2xl shadow-black/30 backdrop-blur">
            <div className="flex flex-col gap-5">
              <div>
                <p className="text-sm font-semibold uppercase tracking-[0.16em] text-slate-500">Studio brief</p>
                <p className="mt-3 text-sm leading-6 text-slate-300">
                  {showcase?.studioPrompt || 'A practical GameGuild course with public catalog metadata, classroom content, and a focused project outcome.'}
                </p>
              </div>
              <div className="grid grid-cols-3 gap-3">
                <div className="rounded-2xl border border-white/10 bg-black/20 p-3">
                  <Clock className="mb-2 text-sky-200" />
                  <p className="text-sm font-semibold">{course.estimatedHours ?? 0}h</p>
                </div>
                <div className="rounded-2xl border border-white/10 bg-black/20 p-3">
                  <BookOpen className="mb-2 text-violet-200" />
                  <p className="text-sm font-semibold">{course.programContents?.length ?? 0}</p>
                </div>
                <div className="rounded-2xl border border-white/10 bg-black/20 p-3">
                  <Users className="mb-2 text-emerald-200" />
                  <p className="text-sm font-semibold">{course.currentEnrollments ?? 0}</p>
                </div>
              </div>
              {course.videoShowcaseUrl ? (
                <Button asChild variant="outline" className="border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
                  <Link href={course.videoShowcaseUrl}>
                    Watch preview
                    <Play />
                  </Link>
                </Button>
              ) : null}
            </div>
          </aside>
        </div>
      </div>
    </section>
  );
}
