import React, { Suspense } from 'react';
import { getPrograms, getProgramStatistics } from '@/lib/programs/programs.actions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Plus, BookOpen, Users, TrendingUp, Eye, Edit, Trash2, Play } from 'lucide-react';
import Link from 'next/link';
import Image from 'next/image';

interface ProgramsPageProps {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}

// Loading component for programs
function ProgramsLoading() {
  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div className="h-8 w-48 bg-gray-200 rounded animate-pulse" />
        <div className="h-10 w-32 bg-gray-200 rounded animate-pulse" />
      </div>

      {/* Statistics skeleton */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {Array.from({ length: 4 }).map((_, i) => (
          <Card key={i}>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <div className="h-4 w-24 bg-gray-200 rounded animate-pulse" />
                <div className="h-6 w-6 bg-gray-200 rounded animate-pulse" />
              </div>
              <div className="h-8 w-16 bg-gray-200 rounded animate-pulse" />
              <div className="h-3 w-20 bg-gray-200 rounded animate-pulse mt-2" />
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Programs list skeleton */}
      <Card>
        <CardHeader>
          <div className="h-6 w-32 bg-gray-200 rounded animate-pulse" />
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="flex items-center justify-between p-4 border rounded-lg">
                <div className="flex items-center space-x-4">
                  <div className="h-16 w-24 bg-gray-200 rounded animate-pulse" />
                  <div className="space-y-2">
                    <div className="h-4 w-32 bg-gray-200 rounded animate-pulse" />
                    <div className="h-3 w-48 bg-gray-200 rounded animate-pulse" />
                    <div className="h-3 w-20 bg-gray-200 rounded animate-pulse" />
                  </div>
                </div>
                <div className="flex items-center space-x-2">
                  <div className="h-6 w-16 bg-gray-200 rounded animate-pulse" />
                  <div className="h-8 w-8 bg-gray-200 rounded animate-pulse" />
                  <div className="h-8 w-8 bg-gray-200 rounded animate-pulse" />
                  <div className="h-8 w-8 bg-gray-200 rounded animate-pulse" />
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// Error component for programs
function ProgramsError({ error }: { error: string }) {
  return (
    <Card>
      <CardContent className="p-6">
        <div className="text-center">
          <BookOpen className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-gray-900 mb-2">Unable to load programs</h3>
          <p className="text-gray-600 mb-4">{error}</p>
          <Button variant="outline" asChild>
            <Link href="/dashboard/courses">Try again</Link>
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

// Helper function to get status color
function getStatusColor(status: string) {
  switch (status) {
    case 'Published':
      return 'default';
    case 'Draft':
      return 'secondary';
    case 'Archived':
      return 'outline';
    default:
      return 'secondary';
  }
}

// Helper function to get visibility color
function getVisibilityColor(visibility: string) {
  switch (visibility) {
    case 'Public':
      return 'default';
    case 'Private':
      return 'destructive';
    case 'Restricted':
      return 'secondary';
    default:
      return 'secondary';
  }
}

// Main programs content component
async function ProgramsContent({ searchParams }: ProgramsPageProps) {
  const params = await searchParams;

  // Extract search parameters
  const page = typeof params.page === 'string' ? parseInt(params.page) : 1;
  const limit = typeof params.limit === 'string' ? parseInt(params.limit) : 20;
  const status = typeof params.status === 'string' ? params.status : undefined;
  const visibility = typeof params.visibility === 'string' ? params.visibility : undefined;

  // Fetch data
  const [programsResult, statisticsResult] = await Promise.all([getPrograms(page, limit, status, visibility), getProgramStatistics()]);

  if (!programsResult.success) {
    return <ProgramsError error={programsResult.error || 'Unknown error'} />;
  }

  const programs = programsResult.data || [];
  const statistics = statisticsResult.data;

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Programs & Courses</h1>
          <p className="text-gray-600">Manage your learning programs and courses</p>
        </div>
        <Button asChild>
          <Link href="/dashboard/courses/create">
            <Plus className="h-4 w-4 mr-2" />
            Create Program
          </Link>
        </Button>
      </div>

      {/* Statistics */}
      {statistics && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Total Programs</p>
                <BookOpen className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.totalPrograms}</div>
              <p className="text-xs text-muted-foreground">+{statistics.programsCreatedThisMonth} this month</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Published</p>
                <Play className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.publishedPrograms}</div>
              <p className="text-xs text-muted-foreground">{statistics.draftPrograms} drafts</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Total Enrollments</p>
                <Users className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.totalEnrollments}</div>
              <p className="text-xs text-muted-foreground">Avg rating: {statistics.averageRating.toFixed(1)}</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">This Week</p>
                <TrendingUp className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.programsCreatedThisWeek}</div>
              <p className="text-xs text-muted-foreground">+{statistics.programsCreatedToday} today</p>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Filter Options */}
      <Card>
        <CardHeader>
          <CardTitle>Filters</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-2">
            <Button variant={!status ? 'default' : 'outline'} size="sm" asChild>
              <Link href="/dashboard/courses">All</Link>
            </Button>
            <Button variant={status === 'Published' ? 'default' : 'outline'} size="sm" asChild>
              <Link href="/dashboard/courses?status=Published">Published</Link>
            </Button>
            <Button variant={status === 'Draft' ? 'default' : 'outline'} size="sm" asChild>
              <Link href="/dashboard/courses?status=Draft">Drafts</Link>
            </Button>
            <Button variant={status === 'Archived' ? 'default' : 'outline'} size="sm" asChild>
              <Link href="/dashboard/courses?status=Archived">Archived</Link>
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Programs List */}
      <Card>
        <CardHeader>
          <CardTitle>Programs</CardTitle>
        </CardHeader>
        <CardContent>
          {programs.length === 0 ? (
            <div className="text-center py-12">
              <BookOpen className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-semibold text-gray-900 mb-2">No programs found</h3>
              <p className="text-gray-600 mb-4">Get started by creating your first program.</p>
              <Button asChild>
                <Link href="/dashboard/courses/create">
                  <Plus className="h-4 w-4 mr-2" />
                  Create Program
                </Link>
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {programs.map((program) => (
                <div key={program.id} className="flex items-center justify-between p-4 border rounded-lg hover:bg-gray-50 transition-colors">
                  <div className="flex items-center space-x-4">
                    {program.imageUrl || program.thumbnail ? (
                      <Image
                        src={program.imageUrl || program.thumbnail || '/placeholder.svg'}
                        alt={program.title}
                        width={96}
                        height={64}
                        className="h-16 w-24 rounded-lg object-cover"
                      />
                    ) : (
                      <div className="h-16 w-24 rounded-lg bg-gray-200 flex items-center justify-center">
                        <BookOpen className="h-8 w-8 text-gray-400" />
                      </div>
                    )}
                    <div>
                      <h3 className="font-semibold text-gray-900">{program.title}</h3>
                      <p className="text-sm text-gray-600 max-w-md truncate">{program.description || program.shortDescription || 'No description'}</p>
                      <div className="flex items-center space-x-2 mt-1">
                        <Badge variant={getStatusColor(program.status)}>{program.status}</Badge>
                        <Badge variant={getVisibilityColor(program.visibility)}>{program.visibility}</Badge>
                        {program.difficulty && (
                          <Badge variant="outline" className="text-xs">
                            {program.difficulty}
                          </Badge>
                        )}
                        {program.duration && <span className="text-xs text-gray-500">{program.duration}h</span>}
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Button variant="ghost" size="sm" asChild>
                      <Link href={`/dashboard/courses/${program.slug || program.id}`}>
                        <Eye className="h-4 w-4" />
                      </Link>
                    </Button>
                    <Button variant="ghost" size="sm" asChild>
                      <Link href={`/dashboard/courses/${program.slug || program.id}/edit`}>
                        <Edit className="h-4 w-4" />
                      </Link>
                    </Button>
                    <Button variant="ghost" size="sm">
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Pagination */}
      {programsResult.pagination && programsResult.pagination.totalPages > 1 && (
        <div className="flex justify-center space-x-2">
          {Array.from({ length: programsResult.pagination.totalPages }, (_, i) => i + 1).map((pageNum) => (
            <Button key={pageNum} variant={pageNum === page ? 'default' : 'outline'} size="sm" asChild>
              <Link href={`/dashboard/courses?page=${pageNum}${status ? `&status=${status}` : ''}${visibility ? `&visibility=${visibility}` : ''}`}>
                {pageNum}
              </Link>
            </Button>
          ))}
        </div>
      )}
    </div>
  );
}

export default async function ProgramsPage({ searchParams }: ProgramsPageProps) {
  return (
    <Suspense fallback={<ProgramsLoading />}>
      <ProgramsContent searchParams={searchParams} />
    </Suspense>
  );
}
