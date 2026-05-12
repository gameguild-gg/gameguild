import { Program, ProgramContentType } from '@/lib/api/generated';
import { BookOpen } from 'lucide-react';

interface CourseToolsProps {
  readonly course: Program;
}

export function CourseTools({ course }: CourseToolsProps) {
  const contentItems = course.programContents ?? [];

  if (contentItems.length === 0) {
    return null;
  }

  const getContentTypeName = (type: number | null | undefined): string => {
    switch (type) {
      case ProgramContentType.Page:
        return 'Pages';
      case ProgramContentType.Assignment:
        return 'Assignments';
      case ProgramContentType.Questionnaire:
        return 'Questionnaires';
      case ProgramContentType.Discussion:
        return 'Discussions';
      case ProgramContentType.Code:
        return 'Code Labs';
      case ProgramContentType.Challenge:
        return 'Challenges';
      case ProgramContentType.Reflection:
        return 'Reflections';
      case ProgramContentType.Survey:
        return 'Surveys';
      case ProgramContentType.Lesson:
      default:
        return 'Lessons';
    }
  };

  const contentTypeCounts = contentItems.reduce<Record<string, number>>((counts, item) => {
    const key = getContentTypeName(typeof item.type === 'number' ? item.type : null);
    counts[key] = (counts[key] ?? 0) + 1;
    return counts;
  }, {});

  const publishedContentTypes = Object.entries(contentTypeCounts).sort((left, right) => right[1] - left[1]);

  return (
    <section>
      <h2 className="text-2xl font-bold mb-6 flex items-center">
        <BookOpen className="mr-3 h-6 w-6 text-purple-400" />
        Published Content Types
      </h2>
      <div className="bg-gray-800/50 rounded-xl p-6 border border-gray-700">
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {publishedContentTypes.map(([contentType, count]) => (
            <div key={contentType} className="flex items-center justify-between p-3 bg-gray-700/50 rounded-lg border border-gray-600 gap-3">
              <div className="w-8 h-8 bg-gray-600 rounded mr-3 flex items-center justify-center">
                <BookOpen className="h-4 w-4 text-gray-300" />
              </div>
              <span className="text-sm font-medium text-gray-300 flex-1">{contentType}</span>
              <span className="text-sm font-semibold text-white">{count}</span>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
