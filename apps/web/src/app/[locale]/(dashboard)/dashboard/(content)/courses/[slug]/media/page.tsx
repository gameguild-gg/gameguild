import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { updateProgram } from '@/lib/content-management/programs/programs.actions';
import { redirect } from 'next/navigation';

interface PageProps {
  params: Promise<{ slug: string; locale: string }>;
}

export default async function CourseMediaPage({ params }: PageProps) {
  const { slug } = await params;
  const result = await getProgramBySlugService(slug);
  const program = result.success ? result.data : null;

  async function saveMedia(formData: FormData) {
    'use server';
    if (!program?.id) return;
    const thumbnail = String(formData.get('thumbnail') ?? '');
    await updateProgram({ path: { id: program.id }, body: { title: undefined, description: undefined, thumbnail } });
    redirect(`/dashboard/courses/${slug}/media`);
  }

  return (
    <div className="flex-1 flex flex-col min-h-0">
      <div className="sticky top-0 z-10 border-b border-border bg-background/95 backdrop-blur-sm">
        <div className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-foreground">Media & Assets</h1>
              <p className="text-sm text-muted-foreground">Update the course thumbnail image URL</p>
            </div>
          </div>
        </div>
      </div>

      <div className="flex-1 p-6 overflow-auto">
        <div className="max-w-2xl mx-auto">
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Thumbnail</CardTitle>
            </CardHeader>
            <CardContent>
              {program ? (
                <form action={saveMedia} className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="thumbnail">Thumbnail URL</Label>
                    <Input id="thumbnail" name="thumbnail" defaultValue={program.thumbnail ?? ''} placeholder="https://..." />
                  </div>
                  <div className="flex justify-end">
                    <Button type="submit">Save</Button>
                  </div>
                </form>
              ) : (
                <div className="text-muted-foreground">Program not found.</div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
