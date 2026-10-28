import '@testing-library/jest-dom/vitest';
import type { AnchorHTMLAttributes, ReactNode } from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { LearningAssessmentsGradingQueue } from '@game-guild/client';
import type { AssessmentSubmissionRuntimeViewV1 } from '@game-guild/grading';

const actions = vi.hoisted(() => ({ get: vi.fn(), resolve: vi.fn(), regrade: vi.fn() }));
const router = vi.hoisted(() => ({ refresh: vi.fn(), replace: vi.fn() }));
globalThis.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};
vi.mock('@/lib/learning/grading-runtime-actions', () => ({
  getRuntimeSubmission: actions.get,
  resolveRuntimeInstructorReview: actions.resolve,
  regradeRuntimeSubmission: actions.regrade,
  releaseRuntimeSubmission: vi.fn(),
  scheduleRuntimeSubmissionRelease: vi.fn(),
  cancelRuntimeSubmissionReleaseSchedule: vi.fn(),
}));
vi.mock('@/i18n/navigation', () => ({
  useRouter: () => router,
  usePathname: () => '/speedgrader/assessments/assessment-1',
  Link: ({ href, children, ...rest }: AnchorHTMLAttributes<HTMLAnchorElement>) => (
    <a href={href} {...rest}>{children}</a>
  ),
}));
vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams('course=course-1'),
}));
// Editor boot and resize geometry do not participate in saving a review or
// projecting the queue. Keep the actual workspace, shell and grading panel.
vi.mock('./submission-viewer', () => ({ SubmissionViewer: () => <div>Submitted work</div> }));
vi.mock('@game-guild/ui/components/resizable', () => ({
  ResizablePanelGroup: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  ResizablePanel: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  ResizableHandle: () => <div role="separator" />,
}));

import { SpeedgraderWorkspace } from './speedgrader-workspace';

function submission(final = false): AssessmentSubmissionRuntimeViewV1 {
  return {
    submissionId: 'submission-1', assessmentId: 'assessment-1',
    definitionRevisionId: 'revision-1', enrollmentId: 'enrollment-1',
    courseGroupId: null, attemptNumber: 1, draftVersion: 0,
    version: final ? 5 : 4, status: final ? 'graded' : 'submitted',
    startedAt: '2026-10-01T09:00:00Z', submittedAt: '2026-10-01T10:00:00Z',
    submittedByUserId: 'student-1', contentCompleted: false,
    execution: {
      executionId: 'execution-1', definitionRevisionId: 'revision-1',
      context: 'official-submission', executionSnapshotHash: 'a'.repeat(64),
      deliveryHash: 'b'.repeat(64),
      delivery: {
        schemaVersion: 1, definitionRevisionId: 'revision-1',
        executionSnapshotHash: 'a'.repeat(64), itemOrder: ['q1'],
        items: { q1: { adapterKey: 'quiz-assessment-type', adapterVersion: '1', learnerPayload: { entry: { stem: 'Review this answer' } } } },
      },
      itemMaxScores: { q1: 100 }, submittedResponse: null,
      status: final ? 'completed' : 'awaitingReview', activeRoundId: 'round-1',
      requiresInstructorReview: !final, released: false, learnerVisibleResult: null,
      instructorVisibleResult: {
        schemaVersion: 1, state: final ? 'final' : 'partial',
        score: final ? 100 : null, maxScore: 100, evidenceRefs: [], feedback: null,
        items: [{ itemId: 'q1', state: final ? 'graded' : 'pending', score: final ? 100 : null,
          maxScore: 100, evidenceRefs: [], reviewMethod: 'InstructorReview',
          handlerKey: 'instructor-review', handlerVersion: '1' }],
      },
      history: final ? [{ roundId: 'round-1', roundNumber: 1, reason: 'initial',
        reasonDetail: null, initiatedByActorId: null, status: 'finalized',
        startedAt: '2026-10-01T10:00:00Z', finalizedAt: '2026-10-01T10:10:00Z',
        result: null, released: false, releasedAt: null, releaseSchedule: null }] : [],
    },
  };
}

