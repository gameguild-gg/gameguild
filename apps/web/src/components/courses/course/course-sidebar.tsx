import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Program } from '@/lib/api/generated';
import type { CourseViewerAccess } from '@/lib/courses/services/course-viewer-access';
import { getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import { BookOpen, Clock, Star, Users } from 'lucide-react';
import CourseAccessCard from './course-access-card';

interface CourseSidebarProps {
  readonly course: Program;
  readonly viewerAccess: CourseViewerAccess;
}

export function CourseSidebar({ course, viewerAccess }: CourseSidebarProps) {
  const averageRating = typeof course.averageRating === 'number' ? course.averageRating : null;
  const levelConfig = getCourseLevelConfig(course.difficulty as string | number | null | undefined);
  const categoryName = getCourseCategoryName(course.category as string | number | null | undefined);

  return (
    <div className="sticky top-8 space-y-6">
      {/* Course Access Card */}
      <CourseAccessCard course={course} viewerAccess={viewerAccess} />

      {/* Course Stats */}
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="text-lg">Course Information</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div className="flex justify-between items-center">
              <span className="text-gray-300">Level</span>
              <Badge variant="secondary" className={levelConfig.bgColor}>
                {levelConfig.name}
              </Badge>
            </div>

            <div className="flex justify-between items-center">
              <span className="text-gray-300">Category</span>
              <span className="text-white">{categoryName}</span>
            </div>

            {course.estimatedHours && (
              <div className="flex justify-between items-center">
                <span className="text-gray-300">Duration</span>
                <span className="text-white flex items-center gap-1">
                  <Clock className="w-4 h-4" />
                  {course.estimatedHours} hours
                </span>
              </div>
            )}

            {course.currentEnrollments !== undefined && (
              <div className="flex justify-between items-center">
                <span className="text-gray-300">Enrolled</span>
                <span className="text-white flex items-center gap-1">
                  <Users className="w-4 h-4" />
                  {course.currentEnrollments}
                </span>
              </div>
            )}

            {averageRating !== null && (
              <div className="flex justify-between items-center">
                <span className="text-gray-300">Rating</span>
                <span className="text-white flex items-center gap-1">
                  <Star className="w-4 h-4 fill-yellow-400 text-yellow-400" />
                  {averageRating.toFixed(1)}
                </span>
              </div>
            )}

            {course.isEnrollmentOpen !== undefined && (
              <div className="flex justify-between items-center">
                <span className="text-gray-300">Enrollment</span>
                <Badge variant={course.isEnrollmentOpen ? "default" : "destructive"}>
                  {course.isEnrollmentOpen ? "Open" : "Closed"}
                </Badge>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Learning Journey */}
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="text-lg">Learning Journey</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-blue-600 rounded-lg flex items-center justify-center">
                <BookOpen className="h-5 w-5 text-white" />
              </div>
              <div>
                <h4 className="font-semibold">Catalog to Classroom</h4>
                <p className="text-sm text-gray-400">Public discovery and guided attendance</p>
              </div>
            </div>
            <p className="text-sm text-gray-300">
              Learners discover published courses in the public catalog, then move into the dedicated learning app once course access is granted.
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
