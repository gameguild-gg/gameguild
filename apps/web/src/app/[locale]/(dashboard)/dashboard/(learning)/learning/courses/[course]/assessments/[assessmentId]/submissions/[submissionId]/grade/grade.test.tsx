import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { IdeHandle } from '@game-guild/emception-ui';

const routerMocks = vi.hoisted(() => ({
  push: vi.fn(),
}));

const ideMock = vi.hoisted(() => ({
  setFiles: vi.fn<(files: Array<{ path: string; content: string }>) => Promise<void>>(),
  getFiles: vi.fn<() => Promise<Array<{ path: string; content: string }>>>(),
  runTests: vi.fn<(plan: unknown) => Promise<unknown>>(),
}));

const gradeActionMock = vi.hoisted(() => ({
  gradeSubmission: vi.fn(),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => routerMocks,
}));

vi.mock('@game-guild/emception-ui', () => {
  const React = require('react') as typeof import('react');
  const Ide = React.forwardRef<IdeHandle>((_props, ref) => {
    React.useImperativeHandle(ref, () => ({
      setFiles: ideMock.setFiles,
      getFiles: ideMock.getFiles,
      runTests: ideMock.runTests,
    }));
    return React.createElement('div', { 'data-testid': 'mock-ide' });
  });
  Ide.displayName = 'Ide';
  const TestResultsPanel = ({
    report,
    maxScore,
  }: {
    report: { cases: { name: string; passed: boolean }[] };
    maxScore?: number;
  }) =>
    React.createElement(
      'div',
      { 'data-testid': 'mock-results' },
      `cases=${report.cases.length} max=${maxScore ?? '?'}`,
    );
  return { Ide, TestResultsPanel };
});

vi.mock('@/lib/learning/grade-action', () => ({
  gradeSubmission: gradeActionMock.gradeSubmission,
}));

import * as emceptionTesting from 'emception/testing';
import { GradeClient, mergeWorkspaceWithSubmission } from './grade-client';
import { gradeSubmission } from '@/lib/learning/grade-action';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/client';

// --- Fixtures ---------------------------------------------------------------

/** Two public stdio cases + one private. Equal weights → score = round(passed/total * max). */
const sampleAssignment: CodingAssignmentContent = {
  Type: 'coding-assignment',
  Version: 1,
  Environment: {
    Language: 'cpp',
    Tools: 'clang',
    AllowStudentCreateFiles: true,
  },
  Data: {
    Files: {
      '/home/user/main.cpp': {
        Content: '// workspace main',
        Encoding: 'text',
        Visibility: 'Public',
        Modifiable: true,
      },
      '/home/user/secret.cpp': {
        Content: '// secret solution — workspace only',
        Encoding: 'text',
        Visibility: 'Private',
        Modifiable: false,
      },
    },
  },
  Tests: {
    Public: [
      { kind: 'standard', Name: 'case1', Stdout: 'a', Weight: 1 },
      { kind: 'standard', Name: 'case2', Stdout: 'b', Weight: 1 },
    ],
    Private: [{ kind: 'standard', Name: 'case3', Stdout: 'c', Weight: 1 }],
  },
  Grading: { MaxScore: 100, PassingScore: 50 },
};

const samplePlan = {
  build: { sources: ['/home/user/main.cpp'] },
  cases: [
    { kind: 'stdio', expectedStdout: 'a', weight: 1 },
    { kind: 'stdio', expectedStdout: 'b', weight: 1 },
    { kind: 'stdio', expectedStdout: 'c', weight: 1 },
  ],
};

const sampleReport = {
  passed: 2,
  failed: 1,
  totalDurationMs: 80,
  cases: [
    { name: 'case1', passed: true, durationMs: 20 },
    { name: 'case2', passed: true, durationMs: 30 },
    { name: 'case3', passed: false, durationMs: 30, diagnostic: 'expected c, got 0' },
  ],
};

// 2/3 passing × maxScore=100 → 67
const EXPECTED_SCORE = 67;

const baseProps = {
  courseSlug: 'course-1',
  assessmentId: 'assessment-1',
  submissionId: 'submission-1',
  assignment: sampleAssignment,
  submittedFiles: [
    { path: '/home/user/main.cpp', content: '// student main' },
    { path: '/home/user/secret.cpp', content: '// ATTEMPT TO OVERRIDE PRIVATE' },
    { path: '/home/user/student.cpp', content: '// student-created file' },
  ],
  maxScore: 100,
  passingScore: 50,
  manifestUrl: '/cdn/manifest.json',
};

