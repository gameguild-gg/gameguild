'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { getCourseBySlug } from '@/lib/courses/actions';
import { Course } from '@/components/legacy/types/courses';
import { CourseEditorProvider } from '@/lib/courses/course-editor.context';
import { CourseEditor } from '@/components/courses/course-editor/course-editor';
import { Skeleton } from '@/components/ui/skeleton';

export default function CourseDetailPage() {
  const params = useParams();
  const slug = params.slug as string;

  const [course, setCourse] = useState<Course | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchCourse = async () => {
      try {
        setLoading(true);
        setError(null);

        const courseData = await getCourseBySlug(slug);
        if (courseData) {
          setCourse(courseData);
        } else {
          setError('Course not found');
        }
      } catch (err) {
        console.error('Error fetching course:', err);
        setError('Failed to load course');
      } finally {
        setLoading(false);
      }
    };

    if (slug) {
      fetchCourse();
    }
  }, [slug]);

  if (loading) {
    return (
      <div className="min-h-screen bg-background">
        <div className="container mx-auto max-w-7xl p-6">
          <div className="space-y-8">
            <Skeleton className="h-16 w-full" />
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
              <div className="lg:col-span-2 space-y-8">
                <Skeleton className="h-96 w-full" />
                <Skeleton className="h-64 w-full" />
              </div>
              <div className="space-y-8">
                <Skeleton className="h-48 w-full" />
                <Skeleton className="h-32 w-full" />
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (error || !course) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-foreground mb-2">Course Not Found</h1>
          <p className="text-muted-foreground mb-4">{error || 'The requested course could not be found.'}</p>
          <a href="/dashboard/courses" className="text-primary hover:underline">
            Return to Courses
          </a>
        </div>
      </div>
    );
  }

  // Transform course data to match our editor format
  const initialCourseData = {
    id: course.id,
    title: course.title,
    slug: course.slug,
    description: course.description,
    summary: course.description.substring(0, 200), // Use first 200 chars as summary
    category: course.area,
    difficulty: course.level,
    estimatedHours: 10, // Default hours since not available in legacy type
    status: 'published' as const, // Default status
    media: {
      thumbnail: undefined, // Will be handled by the media section
      showcaseVideo: undefined
    },
    products: [],
    enrollment: {
      isOpen: true,
      currentEnrollments: 0
    },
    tags: Array.isArray(course.tools) ? course.tools : [],
    manualSlugEdit: true // Since it's an existing course
  };

  return (
    <CourseEditorProvider initialCourse={initialCourseData}>
      <CourseEditor courseSlug={slug} isCreating={false} />
    </CourseEditorProvider>
  );
}
        if (courseData?.id) {
          await fetchProgramContent(courseData.id);
        }
      } catch (err) {
        setError('Failed to load course data');
        console.error('Error fetching course:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchCourse();
  }, [slug]);

  const fetchProgramContent = async (programId: string) => {
    try {
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
      const response = await fetch(`${apiUrl}/api/program/${programId}/content`);
      
      if (response.ok) {
        const content = await response.json();
        setProgramContent(content);
      }
    } catch (err) {
      console.error('Error fetching program content:', err);
    }
  };

  const handleAddLesson = async () => {
    try {
      if (!course?.id) return;

      const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
      const response = await fetch(`${apiUrl}/api/program/${course.id}/content`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          title: lessonForm.title,
          description: lessonForm.description,
          contentType: lessonForm.contentType,
          duration: lessonForm.duration,
          isRequired: lessonForm.isRequired,
          sortOrder: programContent.length + 1,
        }),
      });

      if (response.ok) {
        // Refresh program content
        await fetchProgramContent(course.id);
        
        // Reset form
        setLessonForm({
          title: '',
          description: '',
          contentType: 'text',
          duration: 30,
          isRequired: true,
        });
        
        // Close dialog
        setIsAddLessonOpen(false);
      } else {
        console.error('Failed to add lesson');
      }
    } catch (err) {
      console.error('Error adding lesson:', err);
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="flex items-center justify-center min-h-[200px]">
          <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-primary"></div>
        </div>
      </div>
    );
  }

  if (error || !course) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-destructive mb-4">Error</h1>
          <p className="text-muted-foreground mb-4">{error || 'Course not found'}</p>
          <Button onClick={() => window.history.back()}>
            <ArrowLeft className="w-4 h-4 mr-2" />
            Go Back
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Header Bar */}
      <div className="bg-card border-b border-border">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Button variant="ghost" onClick={() => window.history.back()}>
                <ArrowLeft className="w-4 h-4 mr-2" />
                Back to Courses
              </Button>
              <div className="text-sm text-muted-foreground">
                {course.area.charAt(0).toUpperCase() + course.area.slice(1)} Course
              </div>
            </div>
            <Button className="bg-primary hover:bg-primary/90 text-primary-foreground">
              <Edit className="w-4 h-4 mr-2" />
              EDIT COURSE
            </Button>
          </div>
        </div>
      </div>

      {/* Course Stats Header */}
      <div className="bg-card border-b border-border">
        <div className="container mx-auto px-4 py-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h1 className="text-2xl font-bold text-foreground">{course.title}</h1>
              <p className="text-muted-foreground mt-1">{course.description}</p>
            </div>
          </div>

          {/* Stats Grid */}
          <div className="grid grid-cols-2 md:grid-cols-5 gap-6">
            <div className="text-center">
              <div className="text-3xl font-bold text-foreground">{programContent.length}</div>
              <div className="text-sm text-muted-foreground uppercase tracking-wide">Total Enrolled</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-foreground">
                {programContent.filter((c) => c.isRequired).length}
              </div>
              <div className="text-sm text-muted-foreground uppercase tracking-wide">Students Passed</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-foreground">
                {Math.floor(programContent.reduce((total, content) => total + (content.duration || 0), 0) / 60) || 12}
              </div>
              <div className="text-sm text-muted-foreground uppercase tracking-wide">In Progress</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-foreground">
                {Math.round((programContent.filter((c) => c.isRequired).length / Math.max(programContent.length, 1)) * 100)}%
              </div>
              <div className="text-sm text-muted-foreground uppercase tracking-wide">Average Score</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-foreground">
                {Math.floor(programContent.reduce((total, content) => total + (content.duration || 0), 0) / Math.max(programContent.length, 1)) || 2}:
                {String(Math.floor(Math.random() * 60)).padStart(2, '0')}
              </div>
              <div className="text-sm text-muted-foreground uppercase tracking-wide">Average Time Spent</div>
            </div>
          </div>
        </div>
      </div>

      <div className="container mx-auto px-4 py-8">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Course Content */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                Course Content
                <Dialog open={isAddLessonOpen} onOpenChange={setIsAddLessonOpen}>
                  <DialogTrigger asChild>
                    <Button variant="outline" size="sm">
                      <Plus className="w-4 h-4 mr-2" />
                      Add Lesson
                    </Button>
                  </DialogTrigger>
                  <DialogContent className="sm:max-w-[425px]">
                    <DialogHeader>
                      <DialogTitle>Add New Lesson</DialogTitle>
                      <DialogDescription>Create a new lesson for this course. Fill in the details below.</DialogDescription>
                    </DialogHeader>
                    <div className="grid gap-4 py-4">
                      <div className="grid grid-cols-4 items-center gap-4">
                        <Label htmlFor="title" className="text-right">
                          Title
                        </Label>
                        <Input
                          id="title"
                          value={lessonForm.title}
                          onChange={(e) => setLessonForm((prev) => ({ ...prev, title: e.target.value }))}
                          className="col-span-3"
                          placeholder="Enter lesson title"
                        />
                      </div>
                      <div className="grid grid-cols-4 items-center gap-4">
                        <Label htmlFor="description" className="text-right">
                          Description
                        </Label>
                        <Textarea
                          id="description"
                          value={lessonForm.description}
                          onChange={(e) => setLessonForm((prev) => ({ ...prev, description: e.target.value }))}
                          className="col-span-3"
                          placeholder="Enter lesson description"
                        />
                      </div>
                      <div className="grid grid-cols-4 items-center gap-4">
                        <Label htmlFor="contentType" className="text-right">
                          Content Type
                        </Label>
                        <Select
                          value={lessonForm.contentType}
                          onValueChange={(value: 'text' | 'video' | 'interactive' | 'assessment') => setLessonForm((prev) => ({ ...prev, contentType: value }))}
                        >
                          <SelectTrigger className="col-span-3">
                            <SelectValue placeholder="Select content type" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="text">Text</SelectItem>
                            <SelectItem value="video">Video</SelectItem>
                            <SelectItem value="interactive">Interactive</SelectItem>
                            <SelectItem value="assessment">Assessment</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="grid grid-cols-4 items-center gap-4">
                        <Label htmlFor="duration" className="text-right">
                          Duration (min)
                        </Label>
                        <Input
                          id="duration"
                          type="number"
                          value={lessonForm.duration}
                          onChange={(e) => setLessonForm((prev) => ({ ...prev, duration: parseInt(e.target.value) || 0 }))}
                          className="col-span-3"
                          placeholder="Duration in minutes"
                        />
                      </div>
                    </div>
                    <DialogFooter>
                      <Button type="submit" onClick={handleAddLesson}>
                        Add Lesson
                      </Button>
                    </DialogFooter>
                  </DialogContent>
                </Dialog>
              </CardTitle>
            </CardHeader>
            <CardContent>
              {programContent.length > 0 ? (
                <div className="space-y-3">
                  {programContent.map((content, index) => (
                    <div key={content.id} className="flex items-center justify-between p-3 border border-border rounded-lg bg-card">
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center text-sm font-medium text-primary">
                          {index + 1}
                        </div>
                        <div>
                          <h3 className="font-medium text-foreground">{content.title}</h3>
                          <p className="text-sm text-muted-foreground">{content.description}</p>
                          <div className="flex items-center gap-2 mt-1">
                            <Badge variant="outline" className="text-xs">
                              {content.contentType}
                            </Badge>
                            {content.duration && <span className="text-xs text-muted-foreground">{content.duration} min</span>}
                          </div>
                        </div>
                      </div>
                      <div className="flex gap-2">
                        <Button variant="ghost" size="sm">
                          <Edit className="w-4 h-4" />
                        </Button>
                        <Button variant="ghost" size="sm">
                          <Eye className="w-4 h-4" />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-8">
                  <BookOpen className="w-12 h-12 mx-auto text-muted-foreground mb-4" />
                  <h3 className="font-medium text-foreground mb-2">No lessons yet</h3>
                  <p className="text-muted-foreground mb-4">Start building your course by adding some lessons.</p>
                  <Dialog open={isAddLessonOpen} onOpenChange={setIsAddLessonOpen}>
                    <DialogTrigger asChild>
                      <Button>
                        <Plus className="w-4 h-4 mr-2" />
                        Add First Lesson
                      </Button>
                    </DialogTrigger>
                  </Dialog>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Course Stats */}
          <Card>
            <CardHeader>
              <CardTitle>Course Statistics</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div className="text-center">
                  <div className="text-2xl font-bold text-primary">{course.enrollmentCount || 0}</div>
                  <div className="text-sm text-muted-foreground">Students</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-green-600 dark:text-green-400">{course.analytics?.completions || 0}</div>
                  <div className="text-sm text-muted-foreground">Completions</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-yellow-600 dark:text-yellow-400">{course.analytics?.averageRating || 0}</div>
                  <div className="text-sm text-muted-foreground">Rating</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-purple-600 dark:text-purple-400">{programContent.length}</div>
                  <div className="text-sm text-muted-foreground">Lessons</div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Course Info */}
          <Card>
            <CardHeader>
              <CardTitle>Course Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium text-foreground">Status</label>
                <div className="mt-1">
                  <Badge variant={course.status === 'published' ? 'default' : 'secondary'}>
                    {course.status}
                  </Badge>
                </div>
              </div>
              <div>
                <label className="text-sm font-medium text-foreground">Created</label>
                <div className="mt-1 text-sm text-muted-foreground">
                  {new Date(course.publishedAt || '').toLocaleDateString()}
                </div>
              </div>
              <div>
                <label className="text-sm font-medium text-foreground">Last Updated</label>
                <div className="mt-1 text-sm text-muted-foreground">
                  {new Date(course.updatedAt || '').toLocaleDateString()}
                </div>
              </div>
              <div>
                <label className="text-sm font-medium text-foreground">Estimated Duration</label>
                <div className="mt-1 text-sm text-muted-foreground">
                  {course.estimatedHours || 10} hours
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Quick Actions */}
          <Card>
            <CardHeader>
              <CardTitle>Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Dialog open={isAddLessonOpen} onOpenChange={setIsAddLessonOpen}>
                <DialogTrigger asChild>
                  <Button variant="outline" className="w-full justify-start">
                    <BookOpen className="w-4 h-4 mr-2" />
                    Add Lesson
                  </Button>
                </DialogTrigger>
              </Dialog>
              <Button variant="outline" className="w-full justify-start">
                <Users className="w-4 h-4 mr-2" />
                Manage Students
              </Button>
              <Button variant="outline" className="w-full justify-start">
                <Eye className="w-4 h-4 mr-2" />
                Preview Course
              </Button>
              <Button variant="outline" className="w-full justify-start text-red-600 hover:text-red-700">
                <span className="w-4 h-4 mr-2">🗑️</span>
                Delete Course
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
