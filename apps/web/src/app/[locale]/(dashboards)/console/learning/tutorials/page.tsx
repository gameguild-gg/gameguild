import { Link } from '@/i18n/navigation';
import { getLearningContentLibrary, type LearningContentLibraryItem } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowLeft, BookOpenCheck, ExternalLink, FileText, Plus, Timer } from 'lucide-react';
import React from 'react';

const tutorialTypes = new Set(['Lesson', 'Code', 'Reflection']);

function formatDuration(minutes: number | null) {
  if (!minutes) {
    return 'Self-paced';
  }

  if (minutes < 60) {
    return `${minutes} min`;
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;

  return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
}

function TutorialRow({ item, index }: { item: LearningContentLibraryItem; index: number }) {
  return (
    <Card>
      <CardContent className="flex flex-col gap-4 p-5 md:flex-row md:items-center md:justify-between">
        <div className="flex min-w-0 items-start gap-4">
          <div className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-muted text-sm font-semibold">
            {String(index + 1).padStart(2, '0')}
          </div>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="line-clamp-1 text-base font-semibold">{item.title}</h2>
              <Badge variant={item.status === 'published' ? 'default' : 'secondary'}>{item.status}</Badge>
            </div>
            <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{item.description || item.courseTitle}</p>
            <div className="mt-3 flex flex-wrap gap-2 text-xs text-muted-foreground">
              <Badge variant="outline">{item.type}</Badge>
              <Badge variant="outline">{item.courseTitle}</Badge>
              <span className="inline-flex items-center gap-1">
                <Timer className="size-3.5" />
                {formatDuration(item.durationMinutes)}
              </span>
            </div>
          </div>
        </div>
        <Button asChild variant="outline" size="sm" className="shrink-0">
          <Link href={`/console/learning/courses/${item.courseId}/content/${item.id}`}>
            Edit tutorial
            <ExternalLink className="ml-2 size-4" />
          </Link>
        </Button>
      </CardContent>
    </Card>
  );
}

export default async function Page(): Promise<React.JSX.Element> {
  const { items, error } = await getLearningContentLibrary();
  const tutorials = items.filter((item) => tutorialTypes.has(item.type)).slice(0, 60);

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link href="/console/learning">
              <ArrowLeft className="size-5" />
            </Link>
          </Button>
          <div className="flex size-12 items-center justify-center rounded-lg bg-linear-to-br from-violet-500 to-fuchsia-600">
            <BookOpenCheck className="size-6 text-white" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Tutorials</h1>
            <p className="text-muted-foreground">Curate the hands-on lessons, walkthroughs, and challenges that power course learning paths.</p>
          </div>
        </div>
        <Button asChild>
          <Link href="/console/learning/courses">
            <Plus className="mr-2 size-4" />
            Add tutorial content
          </Link>
        </Button>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">
          <p className="font-medium">API Error</p>
          <p>{error}</p>
        </div>
      ) : null}

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Published tutorials</CardDescription>
            <CardTitle className="text-3xl">{tutorials.filter((item) => item.status === 'published').length}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Draft tutorials</CardDescription>
            <CardTitle className="text-3xl">{tutorials.filter((item) => item.status !== 'published').length}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Estimated learning time</CardDescription>
            <CardTitle className="text-3xl">{Math.round(tutorials.reduce((sum, item) => sum + (item.durationMinutes ?? 0), 0) / 60)}h</CardTitle>
          </CardHeader>
        </Card>
      </div>

      {tutorials.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 text-center">
            <FileText className="mb-4 size-12 text-muted-foreground" />
            <h3 className="text-lg font-semibold">No tutorial content found</h3>
            <p className="max-w-md text-sm text-muted-foreground">Create course lessons, code exercises, or reflections and they will appear here for curation.</p>
            <Button asChild className="mt-5">
              <Link href="/console/learning/courses/new">Create course</Link>
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {tutorials.map((item, index) => (
            <TutorialRow key={`${item.courseId}-${item.id}`} item={item} index={index} />
          ))}
        </div>
      )}
    </div>
  );
}
