/**
 * Pure markdown composer for the instructor's final Feedback column.
 *
 * Concatenates the optional overall comment + the auto-generated scoring
 * feedback (from `lib/emception/scoring.ts` `formatFeedback`) + optional
 * per-file comments into a single markdown block posted to
 * `POST /v1.0/assessments/submissions/{id}/grade`.
 */

export interface ComposeFeedbackOptions {
  /** Free-text overall comment written by the instructor. */
  overallComment?: string;
  /** Path → instructor comment. Defaults to `{}`. */
  perFileComments?: Record<string, string>;
  /** Pre-formatted markdown from `formatFeedback(report, score)`. */
  autoFeedback: string;
}

/**
 * Compose a 3-section markdown block:
 *
 *   ## Overall
 *   <overallComment or "No overall comment">
 *
 *   ## Auto-generated feedback
 *   <autoFeedback>
 *
 *   ## Per-file comments
 *   ### <path>
 *   <comment or "No comment">
 *
 * Per-file entries are emitted in stable (lexicographic) path order so the
 * output is deterministic for snapshot/review comparison.
 */
export function composeFeedback(opts: ComposeFeedbackOptions): string {
  const overall = opts.overallComment?.trim() || 'No overall comment';
  const perFile = opts.perFileComments ?? {};
  const paths = Object.keys(perFile).sort();
  const perFileBlock =
    paths.length === 0
      ? 'No per-file comments.'
      : paths
          .map((p) => `### ${p}\n${perFile[p]?.trim() || 'No comment'}`)
          .join('\n\n');

  return [
    '## Overall',
    overall,
    '',
    '## Auto-generated feedback',
    opts.autoFeedback,
    '',
    '## Per-file comments',
    perFileBlock,
  ].join('\n');
}
