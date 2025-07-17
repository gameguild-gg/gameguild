'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { ScrollArea } from '@/components/ui/scroll-area';
import { AlertTriangle, ArrowLeft, ArrowRight, BookOpen, CheckCircle2, Clock, Code, FileText, MessageSquare, PlayCircle, Trophy, Users } from 'lucide-react';

interface ContentItem {
  id: string;
  title: string;
  type: 'page' | 'video' | 'assignment' | 'quiz' | 'discussion' | 'code' | 'project';
  duration: number; // in minutes
  isRequired: boolean;
  isCompleted: boolean;
  isLocked: boolean;
  progress: number; // 0-100
  description?: string;
  score?: number;
  maxScore?: number;
}

interface CourseContent {
  id: string;
  title: string;
  description: string;
  estimatedHours: number;
  instructor: string;
  progress: number;
  isEnrolled: boolean;
  certificate: boolean;
  contents: ContentItem[];
}

interface CourseViewerProps {
  courseId: string;
  initialCourse: CourseContent;
}

const ContentTypeIcon = ({ type }: { type: ContentItem['type'] }) => {
  const iconProps = { className: 'h-4 w-4' };

  switch (type) {
    case 'page':
      return <FileText {...iconProps} />;
    case 'video':
      return <PlayCircle {...iconProps} />;
    case 'assignment':
      return <BookOpen {...iconProps} />;
    case 'quiz':
      return <MessageSquare {...iconProps} />;
    case 'discussion':
      return <Users {...iconProps} />;
    case 'code':
      return <Code {...iconProps} />;
    case 'project':
      return <Trophy {...iconProps} />;
    default:
      return <FileText {...iconProps} />;
  }
};

