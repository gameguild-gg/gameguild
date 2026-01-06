'use client';
import { BookOpen } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
type CourseCardCourse = {
  id: string;
  slug: string;
  title: string;
  description?: string;
  thumbnailUrl?: string;
  coverUrl?: string;
  duration?: number;
  level?: string;
  totalStudents?: number;
};

export function CourseCard({ course }: { course: CourseCardCourse; viewMode?: 'grid' | 'list' }): React.JSX.Element {
  const imageUrl = course.thumbnailUrl ?? course.coverUrl ?? '';

  return (
    <Link href={`/p/${course.slug}`} className="block">
      <div className="overflow-hidden border rounded-lg bg-card cursor-pointer transition-transform duration-200 hover:scale-[1.02] hover:shadow-lg">
        {/* Thumbnail */}
        <div className="relative aspect-video w-full overflow-hidden bg-slate-700/50">
          {imageUrl ? (
            <Image
              src={imageUrl}
              alt={course.title}
              fill
              loading="lazy"
              className="object-cover transition-transform duration-300 hover:scale-105"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center">
              <BookOpen className="h-10 w-10 text-slate-400" />
            </div>
          )}
        </div>

        {/* Content */}
        <div className="p-4">
          <h3 className="font-semibold text-lg mb-2">{course.title}</h3>
          <p className="text-sm text-muted-foreground mb-3">{course.description}</p>
          <div className="flex items-center gap-4 text-xs text-muted-foreground">
            <span>Duration: {Math.round((course.duration ?? 0) / 60)}h</span>
            <span>Level: {course.level}</span>
            <span>Students: {course.totalStudents ?? 0}</span>
          </div>
        </div>
      </div>
    </Link>
  );
}
