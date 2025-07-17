'use client';

import { useEffect, useState } from 'react';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Badge } from '@game-guild/ui/components/badge';
import { getCourseData, revalidateCourseData } from '@/lib/courses/actions';
import { Course } from '@/types/courses';
import { BookOpen, Plus, Search, Edit, Eye, Trash } from 'lucide-react';
import Link from 'next/link';

export default function CoursesPage() {
  const [courses, setCourses] = useState<Course[]>([]);
  const [filteredCourses, setFilteredCourses] = useState<Course[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedArea, setSelectedArea] = useState<string>('all');

  const areas = ['all', 'programming', 'art', 'design', 'audio'];

  useEffect(() => {
    const fetchCourses = async () => {
      try {
        setLoading(true);
        setError(null);

        const courseData = await getCourseData();
        setCourses(courseData.courses);
        setFilteredCourses(courseData.courses);
      } catch (err) {
        console.error('Error fetching courses:', err);
        setError('Failed to load courses');
      } finally {
        setLoading(false);
      }
    };

    fetchCourses();
  }, []);

  useEffect(() => {
    let filtered = courses;

    // Filter by search term
    if (searchTerm) {
      filtered = filtered.filter(
        (course) => course.title.toLowerCase().includes(searchTerm.toLowerCase()) || course.description.toLowerCase().includes(searchTerm.toLowerCase()),
      );
    }

    // Filter by area
    if (selectedArea !== 'all') {
      filtered = filtered.filter((course) => course.area === selectedArea);
    }

    setFilteredCourses(filtered);
  }, [courses, searchTerm, selectedArea]);

  const handleRefresh = async () => {
    try {
      setLoading(true);
      await revalidateCourseData();
      const courseData = await getCourseData();
      setCourses(courseData.courses);
    } catch (err) {
      console.error('Error refreshing courses:', err);
      setError('Failed to refresh courses');
    } finally {
      setLoading(false);
    }
  };

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
          <p className="mt-4 text-muted-foreground">Loading courses...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col flex-1 items-center justify-center">
        <div className="text-center">
          <p className="text-red-500 mb-4">{error}</p>
          <Button onClick={handleRefresh} variant="outline">
            Try Again
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col flex-1 p-6 max-w-7xl mx-auto w-full">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Course Management</h1>
          <p className="text-muted-foreground">Manage your courses and educational content</p>
        </div>
        <div className="flex gap-2">
          <Button onClick={handleRefresh} variant="outline" disabled={loading}>
            {loading ? 'Refreshing...' : 'Refresh'}
          </Button>
          <Link href="/dashboard/courses/create">
            <Button>
              <Plus className="w-4 h-4 mr-2" />
              Create Course
            </Button>
          </Link>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-4 mb-6">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground w-4 h-4" />
          <Input placeholder="Search courses..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-10" />
        </div>
        <select
          value={selectedArea}
          onChange={(e) => setSelectedArea(e.target.value)}
          className="px-3 py-2 border border-input rounded-md bg-background text-sm"
        >
          {areas.map((area) => (
            <option key={area} value={area}>
              {area === 'all' ? 'All Areas' : area.charAt(0).toUpperCase() + area.slice(1)}
            </option>
          ))}
        </select>
      </div>

      {/* Statistics */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center">
              <BookOpen className="w-8 h-8 text-blue-500" />
              <div className="ml-4">
                <p className="text-sm font-medium text-muted-foreground">Total Courses</p>
                <p className="text-2xl font-bold">{courses.length}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center">
              <div className="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center">
                <span className="text-green-600 font-semibold text-sm">B</span>
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-muted-foreground">Beginner</p>
                <p className="text-2xl font-bold">{courses.filter((c) => c.level === 1).length}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center">
              <div className="w-8 h-8 bg-yellow-100 rounded-full flex items-center justify-center">
                <span className="text-yellow-600 font-semibold text-sm">I</span>
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-muted-foreground">Intermediate</p>
                <p className="text-2xl font-bold">{courses.filter((c) => c.level === 2).length}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center">
              <div className="w-8 h-8 bg-red-100 rounded-full flex items-center justify-center">
                <span className="text-red-600 font-semibold text-sm">A</span>
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-muted-foreground">Advanced</p>
                <p className="text-2xl font-bold">{courses.filter((c) => c.level === 3).length}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Courses Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {filteredCourses.map((course) => (
          <Card key={course.id} className="hover:shadow-lg transition-shadow">
            <CardHeader className="pb-3">
              <div className="flex justify-between items-start">
                <CardTitle className="text-lg line-clamp-2">{course.title}</CardTitle>
                <div className="flex gap-1">
                  <Link href={`/courses/${course.slug}`}>
                    <Button variant="ghost" size="sm">
                      <Eye className="w-4 h-4" />
                    </Button>
                  </Link>
                  <Link href={`/dashboard/courses/${course.slug}/edit`}>
                    <Button variant="ghost" size="sm">
                      <Edit className="w-4 h-4" />
                    </Button>
                  </Link>
                  <Button variant="ghost" size="sm" className="text-red-500 hover:text-red-700">
                    <Trash className="w-4 h-4" />
                  </Button>
                </div>
              </div>
              <CardDescription className="line-clamp-3">{course.description}</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex flex-wrap gap-2 mb-3">
                <Badge className={getLevelColor(course.level)}>{getLevelText(course.level)}</Badge>
                <Badge className={getAreaColor(course.area)}>{course.area.charAt(0).toUpperCase() + course.area.slice(1)}</Badge>
              </div>
              {course.tools && course.tools.length > 0 && (
                <div className="flex flex-wrap gap-1">
                  {course.tools.slice(0, 3).map((tool, index) => (
                    <Badge key={index} variant="outline" className="text-xs">
                      {tool}
                    </Badge>
                  ))}
                  {course.tools.length > 3 && (
                    <Badge variant="outline" className="text-xs">
                      +{course.tools.length - 3} more
                    </Badge>
                  )}
                </div>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      {filteredCourses.length === 0 && !loading && (
        <div className="text-center py-12">
          <BookOpen className="w-12 h-12 text-muted-foreground mx-auto mb-4" />
          <h3 className="text-lg font-semibold mb-2">No courses found</h3>
          <p className="text-muted-foreground mb-4">
            {searchTerm || selectedArea !== 'all' ? 'Try adjusting your search criteria' : 'Get started by creating your first course'}
          </p>
          {!searchTerm && selectedArea === 'all' && (
            <Link href="/dashboard/courses/create">
              <Button>
                <Plus className="w-4 h-4 mr-2" />
                Create Your First Course
              </Button>
            </Link>
          )}
        </div>
      )}
    </div>
  );
}