export function CourseViewer({ courseId, initialCourse }: CourseViewerProps) {
  const router = useRouter();
  const [course, setCourse] = useState<CourseContent>(initialCourse);
  const [currentContentIndex, setCurrentContentIndex] = useState(0);
  const [isLoading, setIsLoading] = useState(false);

  const currentContent = course.contents[currentContentIndex];
  const canGoNext = currentContentIndex < course.contents.length - 1;
  const canGoPrevious = currentContentIndex > 0;

  // Find next available content (not locked)
  const getNextAvailableIndex = () => {
    for (let i = currentContentIndex + 1; i < course.contents.length; i++) {
      if (!course.contents[i].isLocked) {
        return i;
      }
    }
    return -1;
  };

  const handleEnroll = async () => {
    setIsLoading(true);
    try {
      // TODO: Implement GraphQL mutation for enrollment
      console.log('Enrolling in course:', courseId);

      // Simulate API call
      await new Promise((resolve) => setTimeout(resolve, 1000));

      setCourse((prev) => ({ ...prev, isEnrolled: true }));
    } catch (error) {
      console.error('Failed to enroll:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleContentComplete = async (contentId: string) => {
    setIsLoading(true);
    try {
      // TODO: Implement GraphQL mutation for content completion
      console.log('Marking content as complete:', contentId);

      // Simulate API call
      await new Promise((resolve) => setTimeout(resolve, 500));

      setCourse((prev) => ({
        ...prev,
        contents: prev.contents.map((content) => (content.id === contentId ? { ...content, isCompleted: true, progress: 100 } : content)),
      }));

      // Update overall course progress
      const completedCount = course.contents.filter((c) => c.isCompleted).length + 1;
      const newProgress = (completedCount / course.contents.length) * 100;

      setCourse((prev) => ({ ...prev, progress: newProgress }));
    } catch (error) {
      console.error('Failed to complete content:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handlePrevious = () => {
    if (canGoPrevious) {
      setCurrentContentIndex(currentContentIndex - 1);
    }
  };

  const handleNext = () => {
    const nextIndex = getNextAvailableIndex();
    if (nextIndex !== -1) {
      setCurrentContentIndex(nextIndex);
    }
  };

  const reportContent = () => {
    // TODO: Implement reporting functionality
    console.log('Reporting content:', currentContent.id);
  };

  if (!course.isEnrolled) {
    return (
      <div className="min-h-screen bg-gray-950 text-white">
        <div className="container mx-auto px-4 py-8">
          <div className="max-w-4xl mx-auto">
            <Card className="bg-gray-900 border-gray-800">
              <CardHeader>
                <CardTitle className="text-2xl">{course.title}</CardTitle>
                <p className="text-gray-400">{course.description}</p>
                <div className="flex items-center gap-4 text-sm text-gray-400">
                  <span className="flex items-center gap-1">
                    <Clock className="h-4 w-4" />
                    {course.estimatedHours} hours
                  </span>
                  <span className="flex items-center gap-1">
                    <Users className="h-4 w-4" />
                    {course.instructor}
                  </span>
                  {course.certificate && (
                    <Badge variant="secondary" className="flex items-center gap-1">
                      <Trophy className="h-3 w-3" />
                      Certificate
                    </Badge>
                  )}
                </div>
              </CardHeader>
              <CardContent className="space-y-6">
                <div>
                  <h3 className="text-lg font-semibold mb-4">Course Content</h3>
                  <div className="space-y-2">
                    {course.contents.map((content, index) => (
                      <div key={content.id} className="flex items-center gap-3 p-3 rounded-lg bg-gray-800/50">
                        <ContentTypeIcon type={content.type} />
                        <div className="flex-1">
                          <div className="font-medium">{content.title}</div>
                          <div className="text-sm text-gray-400">
                            {content.duration} min {content.isRequired && '• Required'}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <Button onClick={handleEnroll} disabled={isLoading} className="w-full" size="lg">
                  {isLoading ? 'Enrolling...' : 'Enroll in Course'}
                </Button>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-950 text-white">
      <div className="container mx-auto px-4 py-8">
        <div className="grid lg:grid-cols-4 gap-8">
          {/* Course Content Sidebar */}
          <div className="lg:col-span-1">
            <Card className="bg-gray-900 border-gray-800 sticky top-8">
              <CardHeader>
                <CardTitle className="text-lg">{course.title}</CardTitle>
                <div className="space-y-2">
                  <Progress value={course.progress} className="h-2" />
                  <div className="text-sm text-gray-400">{Math.round(course.progress)}% complete</div>
                </div>
              </CardHeader>
              <CardContent>
                <ScrollArea className="h-96">
                  <div className="space-y-2">
                    {course.contents.map((content, index) => (
                      <div
                        key={content.id}
                        className={`p-3 rounded-lg cursor-pointer transition-colors ${
                          index === currentContentIndex ? 'bg-blue-600' : content.isLocked ? 'bg-gray-800/30 opacity-50' : 'bg-gray-800/50 hover:bg-gray-800'
                        }`}
                        onClick={() => !content.isLocked && setCurrentContentIndex(index)}
                      >
                        <div className="flex items-center gap-2">
                          <ContentTypeIcon type={content.type} />
                          {content.isCompleted && <CheckCircle2 className="h-4 w-4 text-green-500" />}
                          {content.isLocked && <div className="h-4 w-4 rounded-full bg-gray-600" />}
                        </div>
                        <div className="mt-2">
                          <div className="font-medium text-sm">{content.title}</div>
                          <div className="text-xs text-gray-400">{content.duration} min</div>
                          {content.progress > 0 && content.progress < 100 && <Progress value={content.progress} className="h-1 mt-1" />}
                        </div>
                      </div>
                    ))}
                  </div>
                </ScrollArea>
              </CardContent>
            </Card>
          </div>

          {/* Main Content Area */}
          <div className="lg:col-span-3">
            <Card className="bg-gray-900 border-gray-800">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="flex items-center gap-2">
                      <ContentTypeIcon type={currentContent.type} />
                      {currentContent.title}
                    </CardTitle>
                    <div className="flex items-center gap-4 text-sm text-gray-400 mt-2">
                      <span>{currentContent.duration} minutes</span>
                      {currentContent.isRequired && <Badge variant="outline">Required</Badge>}
                      {currentContent.score !== undefined && (
                        <span>
                          Score: {currentContent.score}/{currentContent.maxScore}
                        </span>
                      )}
                    </div>
                  </div>
                  <Button variant="ghost" size="sm" onClick={reportContent} className="text-gray-400 hover:text-white">
                    <AlertTriangle className="h-4 w-4" />
                    Report
                  </Button>
                </div>
              </CardHeader>
              <CardContent className="space-y-6">
                {/* Content Display Area */}
                <div className="min-h-96 bg-gray-800/50 rounded-lg p-6">
                  {currentContent.type === 'video' && (
                    <div className="aspect-video bg-black rounded-lg flex items-center justify-center">
                      <PlayCircle className="h-16 w-16 text-gray-400" />
                    </div>
                  )}

                  {currentContent.type === 'page' && (
                    <div className="prose prose-invert max-w-none">
                      <h3>Content: {currentContent.title}</h3>
                      <p>{currentContent.description}</p>
                      {/* Content would be rendered here */}
                    </div>
                  )}

                  {currentContent.type === 'assignment' && (
                    <div className="space-y-4">
                      <h3 className="text-xl font-semibold">Assignment: {currentContent.title}</h3>
                      <p className="text-gray-300">{currentContent.description}</p>
                      <div className="bg-gray-800 p-4 rounded-lg">
                        <h4 className="font-medium mb-2">Instructions:</h4>
                        <p className="text-sm text-gray-300">Complete the assignment and submit your work for review.</p>
                      </div>
                    </div>
                  )}
                </div>

                {/* Action Buttons */}
                <div className="flex items-center justify-between">
                  <Button variant="outline" onClick={handlePrevious} disabled={!canGoPrevious} className="flex items-center gap-2">
                    <ArrowLeft className="h-4 w-4" />
                    Previous
                  </Button>

                  <div className="flex items-center gap-4">
                    {!currentContent.isCompleted && (
                      <Button onClick={() => handleContentComplete(currentContent.id)} disabled={isLoading} className="flex items-center gap-2">
                        <CheckCircle2 className="h-4 w-4" />
                        Mark Complete
                      </Button>
                    )}

                    {currentContent.isCompleted && (
                      <Badge variant="secondary" className="flex items-center gap-2">
                        <CheckCircle2 className="h-4 w-4" />
                        Completed
                      </Badge>
                    )}
                  </div>

                  <Button onClick={handleNext} disabled={getNextAvailableIndex() === -1} className="flex items-center gap-2">
                    Next
                    <ArrowRight className="h-4 w-4" />
                  </Button>
                </div>

                {/* Course Completion */}
                {course.progress === 100 && (
                  <Card className="bg-green-900/20 border-green-800">
                    <CardContent className="pt-6">
                      <div className="text-center space-y-4">
                        <Trophy className="h-12 w-12 text-yellow-500 mx-auto" />
                        <div>
                          <h3 className="text-xl font-semibold text-green-400">Congratulations!</h3>
                          <p className="text-gray-300">
                            You have completed this course.
                            {course.certificate && ' Your certificate is now available!'}
                          </p>
                        </div>
                        {course.certificate && (
                          <Button className="bg-yellow-600 hover:bg-yellow-700">
                            <Trophy className="h-4 w-4 mr-2" />
                            Download Certificate
                          </Button>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
