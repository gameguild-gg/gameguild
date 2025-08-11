import { CourseFeatures } from '@/components/courses/course/course-features';
import { CourseHeader } from '@/components/courses/course/course-header';
import { CourseOverview } from '@/components/courses/course/course-overview';
import { CourseSidebar } from '@/components/courses/course/course-sidebar';
import { CourseTools } from '@/components/courses/course/course-tools';
import { getCourseBySlug } from '@/lib/courses/services/course.service';
import { Loader2 } from 'lucide-react';
import { notFound } from 'next/navigation';
import { Suspense } from 'react';

// Generate metadata for SEO
export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const result = await getCourseBySlug(slug);

  if (!result.success) {
    return {
      title: 'Course Not Found',
      description: 'The requested course could not be found.',
    };
  }

  const course = result.data;

  if (!course) {
    return {
      title: 'Course Not Found',
      description: 'The requested course could not be found.',
    };
  }

  return {
    title: `${course.title} | Game Guild`,
    description: course.description,
    openGraph: {
      title: course.title,
      description: course.description,
      images: [course.thumbnail || 'https://placehold.co/1200x630/1f2937/ffffff?text=Course+Image'],
    },
  };
}

interface CourseDetailPageProps {
  readonly params: Promise<{ slug: string }>;
}

async function CourseContent({ slug }: { slug: string }) {
  const result = await getCourseBySlug(slug);

  if (!result.success) {
    console.error('Error fetching course:', result.error);
    notFound();
  }

  const course = result.data;

  if (!course) {
    return {
      title: 'Course Not Found',
      description: 'The requested course could not be found.',
    };
  }

  return (
    <div className="min-h-screen bg-gray-950 text-white">
      <CourseHeader course={course} />

      <div className="container mx-auto px-4 py-8">
        <div className="grid lg:grid-cols-3 gap-8">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-8">
            <CourseOverview course={course} />
            <CourseTools course={course} />
            <CourseFeatures />
          </div>

          {/* Right Sidebar */}
          <div className="lg:col-span-1">
            <CourseSidebar course={course} />
          </div>
        </div>
      </div>
    </div>
  );
}

export default async function CourseDetailPage({ params }: CourseDetailPageProps) {
  const { slug } = await params;

  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-gray-950 text-white flex items-center justify-center">
          <div className="flex flex-col items-center space-y-4">
            <Loader2 className="h-8 w-8 animate-spin" />
            <p className="text-gray-400">Loading course...</p>
          </div>
        </div>
      }
    >
      <CourseContent slug={slug} />
    </Suspense>
  );
}
