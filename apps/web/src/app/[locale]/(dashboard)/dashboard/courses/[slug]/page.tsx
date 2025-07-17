'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { getCourseBySlug } from '@/lib/courses/actions';
import { Course } from '@/components/legacy/types/courses';
import { ArrowLeft, Edit, Eye, Users, Clock, BookOpen } from 'lucide-react';
import Link from 'next/link';

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
        setCourse(courseData);
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

  const getLevelColor = (level: number) => {
    switch (level) {
      case 1:
        return 'bg-green-100 text-green-800';
      case 2:
        return 'bg-yellow-100 text-yellow-800';
      case 3:
        return 'bg-red-100 text-red-800';
      case 4:
        return 'bg-purple-100 text-purple-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getLevelText = (level: number) => {
    switch (level) {
      case 1:
        return 'Beginner';
      case 2:
        return 'Intermediate';
      case 3:
        return 'Advanced';
      case 4:
        return 'Arcane';
      default:
        return 'Unknown';
    }
  };

  const getAreaColor = (area: string) => {
    switch (area) {
      case 'programming':
        return 'bg-blue-100 text-blue-800';
      case 'art':
        return 'bg-pink-100 text-pink-800';
      case 'design':
        return 'bg-purple-100 text-purple-800';
      case 'audio':
        return 'bg-orange-100 text-orange-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (loading) {
    return (
      <div className="flex flex-col flex-1 items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
          <p className="mt-4 text-muted-foreground">Loading course...</p>
        </div>
      </div>
    );
  }

  if (error || !course) {
    return (
      <div className="flex flex-col flex-1 items-center justify-center">
        <div className="text-center">
          <p className="text-red-500 mb-4">{error || 'Course not found'}</p>
          <Link href="/dashboard/courses">
            <Button variant="outline">Back to Courses</Button>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col flex-1 p-6 max-w-6xl mx-auto w-full">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-4">
          <Link href="/dashboard/courses">
            <Button variant="ghost" size="sm">
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Courses
            </Button>
          </Link>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">{course.title}</h1>
            <p className="text-muted-foreground">Course Management</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Link href={`/courses/${course.slug}`}>
            <Button variant="outline">
              <Eye className="w-4 h-4 mr-2" />
              View Public
            </Button>
          </Link>
          <Link href={`/dashboard/courses/${course.slug}/edit`}>
            <Button>
              <Edit className="w-4 h-4 mr-2" />
              Edit Course
            </Button>
          </Link>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Course Overview */}
          <Card>
            <CardHeader>
              <CardTitle>Course Overview</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div>
                  <h3 className="font-medium mb-2">Description</h3>
                  <p className="text-muted-foreground">{course.description}</p>
                </div>

                <div className="flex flex-wrap gap-2">
                  <Badge className={getLevelColor(course.level)}>{getLevelText(course.level)}</Badge>
                  <Badge className={getAreaColor(course.area)}>{course.area.charAt(0).toUpperCase() + course.area.slice(1)}</Badge>
                </div>

                {course.tools && course.tools.length > 0 && (
                  <div>
                    <h3 className="font-medium mb-2">Tools & Technologies</h3>
                    <div className="flex flex-wrap gap-1">
                      {course.tools.map((tool, index) => (
                        <Badge key={index} variant="outline">
                          {tool}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Course Content */}
          <Card>
            <CardHeader>
              <CardTitle>Course Content</CardTitle>
              <CardDescription>Manage lessons, modules, and course materials</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8">
                <BookOpen className="w-12 h-12 text-muted-foreground mx-auto mb-4" />
                <h3 className="text-lg font-semibold mb-2">No content yet</h3>
                <p className="text-muted-foreground mb-4">Start building your course by adding lessons and modules</p>
                <Button>Add First Lesson</Button>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Course Stats */}
          <Card>
            <CardHeader>
              <CardTitle>Course Statistics</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center">
                <Users className="w-5 h-5 text-blue-500 mr-3" />
                <div>
                  <p className="text-sm font-medium">Enrolled Students</p>
                  <p className="text-2xl font-bold">0</p>
                </div>
              </div>
              <div className="flex items-center">
                <Clock className="w-5 h-5 text-green-500 mr-3" />
                <div>
                  <p className="text-sm font-medium">Completion Rate</p>
                  <p className="text-2xl font-bold">0%</p>
                </div>
              </div>
              <div className="flex items-center">
                <BookOpen className="w-5 h-5 text-purple-500 mr-3" />
                <div>
                  <p className="text-sm font-medium">Total Lessons</p>
                  <p className="text-2xl font-bold">0</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Course Settings */}
          <Card>
            <CardHeader>
              <CardTitle>Course Settings</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <p className="text-sm font-medium mb-1">Status</p>
                <Badge variant="outline">Draft</Badge>
              </div>
              <div>
                <p className="text-sm font-medium mb-1">Visibility</p>
                <Badge variant="outline">Private</Badge>
              </div>
              <div>
                <p className="text-sm font-medium mb-1">Course ID</p>
                <p className="text-sm text-muted-foreground font-mono">{course.id}</p>
              </div>
              <div>
                <p className="text-sm font-medium mb-1">Slug</p>
                <p className="text-sm text-muted-foreground font-mono">{course.slug}</p>
              </div>
            </CardContent>
          </Card>

          {/* Quick Actions */}
          <Card>
            <CardHeader>
              <CardTitle>Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Button variant="outline" className="w-full justify-start">
                <BookOpen className="w-4 h-4 mr-2" />
                Add Lesson
              </Button>
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
