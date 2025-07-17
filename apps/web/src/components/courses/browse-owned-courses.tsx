'use client';

import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Button } from '@game-guild/ui/components/button';
import { Badge } from '@game-guild/ui/components/badge';
import { Progress } from '@game-guild/ui/components/progress';
import { Input } from '@game-guild/ui/components/input';
import { BookOpen, Clock, Trophy, Search, Filter, ChevronRight, Play, CheckCircle, MoreHorizontal, Calendar, Star } from 'lucide-react';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@game-guild/ui/components/dropdown-menu';

interface EnrolledCourse {
  id: string;
  title: string;
  description: string;
  instructor: string;
  thumbnail: string;
  progress: number;
  totalLessons: number;
  completedLessons: number;
  estimatedTime: number;
  difficulty: 'Beginner' | 'Intermediate' | 'Advanced';
  category: string;
  enrolledAt: string;
  lastAccessed?: string;
  status: 'in-progress' | 'completed' | 'not-started';
  certificateEarned?: boolean;
  rating?: number;
  nextLesson?: {
    id: string;
    title: string;
  };
}

const mockEnrolledCourses: EnrolledCourse[] = [
  {
    id: 'course-1',
    title: 'Game Development Fundamentals',
    description: 'Learn the core concepts of game development including design principles, programming basics, and project management.',
    instructor: 'Sarah Johnson',
    thumbnail: '/images/courses/game-dev-fundamentals.jpg',
    progress: 75,
    totalLessons: 20,
    completedLessons: 15,
    estimatedTime: 3,
    difficulty: 'Beginner',
    category: 'Game Development',
    enrolledAt: '2024-01-15',
    lastAccessed: '2024-01-20',
    status: 'in-progress',
    rating: 4.8,
    nextLesson: {
      id: 'lesson-16',
      title: 'Advanced Game Mechanics',
    },
  },
  {
    id: 'course-2',
    title: 'Unity 3D Essentials',
    description: 'Master Unity 3D engine from basics to advanced features for creating stunning 3D games.',
    instructor: 'Mike Chen',
    thumbnail: '/images/courses/unity-3d.jpg',
    progress: 100,
    totalLessons: 25,
    completedLessons: 25,
    estimatedTime: 0,
    difficulty: 'Intermediate',
    category: 'Game Engines',
    enrolledAt: '2023-12-01',
    lastAccessed: '2024-01-18',
    status: 'completed',
    certificateEarned: true,
    rating: 4.9,
  },
  {
    id: 'course-3',
    title: 'Mobile Game Development with Flutter',
    description: 'Build cross-platform mobile games using Flutter and Dart programming language.',
    instructor: 'Emily Rodriguez',
    thumbnail: '/images/courses/flutter-games.jpg',
    progress: 30,
    totalLessons: 18,
    completedLessons: 5,
    estimatedTime: 8,
    difficulty: 'Intermediate',
    category: 'Mobile Development',
    enrolledAt: '2024-01-10',
    lastAccessed: '2024-01-19',
    status: 'in-progress',
    rating: 4.7,
    nextLesson: {
      id: 'lesson-6',
      title: 'Game Physics in Flutter',
    },
  },
  {
    id: 'course-4',
    title: 'Advanced C# for Game Programming',
    description: 'Deep dive into advanced C# programming concepts specifically for game development.',
    instructor: 'David Kim',
    thumbnail: '/images/courses/csharp-advanced.jpg',
    progress: 0,
    totalLessons: 30,
    completedLessons: 0,
    estimatedTime: 15,
    difficulty: 'Advanced',
    category: 'Programming',
    enrolledAt: '2024-01-22',
    status: 'not-started',
    rating: 4.6,
  },
];

