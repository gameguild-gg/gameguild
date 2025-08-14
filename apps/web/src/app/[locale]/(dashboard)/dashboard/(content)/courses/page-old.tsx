import React from 'react';
import { Search, Filter, Grid3X3, List, BookOpen } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { getPrograms as getCourses } from '@/lib/content-management/programs/programs.actions';
import CourseListWrapper from '@/components/courses/course-list-wrapper';

export default async function Page(): Promise<React.JSX.Element> {
  let courses: any[] = [];
  let error: string | null = null;

  try {
    const response = await getCourses();
    // Extract only the serializable data and ensure it's a plain object
    const rawCourses = response.data || [];
    // Serialize and deserialize to ensure we have plain objects
    courses = JSON.parse(JSON.stringify(rawCourses));
  } catch (err) {
    console.error('Error loading courses:', err);
    error = err instanceof Error ? err.message : 'Failed to load courses';
    
    // Fallback to mock data for development/demonstration purposes
    courses = [
      {
        id: '1',
        title: 'Introduction to Game Programming',
        description: 'Learn the fundamentals of game programming with C# and Unity.',
        area: 'programming',
        level: 1,
        tools: ['Unity', 'C#', 'Visual Studio'],
        progress: 75,
        image: '/placeholder.svg',
        slug: 'intro-game-programming'
      },
      {
        id: '2', 
        title: '2D Game Art Creation',
        description: 'Master 2D sprite creation and animation for indie games.',
        area: 'art',
        level: 2,
        tools: ['Photoshop', 'Aseprite', 'Illustrator'],
        progress: 45,
        image: '/placeholder.svg',
        slug: '2d-game-art'
      },
      {
        id: '3',
        title: 'Game Audio Design',
        description: 'Create immersive soundscapes and effects for games.',
        area: 'audio',
        level: 2,
        tools: ['Wwise', 'Reaper', 'Audacity'],
        image: '/placeholder.svg',
        slug: 'game-audio-design'
      }
    ];
  }

    return (
      <div className="flex flex-col min-h-svh bg-background">
        <div className="border-b border-border/50 bg-card/30 backdrop-blur supports-[backdrop-filter]:bg-card/30">
          <div className="flex items-center justify-between p-6">
            <div>
              <h1 className="text-2xl font-semibold text-foreground">Courses</h1>
              <p className="text-sm text-muted-foreground">Manage your educational content</p>
            </div>
            {/* Placeholder for create drawer similar to pm-dashboard */}
            <Button variant="secondary" size="sm" disabled>
              Create Course
            </Button>
          </div>
          <div className="flex items-center justify-between px-6 pb-4">
            <div className="flex items-center gap-4">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input placeholder="Search courses..." className="pl-10 w-80 bg-background/50 border-border/50" />
              </div>
              <Select defaultValue="all">
                <SelectTrigger className="w-40 bg-background/50 border-border/50">
                  <Filter className="h-4 w-4 mr-2" />
                  <SelectValue placeholder="All Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Status</SelectItem>
                  <SelectItem value="published">Published</SelectItem>
                  <SelectItem value="draft">Draft</SelectItem>
                  <SelectItem value="archived">Archived</SelectItem>
                </SelectContent>
              </Select>
              <Select defaultValue="all">
                <SelectTrigger className="w-40 bg-background/50 border-border/50">
                  <SelectValue placeholder="All Levels" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Levels</SelectItem>
                  <SelectItem value="beginner">Beginner</SelectItem>
                  <SelectItem value="intermediate">Intermediate</SelectItem>
                  <SelectItem value="advanced">Advanced</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="flex items-center gap-2">
              <div className="flex items-center border border-border/50 rounded-md">
                <Button variant="secondary" size="sm" className="rounded-r-none" disabled>
                  <Grid3X3 className="h-4 w-4" />
                </Button>
                <Button variant="ghost" size="sm" className="rounded-l-none" disabled>
                  <List className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>
        </div>
        <main className="flex-1 p-6">
          {error && (
            <div className="mb-4 p-4 bg-yellow-50 border border-yellow-200 rounded-md">
              <p className="text-yellow-800 text-sm">
                <strong>Warning:</strong> Using mock data - {error}
              </p>
            </div>
          )}
          {courses?.length ? (
            <CourseListWrapper courses={courses} />
          ) : (
            <div className="flex flex-col items-center justify-center text-center py-20">
              <div className="p-4 bg-muted/20 rounded-full mb-6">
                <BookOpen className="h-12 w-12 text-muted-foreground" />
              </div>
              <h3 className="text-xl font-semibold mb-2">No courses found</h3>
              <p className="text-muted-foreground mb-6 max-w-md">Create your first course to get started</p>
              <Button variant="secondary" disabled>
                Create Course
              </Button>
            </div>
          )}
        </main>
      </div>
    );
}
