'use client';

import MarkdownRenderer from '@/components/markdown-renderer/markdown-renderer';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { getProgramBySlug } from '@/data/courses/mock-data';
import { Program, ProgramContent } from '@/lib/api/generated';
import { BookOpen, CheckCircle, ChevronRight, Circle, Code, FileText, Play } from 'lucide-react';
import { useParams } from 'next/navigation';
import { useEffect, useState } from 'react';

// Helper function to get content type name
const getContentTypeName = (type: number): string => {
  switch (type) {
    case 0: return 'text';
    case 1: return 'video';
    case 2: return 'exercise';
    case 3: return 'discussion';
    case 4: return 'code';
    case 5: return 'challenge';
    case 6: return 'reflection';
    case 7: return 'survey';
    default: return 'text';
  }
};

// Helper function to get content type for lesson type
const getLessonType = (type: number): 'text' | 'video' | 'exercise' | 'code' => {
  switch (type) {
    case 1: return 'video';
    case 2: return 'exercise';
    case 4: return 'code';
    default: return 'text';
  }
};

const getLessonIcon = (type: string) => {
  switch (type) {
    case 'video':
      return <Play className="h-4 w-4" />;
    case 'exercise':
      return <Code className="h-4 w-4" />;
    case 'code':
      return <Code className="h-4 w-4" />;
    default:
      return <FileText className="h-4 w-4" />;
  }
};

const getLessonTypeColor = (type: string) => {
  switch (type) {
    case 'video':
      return 'bg-blue-100 text-blue-800 dark:bg-blue-900/20 dark:text-blue-300';
    case 'exercise':
      return 'bg-green-100 text-green-800 dark:bg-green-900/20 dark:text-green-300';
    case 'code':
      return 'bg-purple-100 text-purple-800 dark:bg-purple-900/20 dark:text-purple-300';
    default:
      return 'bg-muted text-muted-foreground';
  }
};

export default function CourseContentPage() {
  const params = useParams();
  const slug = params.slug as string;
  const [selectedContent, setSelectedContent] = useState<ProgramContent | null>(null);
  const [program, setProgram] = useState<Program | null>(null);

  useEffect(() => {
    const courseData = getProgramBySlug(slug);
    setProgram(courseData);

    // Set the first content as selected by default
    if (courseData?.programContents && courseData.programContents.length > 0) {
      const firstContent = courseData.programContents[0];
      if (firstContent) {
        setSelectedContent(firstContent);
      }
    }
  }, [slug]);

  const totalContent = program?.programContents?.length || 0;
  const completedContent = program?.programContents?.filter(content => content.isRequired)?.length || 0;
  const progressPercentage = totalContent > 0 ? (completedContent / totalContent) * 100 : 0;

  const handleContentClick = (content: ProgramContent) => {
    setSelectedContent(content);
  };

  const handleContentComplete = (contentId: string) => {
    // In a real implementation, this would update the backend
    console.log(`Marking content ${contentId} as completed`);
  };

  if (!program) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-foreground mb-4">Course Not Found</h1>
          <p className="text-muted-foreground">The requested course could not be found.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Course Header */}
      <div className="mb-8">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h1 className="text-3xl font-bold text-foreground">{program.title}</h1>
            <p className="text-muted-foreground mt-2">
              {program.description}
            </p>
          </div>
          <div className="text-right">
            <div className="text-2xl font-bold text-foreground">{completedContent}/{totalContent}</div>
            <div className="text-sm text-muted-foreground">Content completed</div>
          </div>
        </div>
        <Progress value={progressPercentage} className="h-2" />
        <div className="text-sm text-muted-foreground mt-2">
          {Math.round(progressPercentage)}% complete
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Course Content */}
        <div className="lg:col-span-1">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <BookOpen className="h-5 w-5" />
                Course Content
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {program.programContents?.map((content) => (
                <div key={content.id} className="space-y-2">
                  <button
                    onClick={() => handleContentClick(content)}
                    className={`w-full flex items-center justify-between p-3 rounded-lg border transition-colors ${selectedContent?.id === content.id
                      ? 'border-primary bg-primary/10'
                      : 'border-border hover:border-border/80 hover:bg-muted/50'
                      }`}
                  >
                    <div className="flex items-center gap-3">
                      {content.isRequired ? (
                        <CheckCircle className="h-4 w-4 text-green-600 dark:text-green-400" />
                      ) : (
                        <Circle className="h-4 w-4 text-muted-foreground" />
                      )}
                      <div className="flex items-center gap-2">
                        {getLessonIcon(getLessonType(content.type || 0))}
                        <span className="text-sm font-medium text-foreground">
                          {content.title}
                        </span>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge className={getLessonTypeColor(getLessonType(content.type || 0))}>
                        {getContentTypeName(content.type || 0)}
                      </Badge>
                      {content.estimatedMinutes && (
                        <span className="text-xs text-muted-foreground">{content.estimatedMinutes}m</span>
                      )}
                      <ChevronRight className="h-4 w-4 text-muted-foreground" />
                    </div>
                  </button>
                </div>
              ))}
            </CardContent>
          </Card>
        </div>

        {/* Content Display */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                {selectedContent && getLessonIcon(getLessonType(selectedContent.type || 0))}
                {selectedContent?.title || 'Select content to begin'}
              </CardTitle>
              {selectedContent && (
                <div className="flex items-center gap-4 text-sm text-muted-foreground">
                  <Badge className={getLessonTypeColor(getLessonType(selectedContent.type || 0))}>
                    {getContentTypeName(selectedContent.type || 0)}
                  </Badge>
                  {selectedContent.estimatedMinutes && (
                    <span>{selectedContent.estimatedMinutes}m</span>
                  )}
                  {!selectedContent.isRequired && (
                    <Button
                      size="sm"
                      onClick={() => handleContentComplete(selectedContent.id!)}
                      className="ml-auto"
                    >
                      Mark as Complete
                    </Button>
                  )}
                </div>
              )}
            </CardHeader>
            <CardContent>
              {selectedContent?.body ? (
                <div className="prose max-w-none">
                  <MarkdownRenderer content={selectedContent.body} />
                </div>
              ) : (
                <div className="text-center py-12">
                  <BookOpen className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-foreground mb-2">
                    Select content to begin learning
                  </h3>
                  <p className="text-muted-foreground">
                    Choose content from the course to start your learning journey.
                  </p>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