export function BrowseOwnedCoursesPage() {
  const [courses, setCourses] = useState<EnrolledCourse[]>(mockEnrolledCourses);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [sortBy, setSortBy] = useState<string>('recent');

  const filteredCourses = courses.filter((course) => {
    const matchesSearch =
      course.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      course.instructor.toLowerCase().includes(searchQuery.toLowerCase()) ||
      course.category.toLowerCase().includes(searchQuery.toLowerCase());

    const matchesStatus = statusFilter === 'all' || course.status === statusFilter;

    return matchesSearch && matchesStatus;
  });

  const sortedCourses = [...filteredCourses].sort((a, b) => {
    switch (sortBy) {
      case 'recent':
        return new Date(b.lastAccessed || b.enrolledAt).getTime() - new Date(a.lastAccessed || a.enrolledAt).getTime();
      case 'progress':
        return b.progress - a.progress;
      case 'title':
        return a.title.localeCompare(b.title);
      default:
        return 0;
    }
  });

  const handleContinueCourse = (courseId: string) => {
    console.log(`Continue course: ${courseId}`);
    // TODO: Navigate to course content viewer
  };

  const handleStartCourse = (courseId: string) => {
    console.log(`Start course: ${courseId}`);
    // TODO: Navigate to course content viewer
  };

  const handleViewCertificate = (courseId: string) => {
    console.log(`View certificate for course: ${courseId}`);
    // TODO: Open certificate viewer
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'completed':
        return <Badge className="bg-green-500 text-white">Completed</Badge>;
      case 'in-progress':
        return <Badge className="bg-blue-500 text-white">In Progress</Badge>;
      case 'not-started':
        return <Badge variant="outline">Not Started</Badge>;
      default:
        return null;
    }
  };

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty) {
      case 'Beginner':
        return 'text-green-400';
      case 'Intermediate':
        return 'text-yellow-400';
      case 'Advanced':
        return 'text-red-400';
      default:
        return 'text-gray-400';
    }
  };

  return (
    <div className="min-h-screen bg-gray-950 text-white">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold mb-2">My Courses</h1>
          <p className="text-gray-400">Continue your learning journey</p>
        </div>

        {/* Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
          <Card className="bg-gray-900 border-gray-800">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <BookOpen className="h-8 w-8 text-blue-400" />
                <div>
                  <p className="text-2xl font-bold">{courses.length}</p>
                  <p className="text-sm text-gray-400">Enrolled Courses</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card className="bg-gray-900 border-gray-800">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <CheckCircle className="h-8 w-8 text-green-400" />
                <div>
                  <p className="text-2xl font-bold">{courses.filter((c) => c.status === 'completed').length}</p>
                  <p className="text-sm text-gray-400">Completed</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card className="bg-gray-900 border-gray-800">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <Clock className="h-8 w-8 text-yellow-400" />
                <div>
                  <p className="text-2xl font-bold">{courses.filter((c) => c.status === 'in-progress').length}</p>
                  <p className="text-sm text-gray-400">In Progress</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card className="bg-gray-900 border-gray-800">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <Trophy className="h-8 w-8 text-purple-400" />
                <div>
                  <p className="text-2xl font-bold">{courses.filter((c) => c.certificateEarned).length}</p>
                  <p className="text-sm text-gray-400">Certificates</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Filters and Search */}
        <div className="flex flex-col md:flex-row gap-4 mb-6">
          <div className="flex-1">
            <div className="relative">
              <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
              <Input
                placeholder="Search courses, instructors, or categories..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10 bg-gray-900 border-gray-700"
              />
            </div>
          </div>
          <div className="flex gap-2">
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" className="border-gray-700">
                  <Filter className="h-4 w-4 mr-2" />
                  Status: {statusFilter === 'all' ? 'All' : statusFilter}
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent>
                <DropdownMenuItem onClick={() => setStatusFilter('all')}>All Courses</DropdownMenuItem>
                <DropdownMenuItem onClick={() => setStatusFilter('in-progress')}>In Progress</DropdownMenuItem>
                <DropdownMenuItem onClick={() => setStatusFilter('completed')}>Completed</DropdownMenuItem>
                <DropdownMenuItem onClick={() => setStatusFilter('not-started')}>Not Started</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" className="border-gray-700">
                  Sort: {sortBy === 'recent' ? 'Recent' : sortBy === 'progress' ? 'Progress' : 'Title'}
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent>
                <DropdownMenuItem onClick={() => setSortBy('recent')}>Recently Accessed</DropdownMenuItem>
                <DropdownMenuItem onClick={() => setSortBy('progress')}>Progress</DropdownMenuItem>
                <DropdownMenuItem onClick={() => setSortBy('title')}>Title</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>

        {/* Course Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {sortedCourses.map((course) => (
            <Card key={course.id} className="bg-gray-900 border-gray-800 hover:border-gray-700 transition-colors">
              <div className="relative">
                <div className="h-48 bg-gradient-to-br from-blue-600 to-purple-600 rounded-t-lg flex items-center justify-center">
                  <BookOpen className="h-16 w-16 text-white opacity-80" />
                </div>
                <div className="absolute top-2 right-2">
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent>
                      <DropdownMenuItem onClick={() => handleContinueCourse(course.id)}>Continue Course</DropdownMenuItem>
                      {course.certificateEarned && <DropdownMenuItem onClick={() => handleViewCertificate(course.id)}>View Certificate</DropdownMenuItem>}
                      <DropdownMenuItem>Remove from Library</DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
                <div className="absolute top-2 left-2 flex gap-2">
                  {getStatusBadge(course.status)}
                  {course.certificateEarned && (
                    <Badge className="bg-yellow-500 text-black">
                      <Trophy className="h-3 w-3 mr-1" />
                      Certified
                    </Badge>
                  )}
                </div>
              </div>
              <CardHeader>
                <CardTitle className="text-lg">{course.title}</CardTitle>
                <div className="flex items-center justify-between text-sm text-gray-400">
                  <span>by {course.instructor}</span>
                  {course.rating && (
                    <div className="flex items-center gap-1">
                      <Star className="h-3 w-3 fill-yellow-400 text-yellow-400" />
                      <span>{course.rating}</span>
                    </div>
                  )}
                </div>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-gray-400 mb-4 line-clamp-2">{course.description}</p>

                <div className="space-y-3 mb-4">
                  <div className="flex items-center justify-between text-sm">
                    <span>Progress</span>
                    <span>{course.progress}%</span>
                  </div>
                  <Progress value={course.progress} className="h-2" />

                  <div className="flex items-center justify-between text-sm text-gray-400">
                    <span>
                      {course.completedLessons}/{course.totalLessons} lessons
                    </span>
                    {course.estimatedTime > 0 && (
                      <span className="flex items-center gap-1">
                        <Clock className="h-3 w-3" />
                        {course.estimatedTime}h left
                      </span>
                    )}
                  </div>
                </div>

                <div className="flex items-center justify-between text-xs text-gray-500 mb-4">
                  <span className={getDifficultyColor(course.difficulty)}>{course.difficulty}</span>
                  <span>{course.category}</span>
                </div>

                {course.nextLesson && course.status === 'in-progress' && (
                  <div className="bg-gray-800 p-3 rounded-lg mb-4">
                    <p className="text-xs text-gray-400 mb-1">Next lesson:</p>
                    <p className="text-sm font-medium">{course.nextLesson.title}</p>
                  </div>
                )}

                <div className="flex gap-2">
                  {course.status === 'not-started' ? (
                    <Button onClick={() => handleStartCourse(course.id)} className="flex-1">
                      <Play className="h-4 w-4 mr-2" />
                      Start Course
                    </Button>
                  ) : course.status === 'completed' ? (
                    <Button variant="outline" onClick={() => handleContinueCourse(course.id)} className="flex-1">
                      <BookOpen className="h-4 w-4 mr-2" />
                      Review Course
                    </Button>
                  ) : (
                    <Button onClick={() => handleContinueCourse(course.id)} className="flex-1">
                      <Play className="h-4 w-4 mr-2" />
                      Continue
                    </Button>
                  )}
                  <Button variant="ghost" size="sm" className="px-3">
                    <ChevronRight className="h-4 w-4" />
                  </Button>
                </div>

                {course.lastAccessed && (
                  <div className="flex items-center gap-1 text-xs text-gray-500 mt-2">
                    <Calendar className="h-3 w-3" />
                    Last accessed {new Date(course.lastAccessed).toLocaleDateString()}
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>

        {sortedCourses.length === 0 && (
          <div className="text-center py-12">
            <BookOpen className="h-16 w-16 text-gray-600 mx-auto mb-4" />
            <h3 className="text-xl font-semibold mb-2">No courses found</h3>
            <p className="text-gray-400 mb-4">
              {searchQuery || statusFilter !== 'all' ? 'Try adjusting your search or filters' : "You haven't enrolled in any courses yet"}
            </p>
            {!searchQuery && statusFilter === 'all' && <Button>Browse Course Catalog</Button>}
          </div>
        )}
      </div>
    </div>
  );
}
