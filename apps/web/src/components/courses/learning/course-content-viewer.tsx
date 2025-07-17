import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Button } from '@game-guild/ui/components/button';
import { Progress } from '@game-guild/ui/components/progress';
import { Badge } from '@game-guild/ui/components/badge';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@game-guild/ui/components/dropdown-menu';
// import { Tabs, TabsContent, TabsList, TabsTrigger } from '@game-guild/ui/components/tabs';
import { ChevronLeft, ChevronRight, BookOpen, CheckCircle, Clock, Play, FileText, Upload, MessageSquare, Trophy, Flag, MoreVertical } from 'lucide-react';
import { ContentNavigationSidebar } from './content-navigation-sidebar';
import { LessonViewer } from './lesson-viewer';
import { ActivityComponent } from './activity-component';
import { ReportContentDialog } from './report-content-dialog';
import { CertificateNotification } from './certificate-notification';
import { ContentReportService } from '@/lib/courses/services/content-report.service';
import { CourseCompletionCertificateService } from '@/lib/courses/services/certificate.service';

interface ContentItem {
  id: string;
  title: string;
  type: 'lesson' | 'activity' | 'quiz' | 'assignment' | 'peer-review';
  status: 'locked' | 'available' | 'in-progress' | 'completed';
  duration?: number; // in minutes
  description?: string;
  order: number;
  isRequired: boolean;
  activityType?: 'text' | 'code' | 'file' | 'quiz' | 'discussion';
  content?: any;
  progress?: number;
  score?: number;
  maxScore?: number;
}

interface Module {
  id: string;
  title: string;
  description: string;
  order: number;
  items: ContentItem[];
  isLocked: boolean;
  progress: number;
}

interface CourseData {
  id: string;
  title: string;
  description: string;
  modules: Module[];
  overallProgress: number;
  totalItems: number;
  completedItems: number;
  currentItem?: ContentItem;
  estimatedTimeToComplete: number;
}

interface CourseContentViewerProps {
  courseSlug: string;
}

