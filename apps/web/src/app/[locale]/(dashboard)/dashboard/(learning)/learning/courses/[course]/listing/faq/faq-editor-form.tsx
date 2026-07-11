'use client';

import { updateCourseFaq } from '@/lib/learning/actions';
import type { CourseFaqItem } from '@/lib/learning/queries/listing';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Loader2, Plus, Save, Trash2 } from 'lucide-react';
import type { FormEvent } from 'react';
import { useState, useTransition } from 'react';

interface FaqEditorFormProps {
  courseId: string;
  items: CourseFaqItem[];
}

interface EditableFaqItem {
  id: string;
  question: string;
  answer: string;
  category: string;
}

function createEmptyItem(): EditableFaqItem {
  return {
    id: `new-${Date.now()}`,
    question: '',
    answer: '',
    category: 'Course details',
  };
}

export function FaqEditorForm({ courseId, items }: FaqEditorFormProps) {
  const [isPending, startTransition] = useTransition();
  const [draftItems, setDraftItems] = useState<EditableFaqItem[]>(
    items.length > 0
      ? items.map((item) => ({
          id: item.id,
          question: item.question,
          answer: item.answer,
          category: item.category ?? 'Course details',
        }))
      : [createEmptyItem()],
  );
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  function updateItem(id: string, field: keyof Omit<EditableFaqItem, 'id'>, value: string) {
    setDraftItems((current) => current.map((item) => (item.id === id ? { ...item, [field]: value } : item)));
    setError(null);
    setSuccess(false);
  }

  function addItem() {
    setDraftItems((current) => [...current, createEmptyItem()]);
    setSuccess(false);
  }

  function removeItem(id: string) {
    setDraftItems((current) => {
      const next = current.filter((item) => item.id !== id);
      return next.length > 0 ? next : [createEmptyItem()];
    });
    setSuccess(false);
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(false);

    const incompleteIndex = draftItems.findIndex((item) => {
      const question = item.question.trim();
      const answer = item.answer.trim();
      return (question.length > 0 || answer.length > 0) && (question.length === 0 || answer.length === 0);
    });

    if (incompleteIndex >= 0) {
      setError(`Complete both the question and answer for Question ${incompleteIndex + 1}.`);
      return;
    }

    startTransition(async () => {
      const result = await updateCourseFaq(courseId, draftItems);

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
              <p className="text-sm font-medium">Question {index + 1}</p>
              <Button type="button" variant="ghost" size="icon" onClick={() => removeItem(item.id)} aria-label={`Remove question ${index + 1}`}>
                <Trash2 className="size-4" />
              </Button>
            </div>

            <div className="grid gap-4">
              <div className="grid gap-2">
                <Label htmlFor={`${item.id}-question`}>Question</Label>
                <Input
                  id={`${item.id}-question`}
                  value={item.question}
                  onChange={(event) => updateItem(item.id, 'question', event.target.value)}
                  placeholder="What will students need before joining?"
                />
              </div>

              <div className="grid gap-2">
                <Label htmlFor={`${item.id}-answer`}>Answer</Label>
                <Textarea
                  id={`${item.id}-answer`}
                  value={item.answer}
                  onChange={(event) => updateItem(item.id, 'answer', event.target.value)}
                  placeholder="Give a clear, student-facing answer."
                  rows={4}
                />
              </div>

              <div className="grid gap-2">
                <Label htmlFor={`${item.id}-category`}>Category</Label>
                <Input
                  id={`${item.id}-category`}
                  value={item.category}
                  onChange={(event) => updateItem(item.id, 'category', event.target.value)}
                  placeholder="Course details"
                />
              </div>
            </div>
          </div>
        ))}
      </div>

      {error ? (
        <div role="alert" className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">{error}</div>
      ) : null}
      {success ? (
        <div className="rounded-md border border-green-200 bg-green-50 p-3 text-sm text-green-700 dark:border-green-900 dark:bg-green-950 dark:text-green-300">
          FAQ updated successfully.
        </div>
      ) : null}

      <div className="flex flex-wrap gap-3">
        <Button type="button" variant="outline" onClick={addItem}>
          <Plus className="mr-2 size-4" />
          Add question
        </Button>
        <Button type="submit" disabled={isPending}>
          {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Save className="mr-2 size-4" />}
          Save FAQ
        </Button>
      </div>
    </form>
  );
}
