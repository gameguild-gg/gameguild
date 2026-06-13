import { Badge } from '@/components/ui/badge';
import type { Program } from '@/lib/api/generated';
import type { CourseViewerAccess } from '@/lib/courses/services/course-viewer-access';
import { getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import { getProgramForCourse } from '@/lib/courses/public-programs';
import { BookOpen, Clock, Layers3, Star, Users } from 'lucide-react';
import CourseAccessCard from './course-access-card';

interface CourseSidebarProps {
  readonly course: Program;
  readonly viewerAccess: CourseViewerAccess;
}

export function CourseSidebar({ course, viewerAccess }: CourseSidebarProps) {
  const courseSlug = typeof course.slug === 'string' ? course.slug : null;
  const averageRating = typeof course.averageRating === 'number' ? course.averageRating : null;
  const levelConfig = getCourseLevelConfig(course.difficulty as string | number | null | undefined);
  const categoryName = getCourseCategoryName(course.category as string | number | null | undefined);
  const program = getProgramForCourse(courseSlug);

  return (
    <div className="sticky top-8 flex flex-col gap-5">
      <CourseAccessCard course={course} viewerAccess={viewerAccess} />

      <section className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-6 text-white shadow-2xl shadow-black/20 backdrop-blur">
        <h2 className="text-xl font-semibold">Course details</h2>
        <div className="mt-5 flex flex-col gap-4">
          <div className="flex items-center justify-between gap-4">
            <span className="text-sm text-slate-400">Level</span>
            <Badge variant="secondary" className="bg-white/10 text-white">
              {levelConfig.name}
            </Badge>
          </div>
          <div className="flex items-center justify-between gap-4">
            <span className="text-sm text-slate-400">Discipline</span>
            <span className="text-right text-sm text-white">{categoryName}</span>
          </div>
          {course.estimatedHours ? (
            <div className="flex items-center justify-between gap-4">
              <span className="flex items-center gap-2 text-sm text-slate-400">
                <Clock />
                Duration
              </span>
              <span className="text-sm text-white">{course.estimatedHours} hours</span>
            </div>
          ) : null}
          <div className="flex items-center justify-between gap-4">
            <span className="flex items-center gap-2 text-sm text-slate-400">
              <Users />
              Learners
            </span>
            <span className="text-sm text-white">{course.currentEnrollments ?? 0}</span>
          </div>
          <div className="flex items-center justify-between gap-4">
            <span className="flex items-center gap-2 text-sm text-slate-400">
              <BookOpen />
              Content items
            </span>
            <span className="text-sm text-white">{course.programContents?.length ?? 0}</span>
          </div>
          {averageRating !== null ? (
            <div className="flex items-center justify-between gap-4">
              <span className="flex items-center gap-2 text-sm text-slate-400">
                <Star />
                Rating
              </span>
              <span className="text-sm text-white">{averageRating.toFixed(1)}</span>
            </div>
          ) : null}
        </div>
      </section>

      {program ? (
        <section className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-6 text-white shadow-2xl shadow-black/20 backdrop-blur">
          <div className="flex items-start gap-4">
            <div className="rounded-2xl border border-white/10 bg-black/20 p-3 text-sky-200">
              <Layers3 />
            </div>
            <div>
              <h2 className="text-xl font-semibold">{program.shortTitle} package</h2>
              <p className="mt-3 text-sm leading-6 text-slate-400">{program.summary}</p>
            </div>
          </div>
        </section>
      ) : null}
    </div>
  );
}