export function CourseContentViewer({ courseSlug }: CourseContentViewerProps) {
  const [courseData, setCourseData] = useState<CourseData | null>(null);
  const [currentItem, setCurrentItem] = useState<ContentItem | null>(null);
  const [loading, setLoading] = useState(true);
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [showReportDialog, setShowReportDialog] = useState(false);

  const loadCourseData = useCallback(async () => {
    try {
      setLoading(true);

      // Mock course data - replace with actual API call
      const mockData: CourseData = {
        id: courseSlug,
        title: 'Game Development Fundamentals',
        description: 'Learn the essential concepts and skills needed for game development',
        overallProgress: 35,
        totalItems: 12,
        completedItems: 4,
        estimatedTimeToComplete: 8.5,
        modules: [
          {
            id: 'module-1',
            title: 'Introduction to Game Development',
            description: 'Basic concepts and setup',
            order: 1,
            isLocked: false,
            progress: 100,
            items: [
              {
                id: 'lesson-1',
                title: 'What is Game Development?',
                type: 'lesson',
                status: 'completed',
                duration: 15,
                order: 1,
                isRequired: true,
                description: 'Overview of game development process and roles',
              },
              {
                id: 'activity-1',
                title: 'Development Environment Setup',
                type: 'activity',
                status: 'completed',
                duration: 30,
                order: 2,
                isRequired: true,
                activityType: 'text',
                description: 'Set up your development tools and workspace',
              },
            ],
          },
          {
            id: 'module-2',
            title: 'Programming Fundamentals',
            description: 'Core programming concepts for games',
            order: 2,
            isLocked: false,
            progress: 50,
            items: [
              {
                id: 'lesson-2',
                title: 'Game Programming Basics',
                type: 'lesson',
                status: 'completed',
                duration: 25,
                order: 1,
                isRequired: true,
                description: 'Variables, functions, and game loops',
              },
              {
                id: 'activity-2',
                title: 'Write Your First Game Script',
                type: 'activity',
                status: 'in-progress',
                duration: 45,
                order: 2,
                isRequired: true,
                activityType: 'code',
                description: 'Create a simple player movement script',
                progress: 60,
              },
              {
                id: 'quiz-1',
                title: 'Programming Concepts Quiz',
                type: 'quiz',
                status: 'available',
                duration: 15,
                order: 3,
                isRequired: true,
                activityType: 'quiz',
                description: 'Test your understanding of basic programming',
              },
            ],
          },
          {
            id: 'module-3',
            title: 'Game Design Principles',
            description: 'Learn core game design concepts',
            order: 3,
            isLocked: false,
            progress: 0,
            items: [
              {
                id: 'lesson-3',
                title: 'Game Mechanics and Systems',
                type: 'lesson',
                status: 'available',
                duration: 30,
                order: 1,
                isRequired: true,
                description: 'Understanding game mechanics and how they work together',
              },
              {
                id: 'assignment-1',
                title: 'Design Document Creation',
                type: 'assignment',
                status: 'locked',
                duration: 120,
                order: 2,
                isRequired: true,
                activityType: 'file',
                description: 'Create a game design document for your first project',
              },
            ],
          },
        ],
      };

      // Find the current item (first available/in-progress item)
      let foundCurrentItem = null;
      for (const moduleItem of mockData.modules) {
        for (const item of moduleItem.items) {
          if (item.status === 'in-progress' || (item.status === 'available' && !foundCurrentItem)) {
            foundCurrentItem = item;
            break;
          }
        }
        if (foundCurrentItem) break;
      }

      mockData.currentItem = foundCurrentItem || undefined;
      setCourseData(mockData);
      setCurrentItem(foundCurrentItem);
    } catch (error) {
      console.error('Error loading course data:', error);
    } finally {
      setLoading(false);
    }
  }, [courseSlug]);

  useEffect(() => {
    loadCourseData();
  }, [loadCourseData]);

  const handleItemSelect = (item: ContentItem) => {
    if (item.status === 'locked') return;
    setCurrentItem(item);
  };

  const handleItemComplete = async (itemId: string, score?: number) => {
    if (!courseData) return;

    // Update the item status and course progress
    const updatedCourseData = { ...courseData };
    let itemFound = false;

    for (const module of updatedCourseData.modules) {
      for (const item of module.items) {
        if (item.id === itemId) {
          item.status = 'completed';
          item.progress = 100;
          if (score !== undefined) {
            item.score = score;
          }
          itemFound = true;
          break;
        }
      }
      if (itemFound) break;
    }

    // Recalculate progress
    const totalItems = updatedCourseData.modules.reduce((sum, module) => sum + module.items.length, 0);
    const completedItems = updatedCourseData.modules.reduce((sum, module) => sum + module.items.filter((item) => item.status === 'completed').length, 0);

    updatedCourseData.overallProgress = Math.round((completedItems / totalItems) * 100);
    updatedCourseData.completedItems = completedItems;

    // Update module progress
    for (const module of updatedCourseData.modules) {
      const moduleCompleted = module.items.filter((item) => item.status === 'completed').length;
      module.progress = Math.round((moduleCompleted / module.items.length) * 100);
    }

    setCourseData(updatedCourseData);

    // Move to next available item
    const nextItem = findNextAvailableItem(updatedCourseData, itemId);
    if (nextItem) {
      setCurrentItem(nextItem);
    }
  };

  const findNextAvailableItem = (data: CourseData, currentItemId: string): ContentItem | null => {
    let foundCurrent = false;

    for (const module of data.modules) {
      for (const item of module.items) {
        if (foundCurrent && (item.status === 'available' || item.status === 'in-progress')) {
          return item;
        }
        if (item.id === currentItemId) {
          foundCurrent = true;
        }
      }
    }

    return null;
  };

  const navigateToItem = (direction: 'prev' | 'next') => {
    if (!courseData || !currentItem) return;

    const allItems: ContentItem[] = [];
    courseData.modules.forEach((moduleItem) => {
      allItems.push(...moduleItem.items);
    });

    const currentIndex = allItems.findIndex((item) => item.id === currentItem.id);

    if (direction === 'prev' && currentIndex > 0) {
      const prevItem = allItems[currentIndex - 1];
      if (prevItem.status !== 'locked') {
        setCurrentItem(prevItem);
      }
    } else if (direction === 'next' && currentIndex < allItems.length - 1) {
      const nextItem = allItems[currentIndex + 1];
      if (nextItem.status !== 'locked') {
        setCurrentItem(nextItem);
      }
    }
  };

  const handleReportContent = async (reason: string, description: string) => {
    try {
      if (!currentItem) return;

      const report = await ContentReportService.createReport({
        contentId: currentItem.id,
        contentTitle: currentItem.title,
        reportType: reason,
        description,
      });

      console.log('Content reported successfully:', report);
      setShowReportDialog(false);
      // TODO: Show success notification
    } catch (error) {
      console.error('Failed to report content:', error);
      // TODO: Show error notification
    }
  };

  const getContentIcon = (type: string) => {
    switch (type) {
      case 'lesson':
        return <BookOpen className="h-4 w-4" />;
      case 'activity':
        return <Play className="h-4 w-4" />;
      case 'quiz':
        return <FileText className="h-4 w-4" />;
      case 'assignment':
        return <Upload className="h-4 w-4" />;
      case 'peer-review':
        return <MessageSquare className="h-4 w-4" />;
      default:
        return <BookOpen className="h-4 w-4" />;
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-950 text-white">
        <div className="container mx-auto px-4 py-8">
          <div className="flex items-center justify-center h-64">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
          </div>
        </div>
      </div>
    );
  }

  if (!courseData) {
    return (
      <div className="min-h-screen bg-gray-950 text-white">
        <div className="container mx-auto px-4 py-8">
          <Card className="bg-gray-800 border-gray-700">
            <CardContent className="p-6">
              <p className="text-center text-gray-400">Course not found</p>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-950 text-white">
      <div className="border-b border-gray-800 bg-gray-900">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold">{courseData.title}</h1>
              <div className="flex items-center gap-4 mt-2 text-sm text-gray-400">
                <span className="flex items-center gap-1">
                  <CheckCircle className="h-4 w-4" />
                  {courseData.completedItems}/{courseData.totalItems} Complete
                </span>
                <span className="flex items-center gap-1">
                  <Clock className="h-4 w-4" />
                  {courseData.estimatedTimeToComplete}h remaining
                </span>
                <span className="flex items-center gap-1">
                  <Trophy className="h-4 w-4" />
                  {courseData.overallProgress}% Progress
                </span>
              </div>
            </div>
            <Button variant="outline" size="sm" onClick={() => setSidebarOpen(!sidebarOpen)} className="border-gray-600">
              {sidebarOpen ? 'Hide' : 'Show'} Contents
            </Button>
          </div>
          <Progress value={courseData.overallProgress} className="mt-4 h-2" />
        </div>
      </div>

      <div className="flex">
        {sidebarOpen && (
          <ContentNavigationSidebar
            courseId={courseData.id}
            modules={courseData.modules.map((module) => ({
              ...module,
              contentItems: module.items,
            }))}
            currentContentId={currentItem?.id}
            onContentSelect={(contentId: string) => {
              // Find the item by id across all modules
              const foundItem = courseData.modules.flatMap((module) => module.items).find((item) => item.id === contentId);
              if (foundItem) {
                handleItemSelect(foundItem);
              }
            }}
          />
        )}

        <main className={`flex-1 ${sidebarOpen ? 'ml-80' : ''}`}>
          <div className="container mx-auto px-4 py-8">
            {currentItem && (
              <>
                {/* Navigation Controls */}
                <div className="flex items-center justify-between mb-6">
                  <Button variant="outline" size="sm" onClick={() => navigateToItem('prev')} className="border-gray-600">
                    <ChevronLeft className="h-4 w-4 mr-1" />
                    Previous
                  </Button>

                  <div className="flex items-center gap-2">
                    {getContentIcon(currentItem.type)}
                    <Badge variant="outline" className="border-gray-600">
                      {currentItem.type.charAt(0).toUpperCase() + currentItem.type.slice(1)}
                    </Badge>
                    {currentItem.isRequired && <Badge variant="secondary">Required</Badge>}
                  </div>

                  <Button variant="outline" size="sm" onClick={() => navigateToItem('next')} className="border-gray-600">
                    Next
                    <ChevronRight className="h-4 w-4 ml-1" />
                  </Button>
                </div>

                {/* Content Area */}
                <Card className="bg-gray-800 border-gray-700">
                  <CardHeader>
                    <CardTitle className="flex items-center justify-between">
                      <span>{currentItem.title}</span>
                      <div className="flex items-center gap-2">
                        {currentItem.duration && (
                          <span className="text-sm font-normal text-gray-400 flex items-center gap-1">
                            <Clock className="h-4 w-4" />
                            {currentItem.duration} min
                          </span>
                        )}
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="sm">
                              <MoreVertical className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={() => setShowReportDialog(true)}>
                              <Flag className="h-4 w-4 mr-2" />
                              Report Content
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>
                    </CardTitle>
                    {currentItem.description && <p className="text-gray-400">{currentItem.description}</p>}
                  </CardHeader>
                  <CardContent>
                    {currentItem.type === 'lesson' ? (
                      <LessonViewer item={currentItem} onComplete={() => handleItemComplete(currentItem.id)} />
                    ) : (
                      <ActivityComponent item={currentItem} onComplete={(score: number) => handleItemComplete(currentItem.id, score)} />
                    )}
                  </CardContent>
                </Card>

                {/* Report Content Dialog */}
                <ReportContentDialog
                  open={showReportDialog}
                  onOpenChange={setShowReportDialog}
                  contentId={currentItem.id}
                  contentTitle={currentItem.title}
                  onSubmit={handleReportContent}
                />
              </>
            )}
          </div>
        </main>
      </div>
    </div>
  );
}
