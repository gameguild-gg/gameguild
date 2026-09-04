export const QUIZ_POINTS_PATTERN = /^\d{8}\.\d{4}$/;
export const DEFAULT_QUIZ_POINTS = "00000001.0000";

export type QuizPoints = string;

export function parseQuizPoints(value: unknown): QuizPoints {
  if (typeof value !== "string" || !QUIZ_POINTS_PATTERN.test(value)) {
    throw new TypeError("Quiz points must match ^\\d{8}\\.\\d{4}$.");
  }
  return value;
}
