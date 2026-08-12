// Client-safe helpers for the AssessmentGradingMethod [Flags] enum.
// Kept here (not in queries/assessments.ts) because queries/assessments.ts
// imports auth.ts → next/headers, which would violate the client/server
// boundary if pulled into a "use client" module.

// ponytail: [Flags] enum serializes as comma-separated string ("PeerReview,AutoGraded"), not a TS enum
export const ASSESSMENT_GRADING_METHOD_FLAGS = [
  "PeerReview",
  "AIGraded",
  "AutoGraded",
  "InstructorGraded",
] as const;

export type AssessmentGradingMethodFlag =
  (typeof ASSESSMENT_GRADING_METHOD_FLAGS)[number];

export function parseGradingMethods(
  value: string | null | undefined,
): Set<AssessmentGradingMethodFlag> {
  const set = new Set<AssessmentGradingMethodFlag>();
  for (const part of (value ?? "").split(",")) {
    const trimmed = part.trim();
    if (
      (ASSESSMENT_GRADING_METHOD_FLAGS as readonly string[]).includes(trimmed)
    ) {
      set.add(trimmed as AssessmentGradingMethodFlag);
    }
  }
  return set;
}

export function serializeGradingMethods(
  flags: Iterable<AssessmentGradingMethodFlag>,
): string {
  return [...flags].join(",");
}
