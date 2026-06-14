'use client';

import { updateCourseLandingProjects } from '@/lib/learning/actions';
import type { CourseLandingProject } from '@/lib/learning/queries/listing';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Loader2, Plus, Save, Trash2 } from 'lucide-react';
import type { FormEvent } from 'react';
import { useState, useTransition } from 'react';

interface ProjectCarouselEditorFormProps {
  courseId: string;
  items: CourseLandingProject[];
}

interface EditableProjectItem {
  id: string;
  title: string;
  summary: string;
  image: string;
  skills: string;
  deliverable: string;
  moduleLabel: string;
}

function createEmptyProject(index: number): EditableProjectItem {
  return {
    id: `new-project-${index}`,
    title: '',
    summary: '',
    image: '',
    skills: '',
    deliverable: '',
    moduleLabel: `Project ${String(index).padStart(2, '0')}`,
  };
}

export function ProjectCarouselEditorForm({ courseId, items }: ProjectCarouselEditorFormProps) {
  const [isPending, startTransition] = useTransition();
  const [draftItems, setDraftItems] = useState<EditableProjectItem[]>(
    items.length > 0
      ? items.map((item) => ({
          id: item.id,
          title: item.title,
          summary: item.summary,
          image: item.image,
          skills: item.skills.join(', '),
          deliverable: item.deliverable,
          moduleLabel: item.moduleLabel,
        }))
      : [createEmptyProject(1)],
  );
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  function updateItem(id: string, field: keyof Omit<EditableProjectItem, 'id'>, value: string) {
    setDraftItems((current) => current.map((item) => (item.id === id ? { ...item, [field]: value } : item)));
    setSuccess(false);
  }

  function addItem() {
    setDraftItems((current) => [...current, { ...createEmptyProject(current.length + 1), id: `new-project-${Date.now()}` }]);
    setSuccess(false);
  }

  function removeItem(id: string) {
    setDraftItems((current) => {
      const next = current.filter((item) => item.id !== id);
      return next.length > 0 ? next : [createEmptyProject(1)];
    });
    setSuccess(false);
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(false);

    startTransition(async () => {
      const result = await updateCourseLandingProjects(courseId, draftItems);

      if (!result.success) {
        setError(result.error);
        return;
      }

      setSuccess(true);
    });
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <div className="space-y-4">
        {draftItems.map((item, index) => (
          <div key={item.id} className="rounded-lg border p-4">
            <div className="mb-4 flex items-center justify-between gap-4">
              <p className="text-sm font-medium">Project {index + 1}</p>
              <Button type="button" variant="ghost" size="icon" onClick={() => removeItem(item.id)} aria-label={`Remove project ${index + 1}`}>
                <Trash2 className="size-4" />
              </Button>
            </div>

            <div className="grid gap-4 lg:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor={`${item.id}-title`}>Project title {index + 1}</Label>
                <Input
                  id={`${item.id}-title`}
                  value={item.title}
                  onChange={(event) => updateItem(item.id, 'title', event.target.value)}
                  placeholder="Boss behavior sandbox"
                />
              </div>

              <div className="grid gap-2">
                <Label htmlFor={`${item.id}-module-label`}>Module label {index + 1}</Label>
                <Input
                  id={`${item.id}-module-label`}
                  value={item.moduleLabel}
                  onChange={(event) => updateItem(item.id, 'moduleLabel', event.target.value)}
                  placeholder="Project 01"
                />
              </div>

              <div className="grid gap-2 lg:col-span-2">
                <Label htmlFor={`${item.id}-summary`}>Summary {index + 1}</Label>
                <Textarea
                  id={`${item.id}-summary`}
                  value={item.summary}
                  onChange={(event) => updateItem(item.id, 'summary', event.target.value)}
                  placeholder="Describe the project in clear student-facing language."
                  rows={3}
                />
              </div>

              <div className="grid gap-2 lg:col-span-2">
                <Label htmlFor={`${item.id}-deliverable`}>Deliverable {index + 1}</Label>
                <Textarea
                  id={`${item.id}-deliverable`}
                  value={item.deliverable}
                  onChange={(event) => updateItem(item.id, 'deliverable', event.target.value)}
                  placeholder="Explain what students will ship or present."
                  rows={3}
                />
              </div>

              <div className="grid gap-2">
                <Label htmlFor={`${item.id}-image`}>Image URL {index + 1}</Label>
                <Input
                  id={`${item.id}-image`}
                  type="url"
                  value={item.image}
                  onChange={(event) => updateItem(item.id, 'image', event.target.value)}
                  placeholder="https://example.com/project.jpg"
                />
              </div>

              <div className="grid gap-2">
                <Label htmlFor={`${item.id}-skills`}>Skills {index + 1}</Label>
                <Input
                  id={`${item.id}-skills`}
                  value={item.skills}
                  onChange={(event) => updateItem(item.id, 'skills', event.target.value)}
                  placeholder="State debugging, Combat pacing"
                />
              </div>
            </div>
          </div>
        ))}
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">{error}</div>
      ) : null}
      {success ? (
        <div className="rounded-md border border-green-200 bg-green-50 p-3 text-sm text-green-700 dark:border-green-900 dark:bg-green-950 dark:text-green-300">
          Project carousel updated successfully.
        </div>
      ) : null}

      <div className="flex flex-wrap gap-3">
        <Button type="button" variant="outline" onClick={addItem}>
          <Plus className="mr-2 size-4" />
          Add project
        </Button>
        <Button type="submit" disabled={isPending}>
          {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Save className="mr-2 size-4" />}
          Save project carousel
        </Button>
      </div>
    </form>
  );
}
