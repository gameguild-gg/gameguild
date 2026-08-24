import { Link } from '@/i18n/navigation';
import { getLearningContentLibrary, type LearningContentLibraryItem } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowLeft, ExternalLink, FolderOpen, LibraryBig, Plus, Timer } from 'lucide-react';
import React from 'react';

function formatDuration(minutes: number | null) {
  if (!minutes) {
    return 'No duration';
  }

  if (minutes < 60) {
    return `${minutes} min`;
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;

  return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
}

function ResourceCard({ item }: { item: LearningContentLibraryItem }) {
  return (
    <Card>
      <CardHeader className="space-y-3">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <CardTitle className="line-clamp-1 text-base">{item.title}</CardTitle>
            <CardDescription className="mt-1 line-clamp-2">{item.description || item.courseTitle}</CardDescription>
          </div>
          <Badge variant={item.status === 'published' ? 'default' : 'secondary'}>{item.status}</Badge>
        </div>
        <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
          <Badge variant="outline">{item.type}</Badge>
          <Badge variant="outline">{item.visibility}</Badge>
          {item.isRequired ? <Badge variant="outline">Required</Badge> : null}
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <div className="flex items-center justify-between gap-3 text-sm text-muted-foreground">
          <span className="line-clamp-1">{item.courseTitle}</span>
          <span className="inline-flex shrink-0 items-center gap-1">
            <Timer className="size-3.5" />
            {formatDuration(item.durationMinutes)}
          </span>
        </div>
        <Button asChild variant="outline" size="sm" className="justify-between">
          <Link href={`/console/learning/courses/${item.courseId}/content/${item.slug || item.id}`}>
            Edit resource
            <ExternalLink className="size-4" />
          </Link>
        </Button>
      </CardContent>
    </Card>
  );
}

export default async function Page(): Promise<React.JSX.Element> {
  const { items, error } = await getLearningContentLibrary();
  const resourceItems = items.filter((item) => item.type !== 'Questionnaire').slice(0, 60);

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link href="/console/learning">
              <ArrowLeft className="size-5" />
            </Link>
          </Button>
          <div className="flex size-12 items-center justify-center rounded-lg bg-linear-to-br from-cyan-500 to-blue-600">
            <LibraryBig className="size-6 text-white" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Resources</h1>
            <p className="text-muted-foreground">Review every reusable lesson, exercise, download, and reference item across live courses.</p>
          </div>
        </div>
        <Button asChild>
          <Link href="/console/learning/courses">
            <Plus className="mr-2 size-4" />
            Add course content
          </Link>
        </Button>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">
          <p className="font-medium">API Error</p>
          <p>{error}</p>
        </div>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Total resources</CardDescription>
            <CardTitle className="text-3xl">{resourceItems.length}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Published</CardDescription>
            <CardTitle className="text-3xl">{resourceItems.filter((item) => item.status === 'published').length}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Required</CardDescription>
            <CardTitle className="text-3xl">{resourceItems.filter((item) => item.isRequired).length}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Course coverage</CardDescription>
            <CardTitle className="text-3xl">{new Set(resourceItems.map((item) => item.courseId)).size}</CardTitle>
          </CardHeader>
        </Card>
      </div>

      {resourceItems.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 text-center">
            <FolderOpen className="mb-4 size-12 text-muted-foreground" />
            <h3 className="text-lg font-semibold">No course resources found</h3>
            <p className="max-w-md text-sm text-muted-foreground">Create course content first, then this library will show all reusable resources and direct editing links.</p>
            <Button asChild className="mt-5">
              <Link href="/console/learning/courses/new">Create course</Link>
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {resourceItems.map((item) => (
            <ResourceCard key={`${item.courseId}-${item.id}`} item={item} />
          ))}
        </div>
      )}
    </div>
  );
}
