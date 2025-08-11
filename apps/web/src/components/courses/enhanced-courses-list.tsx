'use client';

import { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { BookOpen, Calendar, Filter, GraduationCap, MoreHorizontal, Plus, RefreshCw, Search, TrendingUp, Users, Zap } from 'lucide-react';
import { getCourseData } from '@/lib/courses/actions';
import { EnhancedCourse } from '@/lib/courses/courses-enhanced.context';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { ViewModeToggle } from '@/components/common/filters/view-mode-toggle';
import Link from 'next/link';

export function EnhancedCoursesList() {
  const [courses, setCourses] = useState<EnhancedCourse[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [areaFilter, setAreaFilter] = useState<string>('all');
  const [levelFilter, setLevelFilter] = useState<string>('all');
  const [viewMode, setViewMode] = useState<'cards' | 'row' | 'table'>('cards');

  useEffect(() => {
    fetchCourses();
  }, []);

  const fetchCourses = async () => {
    try {
      setLoading(true);
      const data = await getCourseData();
      setCourses(data.courses);
    } catch (error) {
      console.error('Error fetching courses:', error);
      setCourses([]);
    } finally {
      setLoading(false);
    }
  };

  const filteredCourses = courses.filter((course) => {
    const matchesSearch = searchTerm === '' || course.title.toLowerCase().includes(searchTerm.toLowerCase()) || course.description.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesArea = areaFilter === 'all' || course.area === areaFilter;
    const matchesLevel = levelFilter === 'all' || course.level.toString() === levelFilter;

    return matchesSearch && matchesArea && matchesLevel;
  });

  const getAreaColor = (area: string) => {
    switch (area) {
      case 'programming':
        return 'bg-blue-900/30 text-blue-300 border-blue-500/30';
      case 'art':
        return 'bg-pink-900/30 text-pink-300 border-pink-500/30';
      case 'design':
        return 'bg-purple-900/30 text-purple-300 border-purple-500/30';
      case 'audio':
        return 'bg-orange-900/30 text-orange-300 border-orange-500/30';
      default:
        return 'bg-muted/30 text-muted-foreground border-border';
    }
  };

  const getLevelColor = (level: number) => {
    switch (level) {
      case 1:
        return 'bg-green-900/30 text-green-300 border-green-500/30';
      case 2:
        return 'bg-yellow-900/30 text-yellow-300 border-yellow-500/30';
      case 3:
        return 'bg-red-900/30 text-red-300 border-red-500/30';
      case 4:
        return 'bg-purple-900/30 text-purple-300 border-purple-500/30';
      default:
        return 'bg-muted/30 text-muted-foreground border-border';
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
        return 'Expert';
      default:
        return 'Unknown';
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'Unknown';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-background p-6">
        <div className="container mx-auto max-w-7xl">
          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
              <p className="text-muted-foreground">Loading courses...</p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Sticky Header */}
      <div className="sticky top-0 z-50 bg-background/95 backdrop-blur-sm border-b border-border shadow-sm">
        <div className="container mx-auto max-w-7xl px-6 py-4">
          <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
            <div>
              <h1 className="text-3xl font-bold bg-gradient-to-r from-primary to-chart-2 bg-clip-text text-transparent">Course Management</h1>
              <p className="text-muted-foreground mt-1 text-base">Manage your educational content and track course performance</p>
            </div>

            <div className="flex flex-col sm:flex-row gap-3">
              <Link href="/dashboard/courses/create">
                <Button className="bg-gradient-to-r from-primary to-chart-2 hover:from-primary/90 hover:to-chart-2/90 text-primary-foreground border-0 shadow-lg">
                  <Plus className="h-4 w-4 mr-2" />
                  Create Course
                </Button>
              </Link>

              <Button variant="outline" onClick={() => fetchCourses()} className="border-border bg-card/50 text-foreground hover:bg-accent/50">
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="container mx-auto max-w-7xl p-6 space-y-8">
        {/* Stats Overview */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <Card className="bg-gradient-to-br from-blue-500/10 to-blue-600/5 border-blue-500/20 backdrop-blur-sm shadow-lg hover:shadow-xl transition-shadow duration-300">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-blue-400 text-sm font-medium">Total Courses</p>
                  <p className="text-3xl font-bold text-foreground">{courses.length}</p>
                </div>
                <BookOpen className="h-8 w-8 text-blue-400" />
              </div>
              <p className="text-blue-400/60 text-xs mt-2">{courses.filter((c) => c.status === 'published').length} published</p>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-green-500/10 to-green-600/5 border-green-500/20 backdrop-blur-sm shadow-lg hover:shadow-xl transition-shadow duration-300">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-green-400 text-sm font-medium">Total Students</p>
                  <p className="text-3xl font-bold text-foreground">{courses.reduce((acc, c) => acc + (c.enrollmentCount || 0), 0)}</p>
                </div>
                <Users className="h-8 w-8 text-green-400" />
              </div>
              <p className="text-green-400/60 text-xs mt-2">Across all courses</p>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-purple-500/10 to-purple-600/5 border-purple-500/20 backdrop-blur-sm shadow-lg hover:shadow-xl transition-shadow duration-300">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-purple-400 text-sm font-medium">Completion Rate</p>
                  <p className="text-3xl font-bold text-foreground">
                    {courses.length > 0
                      ? Math.round(
                          (courses.reduce((acc, c) => acc + (c.analytics?.completions || 0), 0) /
                            Math.max(
                              courses.reduce((acc, c) => acc + (c.enrollmentCount || 0), 0),
                              1,
                            )) *
                            100,
                        )
                      : 0}
                    %
                  </p>
                </div>
                <TrendingUp className="h-8 w-8 text-purple-400" />
              </div>
              <p className="text-purple-400/60 text-xs mt-2">Average across courses</p>
            </CardContent>
          </Card>

          <Card className="bg-gradient-to-br from-orange-500/10 to-orange-600/5 border-orange-500/20 backdrop-blur-sm shadow-lg hover:shadow-xl transition-shadow duration-300">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-orange-400 text-sm font-medium">Avg Rating</p>
                  <p className="text-3xl font-bold text-foreground">{courses.length > 0 ? (courses.reduce((acc, c) => acc + (c.analytics?.averageRating || 0), 0) / courses.length).toFixed(1) : '0.0'}</p>
                </div>
                <GraduationCap className="h-8 w-8 text-orange-400" />
              </div>
              <p className="text-orange-400/60 text-xs mt-2">Student feedback</p>
            </CardContent>
          </Card>
        </div>

        {/* Search and Filters */}
        <Card className="bg-card/50 border-border backdrop-blur-sm shadow-lg">
          <CardContent className="p-6">
            <div className="flex flex-col lg:flex-row gap-4 items-start lg:items-center justify-between">
              <div className="flex flex-col sm:flex-row gap-4 flex-1">
                <div className="relative flex-1 min-w-[300px]">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
                  <Input placeholder="Search courses by title or description..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-10 bg-background/50 border-border text-foreground" />
                </div>

                <Select value={areaFilter} onValueChange={setAreaFilter}>
                  <SelectTrigger className="w-full sm:w-48 bg-background/50 border-border text-foreground">
                    <Filter className="h-4 w-4 mr-2" />
                    <SelectValue placeholder="Filter by area" />
                  </SelectTrigger>
                  <SelectContent className="bg-popover border-border">
                    <SelectItem value="all">All Areas</SelectItem>
                    <SelectItem value="programming">Programming</SelectItem>
                    <SelectItem value="art">Art</SelectItem>
                    <SelectItem value="design">Design</SelectItem>
                    <SelectItem value="audio">Audio</SelectItem>
                  </SelectContent>
                </Select>

                <Select value={levelFilter} onValueChange={setLevelFilter}>
                  <SelectTrigger className="w-full sm:w-48 bg-background/50 border-border text-foreground">
                    <GraduationCap className="h-4 w-4 mr-2" />
                    <SelectValue placeholder="Filter by level" />
                  </SelectTrigger>
                  <SelectContent className="bg-popover border-border">
                    <SelectItem value="all">All Levels</SelectItem>
                    <SelectItem value="1">Beginner</SelectItem>
                    <SelectItem value="2">Intermediate</SelectItem>
                    <SelectItem value="3">Advanced</SelectItem>
                    <SelectItem value="4">Expert</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <ViewModeToggle viewMode={viewMode} onViewModeChange={setViewMode} className="ml-auto" />
            </div>
          </CardContent>
        </Card>

        {/* Courses Display */}
        {filteredCourses.length === 0 ? (
          <Card className="bg-card/50 border-border backdrop-blur-sm shadow-lg">
            <CardContent className="p-12 text-center">
              <BookOpen className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-xl font-semibold text-foreground mb-2">No courses found</h3>
              <p className="text-muted-foreground mb-6">{searchTerm || areaFilter !== 'all' || levelFilter !== 'all' ? 'Try adjusting your search or filters' : 'No courses have been created yet'}</p>
              {!searchTerm && areaFilter === 'all' && levelFilter === 'all' && (
                <Link href="/dashboard/courses/create">
                  <Button className="bg-gradient-to-r from-primary to-chart-2 hover:from-primary/90 hover:to-chart-2/90">
                    <Plus className="h-4 w-4 mr-2" />
                    Create Your First Course
                  </Button>
                </Link>
              )}
            </CardContent>
          </Card>
        ) : viewMode === 'table' ? (
          <Card className="bg-card/50 border-border backdrop-blur-sm shadow-lg">
            <div className="overflow-hidden rounded-lg border border-border">
              <Table>
                <TableHeader className="bg-muted/50">
                  <TableRow className="border-border">
                    <TableHead className="text-foreground">Course</TableHead>
                    <TableHead className="text-foreground">Area</TableHead>
                    <TableHead className="text-foreground">Level</TableHead>
                    <TableHead className="text-foreground">Students</TableHead>
                    <TableHead className="text-foreground">Status</TableHead>
                    <TableHead className="text-foreground">Updated</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredCourses.map((course) => (
                    <TableRow key={course.id} className="border-border hover:bg-muted/20">
                      <TableCell className="font-medium">
                        <div>
                          <p className="font-semibold text-foreground">{course.title}</p>
                          <p className="text-sm text-muted-foreground line-clamp-1">{course.description}</p>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge className={getAreaColor(course.area)} variant="outline">
                          {course.area}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge className={getLevelColor(course.level)} variant="outline">
                          {getLevelText(course.level)}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-foreground">{course.enrollmentCount || 0}</TableCell>
                      <TableCell>
                        <Badge variant={course.status === 'published' ? 'default' : 'secondary'}>{course.status}</Badge>
                      </TableCell>
                      <TableCell className="text-muted-foreground">{formatDate(course.updatedAt)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </Card>
        ) : viewMode === 'row' ? (
          <div className="space-y-4">
            {filteredCourses.map((course) => (
              <Link key={course.id} href={`/dashboard/courses/${course.slug}`}>
                <Card className="bg-card/50 border-border backdrop-blur-sm shadow-lg hover:shadow-xl transition-all duration-300 cursor-pointer">
                  <CardContent className="p-6">
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-start gap-4">
                          <div className="w-12 h-12 bg-gradient-to-br from-primary/20 to-chart-2/20 rounded-lg flex items-center justify-center">
                            <BookOpen className="h-6 w-6 text-primary" />
                          </div>
                          <div className="flex-1">
                            <h3 className="text-lg font-semibold text-foreground mb-2">{course.title}</h3>
                            <p className="text-muted-foreground mb-4 line-clamp-2">{course.description}</p>
                            <div className="flex flex-wrap gap-2 mb-4">
                              <Badge className={getAreaColor(course.area)} variant="outline">
                                {course.area}
                              </Badge>
                              <Badge className={getLevelColor(course.level)} variant="outline">
                                {getLevelText(course.level)}
                              </Badge>
                              <Badge variant={course.status === 'published' ? 'default' : 'secondary'}>{course.status}</Badge>
                            </div>
                            <div className="flex items-center gap-6 text-sm text-muted-foreground">
                              <div className="flex items-center gap-1">
                                <Users className="h-4 w-4" />
                                {course.enrollmentCount || 0} students
                              </div>
                              <div className="flex items-center gap-1">
                                <Calendar className="h-4 w-4" />
                                Updated {formatDate(course.updatedAt)}
                              </div>
                              <div className="flex items-center gap-1">
                                <Zap className="h-4 w-4" />
                                {course.estimatedHours || 0}h estimated
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                      <div className="flex gap-2 ml-4">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="sm">
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent className="bg-popover border-border">
                            <DropdownMenuItem>Duplicate</DropdownMenuItem>
                            <DropdownMenuItem>Archive</DropdownMenuItem>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem className="text-destructive">Delete</DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {filteredCourses.map((course) => (
              <Link key={course.id} href={`/dashboard/courses/${course.slug}`}>
                <Card className="bg-card/50 border-border backdrop-blur-sm shadow-lg hover:shadow-xl transition-all duration-300 group cursor-pointer">
                  <CardHeader className="pb-3">
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <CardTitle className="text-lg line-clamp-2 group-hover:text-primary transition-colors">{course.title}</CardTitle>
                        <p className="text-muted-foreground text-sm mt-2 line-clamp-3">{course.description}</p>
                      </div>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="sm" className="opacity-0 group-hover:opacity-100 transition-opacity">
                            <MoreHorizontal className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent className="bg-popover border-border">
                          <DropdownMenuItem>Duplicate</DropdownMenuItem>
                          <DropdownMenuItem>Archive</DropdownMenuItem>
                          <DropdownMenuSeparator />
                          <DropdownMenuItem className="text-destructive">Delete</DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <div className="flex flex-wrap gap-2 mb-4">
                      <Badge className={getAreaColor(course.area)} variant="outline">
                        {course.area}
                      </Badge>
                      <Badge className={getLevelColor(course.level)} variant="outline">
                        {getLevelText(course.level)}
                      </Badge>
                      <Badge variant={course.status === 'published' ? 'default' : 'secondary'}>{course.status}</Badge>
                    </div>

                    <div className="space-y-2 text-sm text-muted-foreground">
                      <div className="flex items-center justify-between">
                        <span className="flex items-center gap-1">
                          <Users className="h-4 w-4" />
                          Students
                        </span>
                        <span className="font-medium text-foreground">{course.enrollmentCount || 0}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="flex items-center gap-1">
                          <Zap className="h-4 w-4" />
                          Duration
                        </span>
                        <span className="font-medium text-foreground">{course.estimatedHours || 0}h</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="flex items-center gap-1">
                          <Calendar className="h-4 w-4" />
                          Updated
                        </span>
                        <span className="font-medium text-foreground">{formatDate(course.updatedAt)}</span>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
