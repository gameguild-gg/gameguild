import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { createProgramContent, deleteProgramContent, getTopLevelProgramContent, reorderProgramContent } from '@/lib/content-management/programs/programs.actions';
import type { CreateContentDto, ProgramContent } from '@/lib/api/generated/types.gen';
import { redirect } from 'next/navigation';

interface PageProps {
  params: Promise<{ slug: string; locale: string }>;
}

export default async function CourseContentPage({ params }: PageProps) {
  const { slug } = await params;
  const result = await getProgramBySlugService(slug);
  const program = result.success ? result.data : null;

  async function addContent(formData: FormData) {
    'use server';
    if (!program?.id) return;
    const title = String(formData.get('title') || 'New Page');
    const description = String(formData.get('description') || '');
    const body = String(formData.get('body') || '');
    const payload: CreateContentDto = { title, description, body, type: 0 };
  await createProgramContent({ path: { id: program.id }, body: payload, url: '/api/program/{id}/content' });
    redirect(`/dashboard/courses/${slug}/content`);
  }

  async function removeContent(formData: FormData) {
    'use server';
    if (!program?.id) return;
    const contentId = String(formData.get('contentId') || '');
    if (!contentId) return;
  await deleteProgramContent({ path: { id: program.id, contentId }, url: '/api/program/{id}/content/{contentId}' });
    redirect(`/dashboard/courses/${slug}/content`);
  }

  async function moveContent(formData: FormData) {
    'use server';
    if (!program?.id) return;
    const orderCsv = String(formData.get('order') || '');
    const contentIds = orderCsv.split(',').map((s) => s.trim()).filter(Boolean);
  await reorderProgramContent({ path: { id: program.id }, body: { contentIds }, url: '/api/program/{id}/content/reorder' });
    redirect(`/dashboard/courses/${slug}/content`);
  }

  const topLevel = program?.id ? await getTopLevelProgramContent({ path: { programId: program.id }, url: '/api/programs/{programId}/content/top-level' }) : null;
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
              {items.map((c, idx) => (
                <div key={c.id} className="flex items-center justify-between border rounded p-3">
                  <div className="flex items-center gap-3">
                    <span className="text-sm text-muted-foreground font-mono">{idx + 1}</span>
                    <div>
                      <div className="font-medium">{c.title}</div>
                      {c.description && <div className="text-sm text-muted-foreground">{c.description}</div>}
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <form action={removeContent}>
                      <input type="hidden" name="contentId" value={c.id || ''} />
                      <Button type="submit" variant="outline">Delete</Button>
                    </form>
                  </div>
                </div>
              ))}
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
