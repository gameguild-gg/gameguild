'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { getCourseBySlug } from '@/lib/courses/actions';
import { Course } from '@/components/legacy/types/courses';
import { CourseEditorProvider } from '@/lib/courses/course-editor.context';
import { CourseEditor } from '@/components/courses/course-editor/course-editor';
import { Skeleton } from '@/components/ui/skeleton';

export default function CourseDetailPage() {
  const params = useParams();
  const slug = params.slug as string;

  const [course, setCourse] = useState<Course | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchCourse = async () => {
      try {
        setLoading(true);
        setError(null);

        const courseData = await getCourseBySlug(slug);
        if (courseData) {
          setCourse(courseData);
        } else {
          setError('Course not found');
        }
      } catch (err) {
        console.error('Error fetching course:', err);
        setError('Failed to load course');
      } finally {
        setLoading(false);
      }
    };

    if (slug) {
      fetchCourse();
    }
  }, [slug]);

  if (loading) {
    return (
      <div className="min-h-screen bg-background">
        <div className="container mx-auto max-w-7xl p-6">
          <div className="space-y-8">
            <Skeleton className="h-16 w-full" />
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
              <div className="lg:col-span-2 space-y-8">
                <Skeleton className="h-96 w-full" />
                <Skeleton className="h-64 w-full" />
              </div>
              <div className="space-y-8">
                <Skeleton className="h-48 w-full" />
                <Skeleton className="h-32 w-full" />
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (error || !course) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-foreground mb-2">Course Not Found</h1>
          <p className="text-muted-foreground mb-4">{error || 'The requested course could not be found.'}</p>
          <a href="/dashboard/courses" className="text-primary hover:underline">
            Return to Courses
          </a>
        </div>
      </div>
    );
  }

  // Transform course data to match our editor format
  const initialCourseData = {
    id: course.id,
    title: course.title,
    slug: course.slug,
    description: course.description,
    summary: course.description.substring(0, 200), // Use first 200 chars as summary
    category: course.area,
    difficulty: course.level,
    estimatedHours: 10, // Default hours since not available in legacy type
    status: 'published' as const, // Default status
    media: {
      thumbnail: undefined, // Will be handled by the media section
      showcaseVideo: undefined,
    },
    products: [],
    enrollment: {
      isOpen: true,
      currentEnrollments: 0,
    },
    tags: Array.isArray(course.tools) ? course.tools : [],
    manualSlugEdit: true, // Since it's an existing course
  };

  return (
    <CourseEditorProvider initialCourse={initialCourseData}>
      <CourseEditor courseSlug={slug} isCreating={false} />
    </CourseEditorProvider>
  );
}
