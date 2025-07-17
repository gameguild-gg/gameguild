import { getCourseBySlug } from '@/lib/courses/services/course.service';
import { notFound } from 'next/navigation';
import { Suspense } from 'react';
import { CourseHeader } from '@/components/course/CourseHeader';
import { CourseOverview } from '@/components/course/CourseOverview';
import { CourseTools } from '@/components/course/CourseTools';
import { CourseFeatures } from '@/components/course/CourseFeatures';
import { CourseSidebar } from '@/components/course/CourseSidebar';


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

  return (
    <div className="min-h-screen bg-gray-950 text-white">
      <CourseHeader course={course} />

      <div className="container mx-auto px-4 py-8">
        <div className="grid lg:grid-cols-3 gap-8">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-8">
            <CourseOverview course={course} />
            <CourseTools tools={course.tools} />
            <CourseFeatures />
          </div>

          {/* Right Sidebar */}
          <div className="lg:col-span-1">
            <CourseSidebar courseSlug={slug} courseTitle={course.title} />
          </div>
        </div>
      </div>
    </div>
  );
}

export default async function CourseDetailPage({ params }: CourseDetailPageProps) {
  const { slug } = await params;

  return (
    <Suspense fallback={<Loading />}>
      <CourseContent slug={slug} />
    </Suspense>
  );
}
