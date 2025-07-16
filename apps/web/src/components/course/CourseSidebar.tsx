import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Button } from '@game-guild/ui/components';
import { BookOpen } from 'lucide-react';
import Link from 'next/link';
import CourseAccessCard from './CourseAccessCard';

interface CourseSidebarProps {
  readonly courseSlug: string;
  readonly courseTitle: string;
}

export function CourseSidebar({ courseSlug }: CourseSidebarProps) {
  return (
    <div className="sticky top-8 space-y-6">
      {/* Course Access Card */}
      <CourseAccessCard courseSlug={courseSlug} />

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
            <p className="text-sm text-gray-300">
              This course is part of a comprehensive learning track designed to take you from beginner to professional game developer.
            </p>
            <Button asChild variant="outline" className="w-full border-gray-600 hover:bg-gray-700">
              <Link href="/tracks/game-development-fundamentals">View Full Track</Link>
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
