'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { getProgramBySlug } from '@/data/courses/mock-data';
import { Program, ProgramContent } from '@/lib/api/generated';
import { BookOpen } from 'lucide-react';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';





export default function CourseContentPage() {
  const params = useParams();
  const router = useRouter();
  const slug = params.slug as string;
  const [program, setProgram] = useState<Program | null>(null);

  useEffect(() => {
    console.log('CourseContentPage - slug:', slug);
    const courseData = getProgramBySlug(slug);
    console.log('CourseContentPage - program found:', courseData);
    setProgram(courseData);
  }, [slug]);

  // Redirect to first content item when program is loaded
  useEffect(() => {
    if (program && program.programContents && program.programContents.length > 0) {
      const firstContent = program.programContents[0];
      if (firstContent) {
        const firstContentSlug = firstContent.slug;
        router.replace(`/courses/${slug}/content/${firstContentSlug}`);
      }
    }
  }, [program, slug, router]);

  const totalContent = program?.programContents?.length || 0;
  const completedContent = program?.programContents?.filter(content => content.isRequired)?.length || 0;
  const progressPercentage = totalContent > 0 ? (completedContent / totalContent) * 100 : 0;

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
    <div className="space-y-8">
      {/* Course Header */}
      <div>
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

      {/* Welcome Message */}
      <Card>
        <CardContent className="pt-6">
          <div className="text-center py-12">
            <BookOpen className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
            <h3 className="text-lg font-medium text-foreground mb-2">
              Select content to begin learning
            </h3>
            <p className="text-muted-foreground">
              Choose content from the sidebar to start your learning journey.
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}