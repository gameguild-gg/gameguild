export type LessonContentFormat =
  | "Markdown"
  | "Lexical"
  | "RevealJs"
  | "Video"
  | "Html"
  | "ExternalLink";

export const DEFAULT_LESSON_FORMAT: LessonContentFormat = "Markdown";

export const LESSON_FORMATS: ReadonlyArray<{
  value: LessonContentFormat;
  label: string;
}> = [
  { value: "Markdown", label: "Markdown" },
  { value: "Html", label: "HTML" },
  { value: "Lexical", label: "Rich text (Lexical)" },
  { value: "RevealJs", label: "Presentation (RevealJS)" },
  { value: "Video", label: "Video (link)" },
  { value: "ExternalLink", label: "External link" },
];

export function getLessonFormatLabel(format: string | null | undefined) {
  if (!format) return "Markdown";

  return (
    LESSON_FORMATS.find((candidate) => candidate.value === format)?.label ??
    format
  );
}
