import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { IdeHandle } from '@game-guild/emception-ui';

const routerMocks = vi.hoisted(() => ({
  push: vi.fn(),
}));

const ideMock = vi.hoisted(() => ({
  setFiles: vi.fn<(files: Array<{ path: string; content: string }>) => Promise<void>>(),
  runTests: vi.fn<(plan: unknown) => Promise<unknown>>(),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => routerMocks,
}));

vi.mock('@game-guild/emception-ui', () => {
  const React = require('react') as typeof import('react');
  const Ide = React.forwardRef<IdeHandle>((_props, ref) => {
    React.useImperativeHandle(ref, () => ({
      setFiles: ideMock.setFiles,
      runTests: ideMock.runTests,
    }));
    return React.createElement('div', { 'data-testid': 'mock-ide' });
  });
  Ide.displayName = 'Ide';
  return {
    Ide,
    TestResultsPanel: ({ report, maxScore }: { report: { cases: { name: string; passed: boolean }[] }; maxScore?: number }) =>
      React.createElement('div', { 'data-testid': 'mock-results' }, `cases=${report.cases.length} max=${maxScore ?? '?'}`),
  };
});

vi.mock('@/lib/learning/grade-action', () => ({
  gradeSubmission: vi.fn(),
}));

import { GradeClient } from './grade-client';
import { gradeSubmission } from '@/lib/learning/grade-action';

const sampleReport = {
  passed: 2,
  failed: 1,
  totalDurationMs: 80,
  cases: [
    { name: 'case1', passed: true, durationMs: 20 },
    { name: 'case2', passed: true, durationMs: 30 },
    { name: 'case3', passed: false, durationMs: 30, diagnostic: 'expected 42, got 0' },
  ],
};

// 2 passing of 3, equal weights, maxScore=100 → round(2/3*100) = 67
const sampleTestPlan = {
  cases: [
    { kind: 'stdio', expectedStdout: 'a', weight: 1 },
    { kind: 'stdio', expectedStdout: 'b', weight: 1 },
    { kind: 'stdio', expectedStdout: 'c', weight: 1 },
  ],
};

const baseProps = {
  courseSlug: 'course-1',
  assessmentId: 'assessment-1',
  submissionId: 'submission-1',
  initialFiles: [{ path: '/home/user/main.cpp', content: 'int main(){}' }],
  workspaceConfig: null,
  testPlan: sampleTestPlan,
  maxScore: 100,
  passingScore: 50,
  manifestUrl: '/cdn/manifest.json',
};

describe('GradeClient', () => {
  beforeEach(() => {
    ideMock.setFiles.mockReset();
    ideMock.runTests.mockReset();
    routerMocks.push.mockReset();
    vi.mocked(gradeSubmission).mockReset();
    ideMock.setFiles.mockResolvedValue(undefined);
  });

  it('seeds IDE with initialFiles on mount', async () => {
    render(<GradeClient {...baseProps} />);
    await waitFor(() => {
      expect(ideMock.setFiles).toHaveBeenCalledWith(baseProps.initialFiles);
    });
  });

  it('Grade click runs tests and shows computed score + results', async () => {
    ideMock.runTests.mockResolvedValue(sampleReport);
    render(<GradeClient {...baseProps} />);

    const gradeBtn = screen.getByTestId('grade-button');
    expect(gradeBtn).not.toBeDisabled();

    fireEvent.click(gradeBtn);

    await waitFor(() => {
      expect(screen.getByTestId('grade-score').textContent).toContain('67');
    });
    expect(ideMock.runTests).toHaveBeenCalledWith(sampleTestPlan);
    expect(screen.getByTestId('grade-result')).toBeInTheDocument();
    expect(screen.getByTestId('mock-results')).toBeInTheDocument();
    expect(screen.getByTestId('confirm-grade-button')).not.toBeDisabled();
  });

  it('Confirm click posts grade with exact score + feedback substring', async () => {
    ideMock.runTests.mockResolvedValue(sampleReport);
    vi.mocked(gradeSubmission).mockResolvedValue({
      success: true,
      data: { submissionId: 'submission-1' },
    });

    render(<GradeClient {...baseProps} />);

    fireEvent.click(screen.getByTestId('grade-button'));
    await waitFor(() => screen.getByTestId('grade-result'));
    fireEvent.click(screen.getByTestId('confirm-grade-button'));

    await waitFor(() => {
      expect(gradeSubmission).toHaveBeenCalledTimes(1);
    });
    const call = vi.mocked(gradeSubmission).mock.calls[0][0];
    expect(call.submissionId).toBe('submission-1');
    // Adversarial: assert actual score number, not just "called"
    expect(call.score).toBe(67);
    // Adversarial: assert actual feedback content (markdown substring), not just "called"
    expect(call.feedback).toContain('Score: 67/100');
    expect(call.feedback).toContain('case3');
    expect(call.feedback).toContain('expected 42, got 0');
    expect(routerMocks.push).toHaveBeenCalled();
  });

  it('Confirm button stays disabled until Grade succeeds', () => {
    render(<GradeClient {...baseProps} />);
    expect(screen.getByTestId('confirm-grade-button')).toBeDisabled();
    // Idle → Confirm disabled even after error
  });

  it('runTests rejects → error shown + Confirm stays disabled', async () => {
    ideMock.runTests.mockRejectedValue(new Error('compile error: missing semicolon'));
    render(<GradeClient {...baseProps} />);

    fireEvent.click(screen.getByTestId('grade-button'));

    await waitFor(() => {
      expect(screen.getByTestId('grade-error')).toHaveTextContent('compile error: missing semicolon');
    });
    expect(screen.queryByTestId('grade-result')).not.toBeInTheDocument();
    expect(screen.getByTestId('confirm-grade-button')).toBeDisabled();
    expect(gradeSubmission).not.toHaveBeenCalled();
  });

  it('gradeSubmission failure surfaces error + stays in ready state', async () => {
    ideMock.runTests.mockResolvedValue(sampleReport);
    vi.mocked(gradeSubmission).mockResolvedValue({
      success: false,
      error: 'Forbidden',
    });

    render(<GradeClient {...baseProps} />);

    fireEvent.click(screen.getByTestId('grade-button'));
    await waitFor(() => screen.getByTestId('grade-result'));
    fireEvent.click(screen.getByTestId('confirm-grade-button'));

    await waitFor(() => {
      expect(screen.getByTestId('grade-error')).toHaveTextContent('Forbidden');
    });
    // Adversarial: stale_state — score persists, instructor can retry Confirm
    expect(screen.getByTestId('grade-result')).toBeInTheDocument();
    expect(screen.getByTestId('confirm-grade-button')).not.toBeDisabled();
  });
});
