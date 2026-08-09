import type { LearningCoursesLessonContentFormat } from "@game-guild/client";

export type LessonContentFormat = LearningCoursesLessonContentFormat;

export const DEFAULT_LESSON_FORMAT: LessonContentFormat = "Markdown";

export const LESSON_FORMATS: ReadonlyArray<{
  value: LessonContentFormat;
  label: string;
}> = [
  { value: "Markdown", label: "Markdown" },
  { value: "Lexical", label: "Rich text (Lexical)" },
  { value: "RevealJs", label: "Presentation (RevealJS)" },
  { value: "Video", label: "Video (link)" },
];

export function getLessonFormatLabel(format: string | null | undefined) {
  if (!format) return "Markdown";

  return (
    LESSON_FORMATS.find((candidate) => candidate.value === format)?.label ??
    format
  );
}
