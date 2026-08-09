"use client";

import React, {
  useCallback,
  useMemo,
  useRef,
  useState,
  useTransition,
} from "react";
import { useRouter } from "next/navigation";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@game-guild/ui/components/card";
import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import { Textarea } from "@game-guild/ui/components/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import { Switch } from "@game-guild/ui/components/switch";
import { Separator } from "@game-guild/ui/components/separator";
import { ArrowLeft, Clock, Eye, Loader2, Pencil, Save } from "lucide-react";
import type { SerializedEditorState } from "lexical";
import type { LearningCoursesLessonContentFormat } from "@game-guild/client";
import type { ContentItemDetail } from "@/lib/learning/types";
import { updateContent } from "@/lib/learning/actions";
import { CONTENT_VISIBILITIES, formatEnumLabel } from "@/lib/learning/enums";
import { getLessonFormatLabel } from "@/lib/learning/lesson-formats";
import { LearnerLessonRenderer } from "@/components/learning/learner-lesson-renderer";
import {
  LessonContentEditor,
  parseLexicalState,
} from "./lesson-content-editor";
import { LessonCodeEditor } from "./lesson-code-editor";
import { LessonVideoEditor } from "./lesson-video-editor";
import { QuizContentEditor } from "./quiz-content-editor";

function formatContentTypeLabel(type: ContentItemDetail["type"]) {
  if (type === "Questionnaire") return "Quiz";
  return type;
}

// ── Component ────────────────────────────────────────────────────────────────

interface ContentItemEditorProps {
  courseId: string;
  item: ContentItemDetail;
  courseTitle: string;
}

