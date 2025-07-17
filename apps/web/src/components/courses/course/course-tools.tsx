import type { Course } from '@/components/legacy/types/course-enhanced';
import { BookOpen } from 'lucide-react';

interface CourseToolsProps {
  readonly tools: Course['tools'];
}

export function CourseTools({ tools }: CourseToolsProps) {
  if (!tools || tools.length === 0) {
    return null;
  }

  return (
    <section>
      <h2 className="text-2xl font-bold mb-6 flex items-center">
        <BookOpen className="mr-3 h-6 w-6 text-purple-400" />
        Tools & Technologies
      </h2>
      <div className="bg-gray-800/50 rounded-xl p-6 border border-gray-700">
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {tools.map((tool, index) => (
            <div key={`${tool}-${index}`} className="flex items-center p-3 bg-gray-700/50 rounded-lg border border-gray-600">
              <div className="w-8 h-8 bg-gray-600 rounded mr-3 flex items-center justify-center">
                <BookOpen className="h-4 w-4 text-gray-300" />
              </div>
              <span className="text-sm font-medium text-gray-300">{tool}</span>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
