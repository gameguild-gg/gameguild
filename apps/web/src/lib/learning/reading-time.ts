const WORDS_PER_MINUTE = 200;

const TEXT_PROPERTY_KEYS = new Set([
  "text",
  "question",
  "title",
  "description",
  "prompt",
  "content",
  "body",
  "label",
  "placeholder",
  "explanation",
  "feedback",
  "answer",
  "option",
  "choices",
  "statement",
  "rationale",
  "hint",
  "caption",
  "alt",
  "arialabel",
]);

export interface ReadingTimeInput {
  body?: string | null;
  jsonBody?: Record<string, unknown> | null;
  lessonFormat?: string | null;
}

function collectText(value: unknown, buffer: string[]): void {
  if (Array.isArray(value)) {
    for (const element of value) {
      collectText(element, buffer);
    }
    return;
  }

  if (typeof value !== "object" || value === null) {
    return;
  }

  for (const [key, property] of Object.entries(value)) {
    if (typeof property === "string" && TEXT_PROPERTY_KEYS.has(key.toLowerCase())) {
      buffer.push(property);
    } else {
      collectText(property, buffer);
    }
  }
}

export function estimateReadingMinutes(input: ReadingTimeInput): number | null {
  if (input.lessonFormat === "Video") {
    return null;
  }

  const buffer: string[] = [];

  if (input.jsonBody !== null && input.jsonBody !== undefined) {
    collectText(input.jsonBody, buffer);
  }

  if (typeof input.body === "string" && input.body.trim() !== "") {
    const stripped = input.body
      .replace(/<[^>]+>/g, " ")
      .replace(/[#*_>[\]()!~|`-]/g, " ");
    buffer.push(stripped);
  }

  const words = buffer
    .join(" ")
    .trim()
    .split(/\s+/)
    .filter(Boolean).length;

  if (words === 0) {
    return null;
  }

  return Math.max(1, Math.ceil(words / WORDS_PER_MINUTE));
}
