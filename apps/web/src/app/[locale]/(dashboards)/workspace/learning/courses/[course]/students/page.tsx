import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getCourseStudents } from '@/lib/learning';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { CheckCircle2, Clock, TrendingUp, Users } from 'lucide-react';
import { StudentTable } from '@/components/learning/console/courses/[course]/students/student-table';

export default async function Page({ params }: PageProps<'/[locale]/workspace/learning/courses/[course]/students'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const [course, studentsData] = await Promise.all([getCourse(courseId), getCourseStudents(courseId)]);

  if (!course) {
    notFound();
  }

  const { students, total } = studentsData;

  const sevenDaysAgo = Date.now() - 7 * 24 * 60 * 60 * 1000;
  const enriched = students.map((student) => ({
    ...student,
    completionPercent: student.progress,
    isActive: new Date(student.lastActivity).getTime() > sevenDaysAgo,
  }));

  const activeCount = enriched.filter((s) => s.isActive).length;
  const completedCount = enriched.filter((s) => s.completionPercent >= 100).length;
  const avgProgress = enriched.length > 0 ? Math.round(enriched.reduce((acc, s) => acc + s.completionPercent, 0) / enriched.length) : 0;

  return (
    <div className="flex flex-col gap-6">
      {/* Stats */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-blue-100 dark:bg-blue-900">
              <Users className="size-5 text-blue-600 dark:text-blue-400" />
            </div>
            <div>
              <p className="text-2xl font-bold">{total}</p>
              <p className="text-sm text-muted-foreground">Total Enrolled</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-green-100 dark:bg-green-900">
              <TrendingUp className="size-5 text-green-600 dark:text-green-400" />
            </div>
            <div>
              <p className="text-2xl font-bold">{activeCount}</p>
              <p className="text-sm text-muted-foreground">Active (7d)</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-purple-100 dark:bg-purple-900">
              <CheckCircle2 className="size-5 text-purple-600 dark:text-purple-400" />
            </div>
            <div>
              <p className="text-2xl font-bold">{completedCount}</p>
              <p className="text-sm text-muted-foreground">Completed</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-orange-100 dark:bg-orange-900">
              <Clock className="size-5 text-orange-600 dark:text-orange-400" />
            </div>
            <div>
              <p className="text-2xl font-bold">{avgProgress}%</p>
              <p className="text-sm text-muted-foreground">Avg Progress</p>
            </div>
          </CardContent>
        </Card>
      </div>

      <StudentTable courseId={course.id} students={enriched} total={total} />
    </div>
  );
}