export function ContentItemEditor({
  courseId,
  item,
  courseTitle,
}: ContentItemEditorProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  const [title, setTitle] = useState(item.title);
  const [description, setDescription] = useState(item.description ?? "");
  const [visibility, setVisibility] = useState<string>(
    item.status === "published" ? "Public" : "Private",
  );
  const [isRequired, setIsRequired] = useState<boolean>(
    (item.settings?.isRequired as boolean) ?? true,
  );
  const [estimatedMinutes, setEstimatedMinutes] = useState<string>(
    item.duration != null ? String(item.duration) : "",
  );
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [previewMode, setPreviewMode] = useState(false);

  // ── Lexical editor state (Lesson only) ──
  // jsonBody is the structured object now; parseLexicalState(item.content) is legacy fallback.
  const initialLexicalState = useMemo(
    () =>
      (item.jsonBody as SerializedEditorState | null) ??
      parseLexicalState(item.content),
    [item.jsonBody, item.content],
  );
  const editorStateRef = useRef<SerializedEditorState | null>(
    initialLexicalState,
  );
  // ── Per-format body refs (Lesson only) ──
  // ponytail: simple string refs; one source of truth per format, swapped on format change.
  const codeBodyRef = useRef<string>(item.content ?? "");
  const videoUrlRef = useRef<string>(item.content ?? "");
  const quizContentRef = useRef<string | undefined>(
    item.type === "Questionnaire" ? (item.content ?? undefined) : undefined,
  );

  const isLesson = item.type === "Lesson";
  const isQuiz = item.type === "Questionnaire";

  // ── Selected lesson format ──
  // Backward-compat: legacy lessons have null lessonFormat but Lexical content.
  const initialSelectedFormat = useMemo(() => {
    if (item.lessonFormat) return item.lessonFormat;
    if (item.jsonBody || parseLexicalState(item.content)) return "Lexical";
    return "Markdown";
  }, [item.lessonFormat, item.content]);
  const [selectedFormat, setSelectedFormat] =
    useState<LearningCoursesLessonContentFormat>(initialSelectedFormat);

  const handleEditorChange = useCallback((state: SerializedEditorState) => {
    editorStateRef.current = state;
  }, []);

  const handleQuizContentChange = useCallback((content: string) => {
    quizContentRef.current = content;
  }, []);

  const contentTypeLabel = formatContentTypeLabel(item.type);

  function handleSave() {
    if (!title.trim()) {
      setError("Title is required.");
      return;
    }
    setError(null);
    setSaved(false);

    // Lesson: Lexical → jsonBody (object); text formats → body (string).
    // Quiz: body string via quizContentRef. jsonBody unused on those paths.
    let bodyToSave: string | undefined;
    let jsonBodyToSave: Record<string, unknown> | undefined;
    if (isLesson) {
      switch (selectedFormat) {
        case "Lexical":
          jsonBodyToSave = (editorStateRef.current ?? undefined) as
            | Record<string, unknown>
            | undefined;
          break;
        case "Markdown":
        case "RevealJs":
          bodyToSave = codeBodyRef.current || undefined;
          break;
        case "Video":
          bodyToSave = videoUrlRef.current || undefined;
          break;
      }
    } else if (isQuiz) {
      bodyToSave = quizContentRef.current;
    }

    startTransition(async () => {
      const result = await updateContent({
        courseId,
        contentId: item.id,
        title: title.trim(),
        description: description.trim() || undefined,
        body: bodyToSave,
        ...(isLesson
          ? { jsonBody: jsonBodyToSave, lessonFormat: selectedFormat }
          : {}),
        visibility,
        isRequired,
        estimatedMinutes: estimatedMinutes
          ? Number(estimatedMinutes)
          : undefined,
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
    router.push(
      "/dashboard/learning/courses/" +
        encodeURIComponent(courseId) +
        "/content",
    );
  }

  // ponytail: IIFE mirrors handleLessonChangeFormat/handleSave body logic.
  // Refs mutate live; preview re-reads on every render — no state needed.
  const previewContent: unknown = (() => {
    switch (selectedFormat) {
      case "Lexical":
        return editorStateRef.current;
      case "Markdown":
      case "RevealJs":
        return codeBodyRef.current;
      case "Video":
        return videoUrlRef.current;
      default:
        return "";
    }
  })();

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
              <div className="flex items-center justify-between gap-2">
                <CardTitle>{contentTypeLabel} content</CardTitle>
                {isLesson && (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setPreviewMode((v) => !v)}
                  >
                    {previewMode ? (
                      <>
                        <Pencil className="mr-2 h-4 w-4" />
                        Edit
                      </>
                    ) : (
                      <>
                        <Eye className="mr-2 h-4 w-4" />
                        Preview
                      </>
                    )}
                  </Button>
                )}
              </div>
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

              {/* ── Lesson format display + body editor ── */}
              {isLesson && (
                <div className="space-y-2">
                  <Label htmlFor="lesson-format">Lesson format</Label>
                  <Input
                    id="lesson-format"
                    value={getLessonFormatLabel(selectedFormat)}
                    readOnly
                    className="bg-muted"
                  />
                </div>
              )}

              {isLesson && selectedFormat === "Lexical" && !previewMode && (
                <LessonContentEditor
                  itemId={item.id}
                  initialState={initialLexicalState}
                  onChange={handleEditorChange}
                />
              )}

              {isLesson &&
                selectedFormat === "Markdown" &&
                !previewMode && (
                  <LessonCodeEditor
                    key={item.id}
                    initialValue={codeBodyRef.current}
                    language="markdown"
                    placeholder="Write lesson content in Markdown."
                    onChange={(v) => (codeBodyRef.current = v)}
                  />
                )}

              {isLesson &&
                selectedFormat === "RevealJs" &&
                !previewMode && (
                  <LessonCodeEditor
                    key={item.id}
                    initialValue={codeBodyRef.current}
                    language="markdown"
                    placeholder="Author slides in Markdown — separate slides with --- on its own line."
                    onChange={(v) => (codeBodyRef.current = v)}
                  />
                )}

              {isLesson && selectedFormat === "Video" && !previewMode && (
                <LessonVideoEditor
                  key={item.id}
                  initialValue={videoUrlRef.current}
                  onChange={(v) => (videoUrlRef.current = v)}
                />
              )}

              {isLesson && previewMode && (
                <div data-testid="lesson-preview" className="rounded-md border p-4">
                  <LearnerLessonRenderer
                    courseId={courseId}
                    itemId={item.id}
                    format={selectedFormat}
                    content={previewContent}
                  />
                </div>
              )}

              {isQuiz && (
                <QuizContentEditor
                  key={item.id}
                  initialContent={item.content}
                  onChange={handleQuizContentChange}
                />
              )}

              {!isLesson && !isQuiz && (
                <div className="space-y-2">
                  <Label>Body</Label>
                  <p className="text-muted-foreground text-sm py-8 text-center">
                    Editor for <strong>{contentTypeLabel}</strong> content is
                    not yet available.
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
                <Label htmlFor="visibility">
                  {contentTypeLabel} visibility
                </Label>
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
                  Controls enrolled-student access only. Public course
                  landing-page visibility is managed in Listing.
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
              {error && <p className="text-destructive text-sm">{error}</p>}
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
