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
  const { name: levelName, color: levelColor } = getCourseLevelConfig(course.difficulty || 0);
  const categoryName = getCourseCategoryName(course.category || 0);

  return (
    <>
      {/* Navigation */}
      <div className="border-b border-gray-800">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center gap-2 text-sm">
            <Button asChild variant="ghost" className="text-gray-300 hover:text-white p-0">
              <Link href="/programs">Courses</Link>
            </Button>
            <span className="text-gray-500">/</span>
            <Button asChild variant="ghost" className="text-gray-300 hover:text-white p-0">
              <Link href="/tracks">Learning Tracks</Link>
            </Button>
            <span className="text-gray-500">/</span>
            <span className="text-gray-400">{course.title}</span>
          </div>
          <Button asChild variant="ghost" className="text-gray-300 hover:text-white mt-2">
            <Link href="/tracks">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Learning Tracks
            </Link>
          </Button>
        </div>
      </div>

      {/* Hero Section */}
      <section className="relative">
        <div className="aspect-video relative overflow-hidden rounded-xl bg-gray-800">
          <Image src={course.thumbnail || '/placeholder.svg'} alt={course.title} fill className="object-cover" priority />
          <div className="absolute inset-0 bg-gradient-to-t from-gray-900/80 to-transparent" />
          <div className="absolute bottom-6 left-6 right-6">
            <div className="flex flex-wrap gap-2 mb-4">
              <Badge className={`border ${levelColor}`}>{levelName}</Badge>
              <Badge variant="outline" className="border-gray-600 text-gray-300">
                {categoryName}
              </Badge>
            </div>
            <h1 className="text-4xl font-bold mb-2">{course.title}</h1>
            <p className="text-xl text-gray-300 leading-relaxed">{course.description}</p>
          </div>
        </div>
      </section>
    </>
  );
}
