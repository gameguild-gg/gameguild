import type { Course } from '@/types/course-enhanced';
import Image from 'next/image';
import Link from 'next/link';
import { Button } from '@game-guild/ui/components/button';
import { Badge } from '@game-guild/ui/components/badge';
import { ArrowLeft } from 'lucide-react';
import { getCourseLevelConfig } from '@/lib/courses/services/course.service';

interface CourseHeaderProps {
  readonly course: Course;
}

export function CourseHeader({ course }: CourseHeaderProps) {
  const { name: levelName, color: levelColor } = getCourseLevelConfig(course.level);

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
          <Image src={course.image || '/placeholder.svg'} alt={course.title} fill className="object-cover" priority />
          <div className="absolute inset-0 bg-gradient-to-t from-gray-900/80 to-transparent" />
          <div className="absolute bottom-6 left-6 right-6">
            <div className="flex flex-wrap gap-2 mb-4">
              <Badge className={`border ${levelColor}`}>{levelName}</Badge>
              <Badge variant="outline" className="border-gray-600 text-gray-300">
                {course.area.charAt(0).toUpperCase() + course.area.slice(1)}
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
