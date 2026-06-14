'use client';

import { Link } from '@/i18n/navigation';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { ArrowDown, ArrowUp, ArrowUpDown, BookOpen, Grid3X3, List, Search } from 'lucide-react';
import { useMemo, useState } from 'react';
import { CourseCard, CourseTableActions } from './course-card';

interface EnrichedCourse {
  id: string;
  title: string;
  status: string;
  visibility: string;
  enrolledCount: number;
  completionPercent: number | null;
  avgRating: string | null;
  createdAt?: string;
  updatedAt?: string;
}

type ViewMode = 'grid' | 'table';
type SortField = 'title' | 'enrolled' | 'completion' | 'rating';
type SortDirection = 'asc' | 'desc';

function getStatusBadge(status: string) {
  switch (status) {
    case 'published':
      return <Badge className="bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">Published</Badge>;
    case 'draft':
      return <Badge variant="secondary">Draft</Badge>;
    case 'archived':
      return <Badge variant="outline">Archived</Badge>;
    default:
      return null;
  }
}

function SortableHeader({
  label,
  field,
  currentSort,
  currentDirection,
  onSort,
}: {
  label: string;
  field: SortField;
  currentSort: SortField;
  currentDirection: SortDirection;
  onSort: (field: SortField) => void;
}) {
  const isActive = currentSort === field;
  return (
    <TableHead
      className="cursor-pointer select-none hover:text-foreground"
      onClick={() => onSort(field)}
    >
      <div className="flex items-center gap-1">
        {label}
        {isActive ? (
          currentDirection === 'asc' ? (
            <ArrowUp className="size-3" />
          ) : (
            <ArrowDown className="size-3" />
          )
        ) : (
          <ArrowUpDown className="size-3 opacity-40" />
        )}
      </div>
    </TableHead>
  );
}

export function CourseList({ courses, locale }: { courses: EnrichedCourse[]; locale: string }) {
  const [viewMode, setViewMode] = useState<ViewMode>('grid');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [sortField, setSortField] = useState<SortField>('title');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');

  const handleSort = (field: SortField) => {
    if (field === sortField) {
      setSortDirection((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortField(field);
      setSortDirection('asc');
    }
  };

  const filtered = useMemo(() => {
    let result = courses;

    if (search) {
      const q = search.toLowerCase();
      result = result.filter((c) => c.title.toLowerCase().includes(q));
    }

    if (statusFilter !== 'all') {
      result = result.filter((c) => c.status === statusFilter);
    }

    result = [...result].sort((a, b) => {
      let cmp = 0;
      switch (sortField) {
        case 'title':
          cmp = a.title.localeCompare(b.title);
          break;
        case 'enrolled':
          cmp = a.enrolledCount - b.enrolledCount;
          break;
        case 'completion':
          cmp = (a.completionPercent ?? -1) - (b.completionPercent ?? -1);
          break;
        case 'rating':
          cmp = parseFloat(a.avgRating ?? '0') - parseFloat(b.avgRating ?? '0');
          break;
      }
      return sortDirection === 'asc' ? cmp : -cmp;
    });

    return result;
  }, [courses, search, statusFilter, sortField, sortDirection]);

  const statusCounts = useMemo(() => {
    const counts = { all: courses.length, published: 0, draft: 0, archived: 0 };
    for (const c of courses) {
      if (c.status in counts) {
        counts[c.status as keyof typeof counts]++;
      }
    }
    return counts;
  }, [courses]);

  return (
    <div className="flex flex-col gap-4">
      {/* Controls */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search courses..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-full sm:w-40">
            <SelectValue placeholder="Status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All ({statusCounts.all})</SelectItem>
            <SelectItem value="published">Published ({statusCounts.published})</SelectItem>
            <SelectItem value="draft">Draft ({statusCounts.draft})</SelectItem>
            <SelectItem value="archived">Archived ({statusCounts.archived})</SelectItem>
          </SelectContent>
        </Select>
        <div className="flex rounded-md border">
          <Button
            variant={viewMode === 'grid' ? 'secondary' : 'ghost'}
            size="sm"
            className="rounded-r-none"
            onClick={() => setViewMode('grid')}
          >
            <Grid3X3 className="size-4" />
          </Button>
          <Button
            variant={viewMode === 'table' ? 'secondary' : 'ghost'}
            size="sm"
            className="rounded-l-none"
            onClick={() => setViewMode('table')}
          >
            <List className="size-4" />
          </Button>
        </div>
      </div>

      {/* Results count */}
      {(search || statusFilter !== 'all') && (
        <p className="text-sm text-muted-foreground">
          Showing {filtered.length} of {courses.length} course{courses.length !== 1 ? 's' : ''}
        </p>
      )}

      {/* Empty */}
      {filtered.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 text-center">
            <BookOpen className="mb-4 size-12 text-muted-foreground" />
            <h3 className="text-lg font-semibold">No courses found</h3>
            <p className="text-sm text-muted-foreground">
              {search || statusFilter !== 'all'
                ? 'Try adjusting your search or filter criteria.'
                : 'Create your first course to start teaching.'}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Grid View */}
      {viewMode === 'grid' && filtered.length > 0 && (
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {filtered.map((course) => (
            <CourseCard key={course.id} course={course} locale={locale} />
          ))}
        </div>
      )}

      {/* Table View */}
      {viewMode === 'table' && filtered.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>All Courses</CardTitle>
            <CardDescription>
              {filtered.length} course{filtered.length !== 1 ? 's' : ''}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <SortableHeader label="Course" field="title" currentSort={sortField} currentDirection={sortDirection} onSort={handleSort} />
                    <TableHead>Status</TableHead>
                    <SortableHeader label="Enrolled" field="enrolled" currentSort={sortField} currentDirection={sortDirection} onSort={handleSort} />
                    <SortableHeader label="Completion" field="completion" currentSort={sortField} currentDirection={sortDirection} onSort={handleSort} />
                    <SortableHeader label="Rating" field="rating" currentSort={sortField} currentDirection={sortDirection} onSort={handleSort} />
                    <TableHead className="w-12.5" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filtered.map((course) => (
                    <TableRow key={course.id} className="cursor-pointer">
                      <TableCell>
                        <Link href={`/dashboard/learning/courses/${course.id}`} className="flex items-center gap-3">
                          <div className="flex size-10 shrink-0 items-center justify-center rounded bg-muted">
                            <BookOpen className="size-5 text-muted-foreground" />
                          </div>
                          <span className="font-medium hover:underline">{course.title}</span>
                        </Link>
                      </TableCell>
                      <TableCell>{getStatusBadge(course.status)}</TableCell>
                      <TableCell className="text-center">{course.enrolledCount}</TableCell>
                      <TableCell className="text-center">{course.completionPercent !== null ? `${course.completionPercent}%` : '—'}</TableCell>
                      <TableCell className="text-center">{course.avgRating ?? '—'}</TableCell>
                      <TableCell>
                        <CourseTableActions courseId={course.id} courseTitle={course.title} locale={locale} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