function queue(needsGrading = 1): LearningAssessmentsGradingQueue {
  return {
    assessment: { id: 'assessment-1', title: 'Assessment', maxScore: 100 },
    needsGrading, total: needsGrading || 1,
    items: Array.from({ length: needsGrading || 1 }, (_, index) => ({
      submissionId: `submission-${index + 1}`, canonicalSubmissionId: `submission-${index + 1}`,
      displayName: `Student ${index + 1}`, attemptNumber: 1, isGroup: false,
      status: needsGrading ? 'Submitted' : 'Graded',
      activeGradeRoundId: `round-${index + 1}`, requiresStaffReview: Boolean(needsGrading),
    })),
  };
}

function renderQueue(value = queue()) {
  return render(<SpeedgraderWorkspace queue={value} assessmentId="assessment-1" courseSlug="course-1" initialIndex={0} />);
}

async function submitReview() {
  const user = userEvent.setup();
  await user.type(await screen.findByTestId('item-score-q1'), '1');
  await user.click(screen.getByTestId('resolve-instructor-review'));
}

describe('SpeedGrader confirmed review consistency with a pending route refresh', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // A refresh request is not a new queue snapshot. It deliberately supplies
    // no new server props, modeling a deferred RSC response without a reload.
    actions.get.mockResolvedValue({ success: true, data: submission() });
  });

  it('updates the visible count only after the server confirms the review', async () => {
    let confirm!: (value: { success: true; data: AssessmentSubmissionRuntimeViewV1 }) => void;
    actions.resolve.mockReturnValue(new Promise((resolve) => { confirm = resolve; }));
    renderQueue();
    await submitReview();
    expect(screen.getByTestId('needs-grading-badge')).toHaveTextContent('1 to grade');
    confirm({ success: true, data: submission(true) });
    expect(await screen.findByTestId('result-release-status')).toHaveTextContent('Ready to release');
    await waitFor(() => expect(router.refresh).toHaveBeenCalled());
    await waitFor(() => expect(screen.getByTestId('needs-grading-badge')).toHaveTextContent('0 to grade'));
  });

  it('keeps the other student in the pending count after one confirmed review', async () => {
    actions.resolve.mockResolvedValue({ success: true, data: submission(true) });
    renderQueue(queue(2));
    await submitReview();
    expect(await screen.findByTestId('result-release-status')).toHaveTextContent('Ready to release');
    await waitFor(() => expect(screen.getByTestId('needs-grading-badge')).toHaveTextContent('1 to grade'));
  });

  it('preserves the count when saving the review fails', async () => {
    actions.resolve.mockResolvedValue({ success: false, error: 'Review was not saved.' });
    renderQueue();
    await submitReview();
    expect(await screen.findByRole('alert')).toHaveTextContent('Review was not saved.');
    expect(screen.getByTestId('needs-grading-badge')).toHaveTextContent('1 to grade');
    expect(router.refresh).not.toHaveBeenCalled();
  });

  it('uses confirmed operational review work even when the delivery status is Graded', async () => {
    const next = submission();
    next.status = 'graded';
    actions.resolve.mockResolvedValue({ success: true, data: next });
    renderQueue();
    await submitReview();
    await waitFor(() => expect(router.refresh).toHaveBeenCalled());
    expect(screen.getByTestId('needs-grading-badge')).toHaveTextContent('1 to grade');
  });

  it('shows confirmed review work from a new grading round while refresh is pending', async () => {
    actions.get.mockResolvedValue({ success: true, data: submission(true) });
    const next = submission();
    next.execution.activeRoundId = 'round-2';
    next.version = 6;
    actions.regrade.mockResolvedValue({ success: true, data: next });
    renderQueue(queue(0));
    const user = userEvent.setup();
    await user.type(await screen.findByPlaceholderText('Reason for opening a new round'), 'New evidence');
    await user.click(screen.getByRole('button', { name: 'Start regrade' }));
    await waitFor(() => expect(actions.regrade).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(screen.getByTestId('needs-grading-badge')).toHaveTextContent('1 to grade'));
  });
});