describe('mergeWorkspaceWithSubmission (criteria a, b)', () => {
  it('overrides modifiable workspace file with submission content (a)', () => {
    const merged = mergeWorkspaceWithSubmission(sampleAssignment, [
      { path: '/home/user/main.cpp', content: 'student override' },
    ]);
    const main = merged.find((f) => f.path === '/home/user/main.cpp');
    expect(main?.content).toBe('student override');
  });

  it('skips submission path that matches a Private workspace file (b)', () => {
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
    const merged = mergeWorkspaceWithSubmission(sampleAssignment, [
      { path: '/home/user/secret.cpp', content: 'STUDENT ATTEMPT' },
    ]);
    const secret = merged.find((f) => f.path === '/home/user/secret.cpp');
    expect(secret?.content).toBe('// secret solution — workspace only');
    expect(warnSpy).toHaveBeenCalledWith(
      expect.stringContaining('/home/user/secret.cpp'),
    );
    warnSpy.mockRestore();
  });

  it('adds student-created files not present in workspace', () => {
    const merged = mergeWorkspaceWithSubmission(sampleAssignment, [
      { path: '/home/user/new.cpp', content: 'new file' },
    ]);
    const added = merged.find((f) => f.path === '/home/user/new.cpp');
    expect(added?.content).toBe('new file');
  });
});

