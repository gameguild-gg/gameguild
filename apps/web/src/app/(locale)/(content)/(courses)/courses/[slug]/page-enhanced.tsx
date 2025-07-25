import { Suspense } from 'react';
import { notFound } from 'next/navigation';
import { getCourseBySlug, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import { CourseHeader } from '@/components/courses/course/course-header';
import { CourseOverview } from '@/components/courses/course/course-overview';
import { CourseSidebar } from '@/components/courses/course/course-sidebar';
import CourseAccessCard from '@/components/courses/course/course-access-card';
import { Skeleton } from '@/components/ui/skeleton';
import { ErrorBoundary } from '@/components/legacy/custom/error-boundary';

interface CourseDetailPageProps {
  params: Promise<{ slug: string }>;
}

export default async function CourseDetailPage({ params }: CourseDetailPageProps) {
  const { slug } = await params;

  const courseResult = await getCourseBySlug(slug);

  if (!courseResult.success) {
    console.error('Error fetching course:', courseResult.error);
    notFound();
  }

  const course = courseResult.data;
  const levelConfig = getCourseLevelConfig(course.level);

  return (
    <div className="min-h-screen bg-gray-950 text-white">
      <CourseHeader course={course} />

      <div className="container mx-auto px-4 py-8">
        <div className="grid lg:grid-cols-3 gap-8">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-8">
            <ErrorBoundary fallback={<div>Failed to load course overview</div>}>
              <Suspense fallback={<Skeleton className="h-32 w-full" />}>
                <CourseOverview course={course} levelConfig={levelConfig} />
              </Suspense>
            </ErrorBoundary>
          </div>

          {/* Sidebar */}
          <div className="lg:col-span-1">
            <div className="sticky top-8 space-y-6">
              <ErrorBoundary fallback={<div>Failed to load course access</div>}>
                <Suspense fallback={<Skeleton className="h-24 w-full" />}>
                  <CourseAccessCard courseSlug={course.slug} courseTitle={course.title} />
                </Suspense>
              </ErrorBoundary>

              <ErrorBoundary fallback={<div>Failed to load course info</div>}>
                <CourseSidebar course={course} />
              </ErrorBoundary>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// Generate static params for better performance
export async function generateStaticParams() {
  // This would ideally come from your CMS or API
  return [{ slug: 'game-dev-portfolio' }, { slug: 'game-jam-survival' }, { slug: 'retro-game-development' }];
}

// Metadata generation
export async function generateMetadata({ params }: CourseDetailPageProps) {
  const { slug } = await params;
  const courseResult = await getCourseBySlug(slug);

  if (!courseResult.success) {
    return {
      title: 'Course Not Found',
      description: 'The requested course could not be found.',
    };
  }

  const course = courseResult.data;

  return {
    title: `${course.title} | Game Guild`,
    description: course.description,
    openGraph: {
      title: course.title,
      description: course.description,
      images: [course.image],
    },
  };
}
