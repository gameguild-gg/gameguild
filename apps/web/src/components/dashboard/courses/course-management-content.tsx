'use client';

import { useActionState, useEffect } from 'react';
import { useCourseContext, useCourseFilters, useCourseSelection, useCoursePagination } from '@/lib/courses';
import { createCourse, updateCourse, deleteCourse, publishCourse, duplicateCourse, bulkUpdateCourses, revalidateCoursesData } from '@/lib/courses/actions';
import { Button } from '@game-guild/ui/components';
import { Input } from '@game-guild/ui/components';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Badge } from '@game-guild/ui/components';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components';
import { Checkbox } from '@game-guild/ui/components';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@game-guild/ui/components';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@game-guild/ui/components';
import { Label } from '@game-guild/ui/components';
import { Textarea } from '@game-guild/ui/components';
import { LoadingSpinner } from '@game-guild/ui/components';
import { BookOpen, Plus, Search, Filter, MoreHorizontal, Edit, Trash2, Eye, Play, Copy, RefreshCw, Grid, List, BarChart3, Calendar, Users } from 'lucide-react';
import { useState } from 'react';

interface CourseManagementContentProps {
  initialPagination?: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

export function CourseManagementContent({ initialPagination }: CourseManagementContentProps) {
  const { state, paginatedCourses, hasSelection, allSelected, selectAll, clearSelection, refreshData, viewMode, toggleViewMode } = useCourseContext();
  const { filters, setSearch, setLevelFilter, setStatusFilter, resetFilters } = useCourseFilters();
  const { selectedCourses, toggleCourse } = useCourseSelection();
  const { pagination, setPage, nextPage, prevPage, hasNextPage, hasPrevPage } = useCoursePagination();

  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [editingCourse, setEditingCourse] = useState<(typeof state.courses)[0] | null>(null);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [courseToDelete, setCourseToDelete] = useState<string | null>(null);

  // Server action states
  const [createState, createCourseAction, isCreatingCourse] = useActionState(createCourse, { success: false });
  const [updateState, updateCourseAction, isUpdatingCourse] = useActionState(updateCourse.bind(null, editingCourse?.id || ''), { success: false });

  // Update pagination from props
  useEffect(() => {
    if (initialPagination) {
      // Update context with server-side pagination
    }
  }, [initialPagination]);

  // Handle successful operations
  useEffect(() => {
    if (createState.success) {
      setIsCreateDialogOpen(false);
      refreshData();
    }
  }, [createState.success, refreshData]);

  useEffect(() => {
    if (updateState.success) {
      setEditingCourse(null);
      refreshData();
    }
  }, [updateState.success, refreshData]);

  const handleDelete = async (courseId: string) => {
    const result = await deleteCourse(courseId);
    if (result.success) {
      setCourseToDelete(null);
      setIsDeleteDialogOpen(false);
      refreshData();
    }
  };

  const handlePublish = async (courseId: string, shouldPublish: boolean) => {
    const result = await publishCourse(courseId, shouldPublish);
    if (result.success) {
      refreshData();
    }
  };

  const handleDuplicate = async (courseId: string) => {
    const result = await duplicateCourse(courseId);
    if (result.success) {
      refreshData();
    }
  };

  const handleBulkUpdate = async (action: 'publish' | 'unpublish' | 'archive') => {
    const updates = selectedCourses.map((id) => ({
      id,
      isPublished: action === 'publish',
      isArchived: action === 'archive',
    }));

    const result = await bulkUpdateCourses(updates);
    if (result.success) {
      clearSelection();
      refreshData();
    }
  };

  const handleRefresh = async () => {
    await revalidateCoursesData();
    refreshData();
  };

  const getStatusBadge = (course: (typeof state.courses)[0]) => {
    if (course.isArchived) return <Badge variant="secondary">Archived</Badge>;
    if (course.isPublished) return <Badge variant="default">Published</Badge>;
    return <Badge variant="outline">Draft</Badge>;
  };

  const getLevelBadge = (level: string) => {
    const variants = {
      beginner: 'default',
      intermediate: 'secondary',
      advanced: 'destructive',
    } as const;

    return <Badge variant={variants[level as keyof typeof variants] || 'outline'}>{level.charAt(0).toUpperCase() + level.slice(1)}</Badge>;
  };

  return (
    <div className="space-y-6">
      {/* Header with Actions */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div className="flex items-center gap-2">
          <BookOpen className="h-6 w-6 text-purple-600" />
          <div>
            <h2 className="text-xl font-semibold">Courses ({pagination.total})</h2>
            <p className="text-sm text-gray-600">{hasSelection ? `${selectedCourses.length} selected` : 'Manage course content and settings'}</p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={toggleViewMode}>
            {viewMode === 'grid' ? (
              <>
                <List className="h-4 w-4 mr-2" />
                List View
              </>
            ) : (
              <>
                <Grid className="h-4 w-4 mr-2" />
                Grid View
              </>
            )}
          </Button>

          <Button variant="outline" size="sm" onClick={handleRefresh} disabled={state.isLoading}>
            <RefreshCw className={`h-4 w-4 mr-2 ${state.isLoading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>

          <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
            <DialogTrigger asChild>
              <Button size="sm">
                <Plus className="h-4 w-4 mr-2" />
                New Course
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl">
              <DialogHeader>
                <DialogTitle>Create New Course</DialogTitle>
                <DialogDescription>Create a new course with content and settings.</DialogDescription>
              </DialogHeader>
              <form action={createCourseAction}>
                <div className="space-y-4 py-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label htmlFor="title">Course Title</Label>
                      <Input id="title" name="title" placeholder="Enter course title" required />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="level">Level</Label>
                      <Select name="level" required>
                        <SelectTrigger>
                          <SelectValue placeholder="Select level" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="beginner">Beginner</SelectItem>
                          <SelectItem value="intermediate">Intermediate</SelectItem>
                          <SelectItem value="advanced">Advanced</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="description">Description</Label>
                    <Textarea id="description" name="description" placeholder="Describe what students will learn..." rows={3} />
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label htmlFor="duration">Duration (hours)</Label>
                      <Input id="duration" name="duration" type="number" min="1" placeholder="e.g., 40" />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="price">Price ($)</Label>
                      <Input id="price" name="price" type="number" min="0" step="0.01" placeholder="e.g., 99.99" />
                    </div>
                  </div>

                  <div className="flex items-center space-x-2">
                    <Checkbox id="isPublished" name="isPublished" />
                    <Label htmlFor="isPublished">Publish immediately</Label>
                  </div>
                </div>
                {createState.error && <div className="text-sm text-red-600 mb-4">{createState.error}</div>}
                <DialogFooter>
                  <Button type="button" variant="outline" onClick={() => setIsCreateDialogOpen(false)}>
                    Cancel
                  </Button>
                  <Button type="submit" disabled={isCreatingCourse}>
                    {isCreatingCourse ? (
                      <>
                        <LoadingSpinner size="sm" className="mr-2" />
                        Creating...
                      </>
                    ) : (
                      'Create Course'
                    )}
                  </Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
        </div>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Filters</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                <Input placeholder="Search courses..." value={filters.search} onChange={(e) => setSearch(e.target.value)} className="pl-10" />
              </div>
            </div>

            <Select value={filters.level} onValueChange={setLevelFilter}>
              <SelectTrigger className="w-48">
                <SelectValue placeholder="All Levels" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Levels</SelectItem>
                <SelectItem value="beginner">Beginner</SelectItem>
                <SelectItem value="intermediate">Intermediate</SelectItem>
                <SelectItem value="advanced">Advanced</SelectItem>
              </SelectContent>
            </Select>

            <Select value={filters.status} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-48">
                <SelectValue placeholder="All Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="published">Published</SelectItem>
                <SelectItem value="draft">Draft</SelectItem>
                <SelectItem value="archived">Archived</SelectItem>
              </SelectContent>
            </Select>

            <Button variant="outline" onClick={resetFilters}>
              <Filter className="h-4 w-4 mr-2" />
              Reset
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Selection Actions */}
      {hasSelection && (
        <Card className="border-purple-200 bg-purple-50">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium">
                {selectedCourses.length} course{selectedCourses.length !== 1 ? 's' : ''} selected
              </span>
              <div className="flex items-center gap-2">
                <Button variant="outline" size="sm" onClick={clearSelection}>
                  Clear Selection
                </Button>
                <Button variant="outline" size="sm" onClick={() => handleBulkUpdate('publish')}>
                  <Play className="h-4 w-4 mr-2" />
                  Publish
                </Button>
                <Button variant="outline" size="sm" onClick={() => handleBulkUpdate('unpublish')}>
                  <Eye className="h-4 w-4 mr-2" />
                  Unpublish
                </Button>
                <Button variant="destructive" size="sm">
                  <Trash2 className="h-4 w-4 mr-2" />
                  Delete
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Courses Table/Grid */}
      {viewMode === 'table' ? (
        <Card>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-12">
                  <Checkbox checked={allSelected} onCheckedChange={selectAll} />
                </TableHead>
                <TableHead>Course</TableHead>
                <TableHead>Level</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Students</TableHead>
                <TableHead>Revenue</TableHead>
                <TableHead>Updated</TableHead>
                <TableHead className="w-12"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {state.isLoading ? (
                <TableRow>
                  <TableCell colSpan={8} className="text-center py-8">
                    <LoadingSpinner />
                  </TableCell>
                </TableRow>
              ) : paginatedCourses.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} className="text-center py-8 text-gray-500">
                    No courses found
                  </TableCell>
                </TableRow>
              ) : (
                paginatedCourses.map((course) => (
                  <TableRow key={course.id}>
                    <TableCell>
                      <Checkbox checked={selectedCourses.includes(course.id)} onCheckedChange={() => toggleCourse(course.id)} />
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <div className="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center">
                          <BookOpen className="h-6 w-6 text-purple-600" />
                        </div>
                        <div>
                          <div className="font-medium line-clamp-2">{course.title}</div>
                          <div className="text-sm text-gray-500 line-clamp-1">{course.description}</div>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>{getLevelBadge(course.level)}</TableCell>
                    <TableCell>{getStatusBadge(course)}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Users className="h-4 w-4 text-gray-500" />
                        {course.analytics?.totalStudents || 0}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <BarChart3 className="h-4 w-4 text-gray-500" />${course.analytics?.totalRevenue?.toFixed(2) || '0.00'}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Calendar className="h-4 w-4 text-gray-500" />
                        {new Date(course.updatedAt).toLocaleDateString()}
                      </div>
                    </TableCell>
                    <TableCell>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="sm">
                            <MoreHorizontal className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem onClick={() => setEditingCourse(course)}>
                            <Edit className="h-4 w-4 mr-2" />
                            Edit
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleDuplicate(course.id)}>
                            <Copy className="h-4 w-4 mr-2" />
                            Duplicate
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handlePublish(course.id, !course.isPublished)}>
                            {course.isPublished ? (
                              <>
                                <Eye className="h-4 w-4 mr-2" />
                                Unpublish
                              </>
                            ) : (
                              <>
                                <Play className="h-4 w-4 mr-2" />
                                Publish
                              </>
                            )}
                          </DropdownMenuItem>
                          <DropdownMenuItem
                            className="text-red-600"
                            onClick={() => {
                              setCourseToDelete(course.id);
                              setIsDeleteDialogOpen(true);
                            }}
                          >
                            <Trash2 className="h-4 w-4 mr-2" />
                            Delete
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </Card>
      ) : (
        // Grid View
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {state.isLoading ? (
            <div className="col-span-full flex justify-center py-8">
              <LoadingSpinner />
            </div>
          ) : paginatedCourses.length === 0 ? (
            <div className="col-span-full text-center py-8 text-gray-500">No courses found</div>
          ) : (
            paginatedCourses.map((course) => (
              <Card key={course.id} className="overflow-hidden hover:shadow-lg transition-shadow">
                <div className="relative">
                  <div className="h-48 bg-gradient-to-br from-purple-400 to-purple-600 flex items-center justify-center">
                    <BookOpen className="h-16 w-16 text-white" />
                  </div>
                  <div className="absolute top-4 right-4 flex gap-2">
                    {getStatusBadge(course)}
                    <Checkbox checked={selectedCourses.includes(course.id)} onCheckedChange={() => toggleCourse(course.id)} className="bg-white" />
                  </div>
                </div>
                <CardContent className="p-6">
                  <div className="flex items-start justify-between mb-2">
                    <h3 className="font-semibold text-lg line-clamp-2">{course.title}</h3>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="sm">
                          <MoreHorizontal className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={() => setEditingCourse(course)}>
                          <Edit className="h-4 w-4 mr-2" />
                          Edit
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleDuplicate(course.id)}>
                          <Copy className="h-4 w-4 mr-2" />
                          Duplicate
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handlePublish(course.id, !course.isPublished)}>
                          {course.isPublished ? (
                            <>
                              <Eye className="h-4 w-4 mr-2" />
                              Unpublish
                            </>
                          ) : (
                            <>
                              <Play className="h-4 w-4 mr-2" />
                              Publish
                            </>
                          )}
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          className="text-red-600"
                          onClick={() => {
                            setCourseToDelete(course.id);
                            setIsDeleteDialogOpen(true);
                          }}
                        >
                          <Trash2 className="h-4 w-4 mr-2" />
                          Delete
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>

                  <p className="text-gray-600 text-sm line-clamp-2 mb-4">{course.description}</p>

                  <div className="flex items-center justify-between mb-4">
                    {getLevelBadge(course.level)}
                    <div className="text-sm text-gray-500">{course.duration} hours</div>
                  </div>

                  <div className="flex items-center justify-between text-sm">
                    <div className="flex items-center gap-1">
                      <Users className="h-4 w-4 text-gray-500" />
                      {course.analytics?.totalStudents || 0} students
                    </div>
                    <div className="flex items-center gap-1">
                      <BarChart3 className="h-4 w-4 text-gray-500" />${course.analytics?.totalRevenue?.toFixed(2) || '0.00'}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))
          )}
        </div>
      )}

      {/* Pagination */}
      <div className="flex items-center justify-between">
        <div className="text-sm text-gray-600">
          Showing {(pagination.page - 1) * pagination.limit + 1} to {Math.min(pagination.page * pagination.limit, pagination.total)} of {pagination.total}{' '}
          courses
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={prevPage} disabled={!hasPrevPage}>
            Previous
          </Button>

          <div className="flex items-center gap-1">
            {Array.from({ length: Math.min(5, pagination.totalPages) }, (_, i) => {
              const pageNum = i + 1;
              return (
                <Button key={pageNum} variant={pagination.page === pageNum ? 'default' : 'outline'} size="sm" onClick={() => setPage(pageNum)} className="w-10">
                  {pageNum}
                </Button>
              );
            })}
          </div>

          <Button variant="outline" size="sm" onClick={nextPage} disabled={!hasNextPage}>
            Next
          </Button>
        </div>
      </div>

      {/* Edit Course Dialog */}
      <Dialog open={!!editingCourse} onOpenChange={(open) => !open && setEditingCourse(null)}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Edit Course</DialogTitle>
            <DialogDescription>Update course information and settings.</DialogDescription>
          </DialogHeader>
          {editingCourse && (
            <form action={updateCourseAction}>
              <div className="space-y-4 py-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="edit-title">Course Title</Label>
                    <Input id="edit-title" name="title" defaultValue={editingCourse.title} required />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="edit-level">Level</Label>
                    <Select name="level" defaultValue={editingCourse.level}>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="beginner">Beginner</SelectItem>
                        <SelectItem value="intermediate">Intermediate</SelectItem>
                        <SelectItem value="advanced">Advanced</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="edit-description">Description</Label>
                  <Textarea id="edit-description" name="description" defaultValue={editingCourse.description} rows={3} />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="edit-duration">Duration (hours)</Label>
                    <Input id="edit-duration" name="duration" type="number" defaultValue={editingCourse.duration} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="edit-price">Price ($)</Label>
                    <Input id="edit-price" name="price" type="number" step="0.01" defaultValue={editingCourse.price} />
                  </div>
                </div>

                <div className="flex items-center space-x-2">
                  <Checkbox id="edit-isPublished" name="isPublished" defaultChecked={editingCourse.isPublished} />
                  <Label htmlFor="edit-isPublished">Published</Label>
                </div>
              </div>
              {updateState.error && <div className="text-sm text-red-600 mb-4">{updateState.error}</div>}
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setEditingCourse(null)}>
                  Cancel
                </Button>
                <Button type="submit" disabled={isUpdatingCourse}>
                  {isUpdatingCourse ? (
                    <>
                      <LoadingSpinner size="sm" className="mr-2" />
                      Updating...
                    </>
                  ) : (
                    'Update Course'
                  )}
                </Button>
              </DialogFooter>
            </form>
          )}
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Course</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this course? This action cannot be undone and will remove all associated content.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsDeleteDialogOpen(false)}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={() => courseToDelete && handleDelete(courseToDelete)}>
              Delete Course
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
