'use client';

import React, { useCallback, useMemo, useRef, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Switch } from '@game-guild/ui/components/switch';
import { Separator } from '@game-guild/ui/components/separator';
import { ArrowLeft, Clock, Loader2, Save } from 'lucide-react';
import type { SerializedEditorState } from 'lexical';
import type { ContentItemDetail } from '@/lib/learning/types';
import { updateContent } from '@/lib/learning/actions';
import { CONTENT_VISIBILITIES, formatEnumLabel } from '@/lib/learning/enums';
import { LessonContentEditor, parseLexicalState } from './lesson-content-editor';
import { QuizContentEditor } from './quiz-content-editor';

function formatContentTypeLabel(type: ContentItemDetail['type']) {
  if (type === 'Questionnaire') return 'Quiz';
  return type;
}

// ── Component ────────────────────────────────────────────────────────────────

interface ContentItemEditorProps {
  courseId: string;
  item: ContentItemDetail;
  courseTitle: string;
}

export function ContentItemEditor({ courseId, item, courseTitle }: ContentItemEditorProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  const [title, setTitle] = useState(item.title);
  const [description, setDescription] = useState(item.description ?? '');
  const [visibility, setVisibility] = useState<string>(
    item.status === 'published' ? 'Public' : 'Private',
  );
  const [isRequired, setIsRequired] = useState<boolean>(
    (item.settings?.isRequired as boolean) ?? true,
  );
  const [estimatedMinutes, setEstimatedMinutes] = useState<string>(
    item.duration != null ? String(item.duration) : '',
  );
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  // ── Lexical editor state (Lesson only) ──
  const initialLexicalState = useMemo(() => parseLexicalState(item.content), [item.content]);
  const editorStateRef = useRef<SerializedEditorState | null>(initialLexicalState);

  const handleEditorChange = useCallback(
    (state: SerializedEditorState) => {
      editorStateRef.current = state;
    },
    [],
  );

  const isLesson = item.type === 'Lesson';
  const isQuiz = item.type === 'Questionnaire';
  const contentTypeLabel = formatContentTypeLabel(item.type);

  function handleSave() {
    if (!title.trim()) {
      setError('Title is required.');
      return;
    }
    setError(null);
    setSaved(false);

    // For Lesson: serialize Lexical JSON. For Quiz: body is managed by the quiz engine.
    const bodyToSave =
      isLesson && editorStateRef.current ? JSON.stringify(editorStateRef.current) : undefined;

    startTransition(async () => {
      const result = await updateContent({
        courseId,
        contentId: item.id,
        title: title.trim(),
        description: description.trim() || undefined,
        body: bodyToSave,
        visibility,
        isRequired,
        estimatedMinutes: estimatedMinutes ? Number(estimatedMinutes) : undefined,
      });

      if (result.success) {
        setSaved(true);
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function handleBack() {
    router.back();
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="sm" onClick={handleBack}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back
        </Button>
        <div className="flex-1">
          <p className="text-muted-foreground text-sm">{courseTitle}</p>
          <h1 className="text-2xl font-bold">{item.title}</h1>
        </div>
        <Badge variant="secondary">{contentTypeLabel}</Badge>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Main editor area */}
        <div className="space-y-6 lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle>{contentTypeLabel} content</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="title">Title</Label>
                <Input
                  id="title"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="Content title"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Brief description for enrolled students"
                  rows={3}
                />
              </div>

              <Separator />

              {/* ── Body editor: conditional by content type ── */}
              {isLesson && (
                <LessonContentEditor
                  itemId={item.id}
                  initialState={initialLexicalState}
                  onChange={handleEditorChange}
                />
              )}

              {isQuiz && <QuizContentEditor />}

              {!isLesson && !isQuiz && (
                <div className="space-y-2">
                  <Label>Body</Label>
                  <p className="text-muted-foreground text-sm py-8 text-center">
                    Editor for <strong>{contentTypeLabel}</strong> content is not yet available.
                  </p>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Sidebar settings */}
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>{contentTypeLabel} publication</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="visibility">{contentTypeLabel} visibility</Label>
                <Select value={visibility} onValueChange={setVisibility}>
                  <SelectTrigger id="visibility">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {CONTENT_VISIBILITIES.map((v) => (
                      <SelectItem key={v} value={v}>
                        {formatEnumLabel(v)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-muted-foreground text-xs">
                  Controls enrolled-student access only. Public course landing-page visibility is managed in Listing.
                </p>
              </div>

              <div className="flex items-center justify-between">
                <Label htmlFor="required">Required</Label>
                <Switch
                  id="required"
                  checked={isRequired}
                  onCheckedChange={setIsRequired}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="duration">
                  <Clock className="mr-1 inline h-3 w-3" />
                  Estimated minutes
                </Label>
                <Input
                  id="duration"
                  type="number"
                  min={0}
                  value={estimatedMinutes}
                  onChange={(e) => setEstimatedMinutes(e.target.value)}
                  placeholder="e.g. 15"
                />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {error && (
                <p className="text-destructive text-sm">{error}</p>
              )}
              {saved && (
                <p className="text-sm text-green-600">Saved successfully.</p>
              )}

              <Button
                className="w-full"
                onClick={handleSave}
                disabled={isPending}
              >
                {isPending ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Save className="mr-2 h-4 w-4" />
                )}
                Save Changes
              </Button>
              <Button variant="outline" className="w-full" onClick={handleBack}>
                Cancel
              </Button>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Info</CardTitle>
            </CardHeader>
            <CardContent className="text-muted-foreground space-y-1 text-sm">
              <p>Type: {contentTypeLabel}</p>
              <p>Status: {item.status}</p>
              <p>Created: {new Date(item.createdAt).toLocaleDateString()}</p>
              <p>Updated: {new Date(item.updatedAt).toLocaleDateString()}</p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
