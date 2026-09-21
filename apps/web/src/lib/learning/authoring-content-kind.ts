import type { LearningCoursesProgramContentType } from "@game-guild/client";

/**
 * The authoring editor surface The workspace renders for a content item.
 *
 * There are exactly three dedicated authoring editors. Everything else is
 * authored as a lesson. Keeping this as a single derived value (instead of
 * scattered `isCode` / `isQuiz` / `isLesson` booleans computed in several places
 * and independently patched) is what prevents the type-discrimination drift that
 * showed up as a stream of "narrow/harden/guard content type" commits.
 */
export type AuthoringContentKind = "code" | "quiz" | "lesson";

/**
 * Normalize a legacy professor-facing type the same way the backend does
 * (`ProgramContentMappingExtensions.NormalizeProfessorFacingType`).
 *
 * The persisted `ProgramContentType` enum still carries the legacy `Page` and
 * `Challenge` values; every professor-facing path maps them to `Lesson` and
 * `Assignment`. Reflecting that here means the client discriminator never sees a
 * stale `Page`/`Challenge` and can hand off to the editor deterministically.
 */
export function normalizeProfessorFacingType(
  type: string | null | undefined,
): LearningCoursesProgramContentType | null {
  switch (type) {
    case "Page":
      return "Lesson";
    case "Challenge":
      return "Assignment";
    case null:
    case undefined:
      return null;
    default:
      return type as LearningCoursesProgramContentType;
  }
}

/**
 * Resolve which dedicated editor to open for a content item.
 *
 * The authoritative source is the published content tree type (`itemType`) — a
 * draft payload can carry a stale `Lesson` type from before the normalized
 * writer existed, but the published type is always current. The payload type is
 * consulted only as a fallback so a freshly-drafted legacy item still opens the
 * right editor until its first save normalizes it.
 *
 * Behavior is intentionally identical to the previous OR-joined discriminator:
 * - `Code` wins over everything else.
 * - `Questionnaire` (when not `Code`) selects the quiz editor.
 * - Anything else is authored as a lesson.
 */
export function resolveAuthoringContentKind(
  itemType: string | null | undefined,
  payloadType: string | null | undefined,
): AuthoringContentKind {
  const item = normalizeProfessorFacingType(itemType);
  const payload = normalizeProfessorFacingType(payloadType);
  if (item === "Code" || payload === "Code") return "code";
  if (item === "Questionnaire" || payload === "Questionnaire") return "quiz";
  return "lesson";
}
