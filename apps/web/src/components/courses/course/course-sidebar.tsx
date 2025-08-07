import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Program } from '@/lib/api/generated';
import { getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import { BookOpen, Clock, Star, Users } from 'lucide-react';
import Link from 'next/link';
import CourseAccessCard from './course-access-card';

interface CourseSidebarProps {
  readonly course: Program;
}

export function CourseSidebar({ course }: CourseSidebarProps) {
  const levelConfig = getCourseLevelConfig(course.difficulty || 0);
  const categoryName = getCourseCategoryName(course.category || 0);

  return (
    <div className="sticky top-8 space-y-6">
      {/* Course Access Card */}
      <CourseAccessCard courseSlug={course.slug || ''} />

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

            {course.averageRating !== undefined && (
              <div className="flex justify-between items-center">
                <span className="text-gray-300">Rating</span>
                <span className="text-white flex items-center gap-1">
                  <Star className="w-4 h-4 fill-yellow-400 text-yellow-400" />
                  {course.averageRating.toFixed(1)}
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

      {/* Part of Track */}
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="text-lg">Part of Learning Track</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-blue-600 rounded-lg flex items-center justify-center">
                <BookOpen className="h-5 w-5 text-white" />
              </div>
              <div>
                <h4 className="font-semibold">Game Development Fundamentals</h4>
                <p className="text-sm text-gray-400">Complete learning track</p>
              </div>
            </div>
            <p className="text-sm text-gray-300">This course is part of a comprehensive learning track designed to take you from beginner to professional game developer.</p>
            <Button asChild variant="outline" className="w-full border-gray-600 hover:bg-gray-700">
              <Link href="/tracks/game-development-fundamentals">View Full Track</Link>
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