describe('GradeClient (criteria c, d, e, f)', () => {
  let buildTestPlanSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    ideMock.setFiles.mockReset();
    ideMock.getFiles.mockReset();
    ideMock.runTests.mockReset();
    routerMocks.push.mockReset();
    vi.mocked(gradeSubmission).mockReset();
    buildTestPlanSpy?.mockRestore();
    ideMock.setFiles.mockResolvedValue(undefined);
    ideMock.getFiles.mockResolvedValue([
      { path: '/home/user/main.cpp', content: '// student main' },
      { path: '/home/user/secret.cpp', content: '// secret solution — workspace only' },
      { path: '/home/user/student.cpp', content: '// student-created file' },
    ]);
    buildTestPlanSpy = vi.spyOn(emceptionTesting, 'buildTestPlan');
    buildTestPlanSpy.mockReturnValue({
      plan: samplePlan,
      generatedFiles: [],
    });
  });

  it('seeds IDE with merged workspace (Private file NOT overridden) (a, b)', async () => {
    render(<GradeClient {...baseProps} />);

    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());
    const seeded = ideMock.setFiles.mock.calls[0][0] as Array<{
      path: string;
      content: string;
    }>;
    const byPath = Object.fromEntries(seeded.map((f) => [f.path, f.content]));

    // (a) submission overrides modifiable workspace file
    expect(byPath['/home/user/main.cpp']).toBe('// student main');
    // (b) Private file keeps workspace content (NOT student override)
    expect(byPath['/home/user/secret.cpp']).toBe(
      '// secret solution — workspace only',
    );
    // Student-created file added.
    expect(byPath['/home/user/student.cpp']).toBe('// student-created file');
  });

  it('Run Tests invokes buildTestPlan + re-seeds IDE with harness + calls runTests (c)', async () => {
    buildTestPlanSpy.mockReturnValue({
      plan: samplePlan,
      generatedFiles: [
        { path: '/home/user/functional_2_test.cpp', content: '// harness' },
      ],
    });
    ideMock.runTests.mockResolvedValue(sampleReport);

    render(<GradeClient {...baseProps} />);
    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());
    // Reset to isolate the in-handler setFiles call from the seed call.
    ideMock.setFiles.mockClear();

    fireEvent.click(screen.getByTestId('grade-button'));

    await waitFor(() => {
      expect(buildTestPlanSpy).toHaveBeenCalledWith(sampleAssignment, {
        mode: 'full',
      });
    });
    await waitFor(() => {
      expect(ideMock.runTests).toHaveBeenCalledWith(samplePlan);
    });
    // Re-seed merged the harness file alongside the existing workspace.
    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());
    const reSeeded = ideMock.setFiles.mock.calls[0][0] as Array<{
      path: string;
      content: string;
    }>;
    const harness = reSeeded.find((f) => f.path === '/home/user/functional_2_test.cpp');
    expect(harness?.content).toBe('// harness');
    // Existing workspace file preserved (replace semantics still includes it).
    expect(reSeeded.some((f) => f.path === '/home/user/main.cpp')).toBe(true);
  });

  it('Run Tests computes score + shows TestResultsPanel + enables Confirm (e)', async () => {
    ideMock.runTests.mockResolvedValue(sampleReport);

    render(<GradeClient {...baseProps} />);
    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());

    fireEvent.click(screen.getByTestId('grade-button'));

    await waitFor(() => {
      expect(screen.getByTestId('grade-score').textContent).toContain(String(EXPECTED_SCORE));
    });
    expect(screen.getByTestId('grade-result')).toBeInTheDocument();
    expect(screen.getByTestId('mock-results')).toBeInTheDocument();
    expect(screen.getByTestId('confirm-grade-button')).not.toBeDisabled();
  });

  it('Confirm Grade composes Feedback from overall + per-file + auto-feedback (d, e)', async () => {
    ideMock.runTests.mockResolvedValue(sampleReport);
    vi.mocked(gradeSubmission).mockResolvedValue({
      success: true,
      data: { submissionId: 'submission-1' },
    });

    render(<GradeClient {...baseProps} />);
    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());
    fireEvent.click(screen.getByTestId('grade-button'));
    await waitFor(() => screen.getByTestId('grade-result'));

    // (d) Fill overall + one per-file comment.
    fireEvent.change(screen.getByTestId('overall-comment'), {
      target: { value: 'Strong submission.' },
    });
    const mainComment = screen.getByTestId('comment-/home/user/main.cpp') as HTMLTextAreaElement;
    fireEvent.change(mainComment, { target: { value: 'Rename loop counter.' } });

    fireEvent.click(screen.getByTestId('confirm-grade-button'));

    await waitFor(() => expect(gradeSubmission).toHaveBeenCalledTimes(1));
    const call = vi.mocked(gradeSubmission).mock.calls[0][0];
    expect(call.submissionId).toBe('submission-1');
    expect(call.score).toBe(EXPECTED_SCORE);
    // Composed markdown asserts actual content from all three sources.
    expect(call.feedback).toContain('## Overall');
    expect(call.feedback).toContain('Strong submission.');
    expect(call.feedback).toContain('## Auto-generated feedback');
    expect(call.feedback).toContain(`Score: ${EXPECTED_SCORE}/100`);
    expect(call.feedback).toContain('## Per-file comments');
    expect(call.feedback).toContain('### /home/user/main.cpp');
    expect(call.feedback).toContain('Rename loop counter.');
    expect(call.feedback).toContain('expected c, got 0');
    expect(routerMocks.push).toHaveBeenCalled();
  });

  it('Compile error in student code → runTests resolves a ToolResult, page shows error from report, no rejection thrown (f, Metis #33)', async () => {
    // Metis #33: tool failure resolves, does NOT reject. The engine returns a
    // ToolResult with exitCode != 0 + non-empty stderr; the testing engine
    // surfaces it as a fully-failing TestReport (all cases fail with a
    // stderr-bearing diagnostic). The page MUST NOT throw — it shows the
    // report so the instructor can review.
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
    const compileErrorReport = {
      passed: 0,
      failed: 3,
      totalDurationMs: 60,
      cases: [
        {
          name: 'case1',
          passed: false,
          durationMs: 20,
          diagnostic: 'main.cpp:5:12: error: expected \';\' after expression',
        },
        { name: 'case2', passed: false, durationMs: 20, diagnostic: 'compile failed' },
        { name: 'case3', passed: false, durationMs: 20, diagnostic: 'compile failed' },
      ],
    };
    ideMock.runTests.mockResolvedValue(compileErrorReport);

    render(<GradeClient {...baseProps} />);
    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());
    fireEvent.click(screen.getByTestId('grade-button'));

    // runTests resolved — no grade-error alert (Metis #33: NOT a rejection).
    await waitFor(() => {
      expect(screen.getByTestId('grade-score').textContent).toContain('0');
    });
    expect(screen.queryByTestId('grade-error')).not.toBeInTheDocument();
    // Report IS shown — instructor reviews the failing cases.
    expect(screen.getByTestId('grade-result')).toBeInTheDocument();
    // The diagnostic was carried into the report; we can verify via the mock
    // panel's "cases=3" badge.
    expect(screen.getByTestId('mock-results').textContent).toContain('cases=3');
    // Confirm-grade was never auto-pressed — instructor must review first.
    expect(gradeSubmission).not.toHaveBeenCalled();
    // runTests was called and resolved (the test reaching this point proves it).
    expect(ideMock.runTests).toHaveBeenCalled();
    warnSpy.mockRestore();
  });

  it('Confirm button stays disabled until Run Tests succeeds', () => {
    render(<GradeClient {...baseProps} />);
    expect(screen.getByTestId('confirm-grade-button')).toBeDisabled();
  });

  it('gradeSubmission failure surfaces error + stays reviewable', async () => {
    ideMock.runTests.mockResolvedValue(sampleReport);
    vi.mocked(gradeSubmission).mockResolvedValue({
      success: false,
      error: 'Forbidden',
    });

    render(<GradeClient {...baseProps} />);
    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());
    fireEvent.click(screen.getByTestId('grade-button'));
    await waitFor(() => screen.getByTestId('grade-result'));
    fireEvent.click(screen.getByTestId('confirm-grade-button'));

    await waitFor(() => {
      expect(screen.getByTestId('grade-error')).toHaveTextContent('Forbidden');
    });
    expect(screen.getByTestId('grade-result')).toBeInTheDocument();
    expect(screen.getByTestId('confirm-grade-button')).not.toBeDisabled();
  });
});
