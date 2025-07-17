import type { Course } from '@/types/course-enhanced';
import { Badge } from '@game-guild/ui/components/badge';
import { CheckCircle, Target } from 'lucide-react';
import { getCourseLevelConfig } from '@/lib/courses/services/course.service';

interface CourseOverviewProps {
  readonly course: Course;
}

const LEARNING_OBJECTIVES = [
  'Master the fundamentals',
  'Build practical projects to reinforce learning',
  'Apply industry best practices and workflows',
  'Prepare for real-world development challenges',
] as const;

export function CourseOverview({ course }: CourseOverviewProps) {
  const { name: levelName, color: levelColor } = getCourseLevelConfig(course.level);

  return (
    <section>
      <h2 className="text-2xl font-bold mb-6 flex items-center">
        <Target className="mr-3 h-6 w-6 text-blue-400" />
        Course Overview
      </h2>

      <div className="bg-gray-800/50 rounded-xl p-6 border border-gray-700">
        {/* Level and Area Badges */}
        <div className="flex flex-wrap gap-2 mb-6">
          <Badge className={`border ${levelColor}`}>{levelName}</Badge>
          <Badge variant="outline" className="border-gray-600 text-gray-300">
            {course.area.charAt(0).toUpperCase() + course.area.slice(1)}
          </Badge>
        </div>

        <p className="text-lg text-gray-300 leading-relaxed mb-6">{course.description}</p>

        <div className="grid md:grid-cols-2 gap-6">
          <div>
            <h3 className="text-lg font-semibold mb-3 text-blue-400">What You&apos;ll Learn</h3>
            <ul className="space-y-2">
              {LEARNING_OBJECTIVES.map((objective, index) => (
                <li key={index} className="flex items-start">
                  <CheckCircle className="mr-2 h-5 w-5 text-green-400 mt-0.5 flex-shrink-0" />
                  <span className="text-gray-300">
                    {objective} {index === 0 && course.area ? `of ${course.area} development` : ''}
                  </span>
                </li>
              ))}
            </ul>
          </div>

          <div>
            <h3 className="text-lg font-semibold mb-3 text-blue-400">Prerequisites</h3>
            <ul className="space-y-2 text-gray-300">
              <li>• Basic computer literacy</li>
              <li>• Enthusiasm for learning</li>
              <li>• {course.level > 1 ? 'Some programming experience recommended' : 'No prior experience required'}</li>
              <li>• Access to development tools</li>
            </ul>
          </div>
        </div>
      </div>
    </section>
  );
}
