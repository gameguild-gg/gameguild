import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Program } from '@/lib/api/generated';
import { getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import { ArrowLeft } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';

interface CourseHeaderProps {
  readonly course: Program;
}

export function CourseHeader({ course }: CourseHeaderProps) {
  const thumbnailSrc = typeof course.thumbnail === 'string' && course.thumbnail.length > 0 ? course.thumbnail : null;
  const courseTitle = typeof course.title === 'string' && course.title.length > 0 ? course.title : 'Course';
  const courseDescription = typeof course.description === 'string' ? course.description : '';
  const { name: levelName, color: levelColor } = getCourseLevelConfig(course.difficulty as string | number | null | undefined);
  const categoryName = getCourseCategoryName(course.category as string | number | null | undefined);

  return (
    <>
      {/* Navigation */}
      <div className="border-b border-gray-800">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center gap-2 text-sm">
            <Button asChild variant="ghost" className="text-gray-300 hover:text-white p-0">
              <Link href="/courses">Courses</Link>
            </Button>
            <span className="text-gray-500">/</span>
            <span className="text-gray-400">{courseTitle}</span>
          </div>
          <Button asChild variant="ghost" className="text-gray-300 hover:text-white mt-2">
            <Link href="/courses">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Course Catalog
            </Link>
          </Button>
        </div>
      </div>

      {/* Hero Section */}
      <section className="relative">
        <div className="aspect-video relative overflow-hidden rounded-xl bg-gray-800">
          {thumbnailSrc ? (
            <Image src={thumbnailSrc} alt={courseTitle} fill className="object-cover" priority />
          ) : (
            <div className="h-full w-full bg-[radial-gradient(circle_at_20%_15%,rgba(59,130,246,0.45),transparent_30%),linear-gradient(135deg,#111827,#1e3a8a_50%,#111827)]" />
          )}
          <div className="absolute inset-0 bg-gradient-to-t from-gray-900/80 to-transparent" />
          <div className="absolute bottom-6 left-6 right-6">
            <div className="flex flex-wrap gap-2 mb-4">
              <Badge className={`border ${levelColor}`}>{levelName}</Badge>
              <Badge variant="outline" className="border-gray-600 text-gray-300">
                {categoryName}
              </Badge>
            </div>
            <h1 className="text-4xl font-bold mb-2">{courseTitle}</h1>
            <p className="text-xl text-gray-300 leading-relaxed">{courseDescription}</p>
          </div>
        </div>
      </section>
    </>
  );
}
