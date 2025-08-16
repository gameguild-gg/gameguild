import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { publishProgram, unpublishProgram, archiveProgram, restoreProgram } from '@/lib/content-management/programs/programs.actions';
import { redirect } from 'next/navigation';
import type { ContentStatus } from '@/lib/api/generated/types.gen';

interface PageProps {
  params: Promise<{ slug: string; locale: string }>;
}

export default async function CourseSettingsPage({ params }: PageProps) {
  const { slug } = await params;
  const result = await getProgramBySlugService(slug);
  const program = result.success ? result.data : null;

  async function onPublish() {
    'use server';
    if (!program?.id) return;
    await publishProgram(program.id);
    redirect(`/dashboard/courses/${slug}/settings`);
  }

  async function onUnpublish() {
    'use server';
    if (!program?.id) return;
    await unpublishProgram(program.id);
    redirect(`/dashboard/courses/${slug}/settings`);
  }

  async function onArchive() {
    'use server';
    if (!program?.id) return;
    await archiveProgram(program.id);
    redirect(`/dashboard/courses/${slug}/settings`);
  }

  async function onRestore() {
    'use server';
    if (!program?.id) return;
    await restoreProgram(program.id);
    redirect(`/dashboard/courses/${slug}/settings`);
  }

  const status = (program?.status ?? 0) as ContentStatus;
  const isPublished = status === 2; // ContentStatus.PUBLISHED
  const isArchived = status === 3; // ContentStatus.ARCHIVED

  return (
    <div className="flex-1 flex flex-col min-h-0 p-6">
      <div className="max-w-3xl space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Course Status</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="grid gap-1">
                <div className="text-sm text-muted-foreground">Current status</div>
                <Badge variant={isArchived ? 'secondary' : isPublished ? 'default' : 'secondary'}>
                  {isArchived ? 'Archived' : isPublished ? 'Published' : 'Draft'}
                </Badge>
              </div>
              <div className="flex gap-2">
                {!isPublished && !isArchived && (
                  <form action={onPublish}><Button type="submit">Publish</Button></form>
                )}
                {isPublished && (
                  <form action={onUnpublish}><Button type="submit" variant="outline">Unpublish</Button></form>
                )}
                {!isArchived && (
                  <form action={onArchive}><Button type="submit" variant="outline">Archive</Button></form>
                )}
                {isArchived && (
                  <form action={onRestore}><Button type="submit">Restore</Button></form>
                )}
              </div>
            </div>
            <div className="flex items-center justify-between">
              <div className="text-sm text-muted-foreground">Public URL</div>
              <code className="px-2 py-1 bg-muted rounded text-sm">/courses/{slug}</code>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
