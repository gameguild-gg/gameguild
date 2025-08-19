import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import type { CreateContentDto, ProgramContent } from '@/lib/api/generated/types.gen';
import { createProgramContent, deleteProgramContent, getTopLevelProgramContent, reorderProgramContent } from '@/lib/content-management/programs/programs.actions';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { ChevronRight } from 'lucide-react';
import Link from 'next/link';
import { redirect } from 'next/navigation';

interface PageProps {
  params: Promise<{ slug: string; locale: string }>;
}

export default async function CourseContentPage({ params }: PageProps) {
  const { slug } = await params;

  const program = await getProgramBySlugService(slug);
  if (!program.success) {
    redirect('/dashboard/courses');
  }

  async function addContent(formData: FormData) {
    'use server';
    const title = formData.get('title') as string;
    const description = formData.get('description') as string;
    const body = formData.get('body') as string;

    if (!title || !program.data?.id) return;

    const createData: CreateContentDto = {
      title,
      description: description || undefined,
      body: body || undefined,
      type: 0, // Page
      sortOrder: 0,
      isRequired: false,
      estimatedMinutes: null,
    };

    await createProgramContent({
      path: { id: program.data.id },
      body: createData
    });
    redirect(`/dashboard/courses/${slug}/content`);
  }

  async function removeContent(formData: FormData) {
    'use server';
    const contentId = formData.get('contentId') as string;
    if (!contentId || !program.data?.id) return;

    await deleteProgramContent({
      path: { id: program.data.id, contentId }
    });
    redirect(`/dashboard/courses/${slug}/content`);
  }

  async function moveContent(formData: FormData) {
    'use server';
    const order = formData.get('order') as string;
    if (!order || !program.data?.id) return;

    const contentIds = order.split(',').map(id => id.trim());
    await reorderProgramContent({
      path: { id: program.data.id },
      body: { contentIds }
    });
    redirect(`/dashboard/courses/${slug}/content`);
  }

  const topLevel = program.data?.id ? await getTopLevelProgramContent({ path: { programId: program.data.id } }) : null;
  const items: ProgramContent[] = (topLevel?.data as any) ?? [];

  return (
    <div className="flex-1 flex flex-col min-h-0 p-6">
      <div className="max-w-4xl mx-auto space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Course Content</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <form action={addContent} className="grid gap-3 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="title">Title</Label>
                <Input id="title" name="title" placeholder="New content title" />
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="description">Description</Label>
                <Input id="description" name="description" placeholder="Optional description" />
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="body">Body</Label>
                <Input id="body" name="body" placeholder="Optional body" />
              </div>
              <div className="md:col-span-2 flex justify-end">
                <Button type="submit">Add Content</Button>
              </div>
            </form>

            <div className="space-y-2">
              {items.length === 0 && <div className="text-muted-foreground">No content yet.</div>}
              {items.map((c, idx) => {
                // Generate content slug for navigation
                const contentSlug = c.slug || c.title?.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '') || c.id;

                return (
                  <div key={c.id} className="flex items-center justify-between border rounded p-3">
                    <div className="flex items-center gap-3 flex-1">
                      <span className="text-sm text-muted-foreground font-mono">{idx + 1}</span>
                      <div className="flex-1">
                        <div className="font-medium">{c.title}</div>
                        {c.description && <div className="text-sm text-muted-foreground">{c.description}</div>}
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Link href={`/courses/${slug}/content/${contentSlug}`}>
                        <Button variant="ghost" size="sm">
                          View Content
                          <ChevronRight className="h-4 w-4 ml-2" />
                        </Button>
                      </Link>
                      <form action={removeContent}>
                        <input type="hidden" name="contentId" value={c.id || ''} />
                        <Button type="submit" variant="outline" size="sm">Delete</Button>
                      </form>
                    </div>
                  </div>
                );
              })}
            </div>

            {items.length > 1 && (
              <form action={moveContent} className="flex items-center gap-2">
                <Input name="order" placeholder="Reorder as CSV of IDs" className="flex-1" />
                <Button type="submit" variant="outline">Apply Order</Button>
              </form>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
