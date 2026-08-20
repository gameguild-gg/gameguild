// TEMPORARY diagnostic — delete before done. Counts Ide mount/unmount across
// lifecycle events that occur on the real page.
import '@testing-library/jest-dom/vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { IdeHandle } from '@game-guild/emception-ui';
import type { LearningAssessmentsGradingQueue } from '@game-guild/client';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/client';

const actionsMock = vi.hoisted(() => ({
  fetchSubmission: vi.fn(),
  fetchPeerReviews: vi.fn(),
}));

const ideStats = vi.hoisted(() => ({ mounts: 0, unmounts: 0 }));

vi.mock('./speedgrader-actions', () => ({
  fetchSubmissionAction: actionsMock.fetchSubmission,
  fetchPeerReviewsAction: actionsMock.fetchPeerReviews,
}));

vi.mock('@game-guild/emception-ui', () => {
  const React = require('react') as typeof import('react');
  const Ide = React.forwardRef<IdeHandle, { manifestUrl?: string }>((props, ref) => {
    React.useEffect(() => {
      ideStats.mounts++;
      return () => {
        ideStats.unmounts++;
      };
    }, []);
    React.useImperativeHandle(ref, () => ({
      setFiles: vi.fn().mockResolvedValue(undefined),
      getFiles: vi.fn().mockResolvedValue([]),
      runTests: vi.fn().mockResolvedValue({}),
    }));
    return React.createElement('div', { 'data-testid': 'mock-ide' });
  });
  const TestResultsPanel = () => React.createElement('div', { 'data-testid': 'mock-results' });
  const ASSIGNMENT_SAMPLES = {
    cpp: {
      workspaceConfig: {
        id: 'cpp',
        layout: { activeFile: '/user/main.cpp', openTabs: [{ path: '/user/main.cpp', group: 'main' }] },
      },
    },
  };
  return { Ide, TestResultsPanel, ASSIGNMENT_SAMPLES };
});

const navMock = vi.hoisted(() => ({
  replace: vi.fn(),
  refresh: vi.fn(),
  pathname: '/speedgrader/assessments/a1',
  params: new URLSearchParams('course=c1&nav=0'),
}));

vi.mock('next/navigation', () => ({
  useSearchParams: () => navMock.params,
}));

vi.mock('@/i18n/navigation', () => ({
  useRouter: () => ({ replace: navMock.replace, push: vi.fn(), refresh: navMock.refresh }),
  usePathname: () => navMock.pathname,
  Link: ({ children }: { children: React.ReactNode }) => children,
}));

vi.mock('@game-guild/ui/components/resizable', () => ({
  ResizablePanelGroup: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  ResizablePanel: ({ children }: { children?: React.ReactNode }) => <div>{children}</div>,
  ResizableHandle: () => <div role="separator" />,
}));

global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

import { SpeedgraderWorkspace } from './speedgrader-workspace';

const assignment: CodingAssignmentContent = {
  Type: 'coding-assignment',
  Version: 1,
  Environment: { Language: 'cpp', Tools: 'clang', AllowStudentCreateFiles: true },
  Data: {
    Files: {
      '/home/user/main.cpp': { Content: '// workspace', Encoding: 'text', Visibility: 'Public', Modifiable: true },
    },
  },
  Tests: { Public: [], Private: [] },
  Grading: { MaxScore: 100 },
};

function makeQueue(): LearningAssessmentsGradingQueue {
  return {
    assessment: { id: 'a1', title: 'T', maxScore: 100, hasRubric: false, gradingMethods: 'Manual' },
    needsGrading: 2,
    items: [
      { submissionId: 'sub-1', displayName: 'A', attemptNumber: 1, status: 'Submitted' },
      { submissionId: 'sub-2', displayName: 'B', attemptNumber: 1, status: 'Submitted' },
    ],
  } as unknown as LearningAssessmentsGradingQueue;
}

const codeSubmission = {
  ok: true,
  submission: {
    id: 'sub-1',
    attemptNumber: 1,
    status: 'Submitted',
    submittedModalities: 'Code',
    codePayload: JSON.stringify([{ path: '/home/user/main.cpp', content: '// student' }]),
  },
};

function setup() {
  return render(
    <SpeedgraderWorkspace
      queue={makeQueue()}
      assessmentId="a1"
      courseSlug="c1"
      initialIndex={0}
      codingAssignment={assignment}
      manifestUrl="/emception/manifest.json"
    />,
  );
}

describe('DIAGNOSTIC ide mount lifecycle', () => {
  it('scenario A: initial load + fetch resolve', async () => {
    actionsMock.fetchSubmission.mockResolvedValue(codeSubmission);
    actionsMock.fetchPeerReviews.mockResolvedValue({ ok: true, reviews: [] });
    setup();
    await waitFor(() => expect(screen.getByTestId('mock-ide')).toBeInTheDocument());
    // let any trailing state updates flush
    await new Promise((r) => setTimeout(r, 50));
    console.log('A:', { ...ideStats });
    expect(ideStats.mounts).toBe(1);
  });

  it('scenario B: queue prop identity churn (router.refresh equivalent)', async () => {
    ideStats.mounts = 0;
    ideStats.unmounts = 0;
    actionsMock.fetchSubmission.mockResolvedValue(codeSubmission);
    actionsMock.fetchPeerReviews.mockResolvedValue({ ok: true, reviews: [] });
    const { rerender } = setup();
    await waitFor(() => expect(screen.getByTestId('mock-ide')).toBeInTheDocument());

    // fresh identities for every prop, same values — mirrors a server re-render
    rerender(
      <SpeedgraderWorkspace
        queue={makeQueue()}
        assessmentId="a1"
        courseSlug="c1"
        initialIndex={0}
        codingAssignment={JSON.parse(JSON.stringify(assignment))}
        manifestUrl="/emception/manifest.json"
      />,
    );
    await new Promise((r) => setTimeout(r, 50));
    console.log('B:', { ...ideStats });
    expect(ideStats.mounts).toBe(1);
  });

  it('scenario C: nav change (prev/next submission)', async () => {
    ideStats.mounts = 0;
    ideStats.unmounts = 0;
    actionsMock.fetchSubmission.mockResolvedValue(codeSubmission);
    actionsMock.fetchPeerReviews.mockResolvedValue({ ok: true, reviews: [] });
    setup();
    await waitFor(() => expect(screen.getByTestId('mock-ide')).toBeInTheDocument());

    // next button → goTo(1) → setRawIndex(1) + router.replace
    const { fireEvent } = await import('@testing-library/react');
    fireEvent.click(screen.getByRole('button', { name: 'Next submission' }));
    actionsMock.fetchSubmission.mockResolvedValue({
      ok: true,
      submission: { ...codeSubmission.submission, id: 'sub-2' },
    });
    await new Promise((r) => setTimeout(r, 50));
    console.log('C:', { ...ideStats });
  });
});
