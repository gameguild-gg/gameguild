import { Program, ProgramContentType } from '@/lib/api/generated';
import { BookOpen, Code, FileText, MessageSquare } from 'lucide-react';

interface CourseToolsProps {
  readonly course: Program;
}

function getContentTypeName(type: number | null | undefined): string {
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
      return 'Code labs';
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
}

function getContentIcon(typeName: string) {
  if (typeName.includes('Code') || typeName.includes('Challenge') || typeName.includes('Assignment')) {
    return <Code />;
  }

  if (typeName.includes('Discussion') || typeName.includes('Questionnaire')) {
    return <MessageSquare />;
  }

  return <FileText />;
}

export function CourseTools({ course }: CourseToolsProps) {
  const contentItems = course.programContents ?? [];

  if (contentItems.length === 0) {
    return null;
  }

  const contentTypeCounts = contentItems.reduce<Record<string, number>>((counts, item) => {
    const key = getContentTypeName(typeof item.type === 'number' ? item.type : null);
    counts[key] = (counts[key] ?? 0) + 1;
    return counts;
  }, {});

  const publishedContentTypes = Object.entries(contentTypeCounts).sort((left, right) => right[1] - left[1]);

  return (
    <section className="rounded-[2rem] border border-white/10 bg-white/[0.045] p-7 text-white">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-end">
        <div>
          <h2 className="flex items-center gap-3 text-3xl font-semibold tracking-tight">
            <BookOpen className="text-violet-200" />
            Learning activities
          </h2>
          <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">
            Content types give students a quick read on the course rhythm before they enter the classroom.
          </p>
        </div>
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {publishedContentTypes.map(([contentType, count]) => (
          <div key={contentType} className="rounded-2xl border border-white/10 bg-black/20 p-4">
            <div className="mb-4 text-slate-300">{getContentIcon(contentType)}</div>
            <p className="text-2xl font-semibold">{count}</p>
            <p className="mt-1 text-sm text-slate-400">{contentType}</p>
          </div>
        ))}
      </div>
    </section>
  );
}
