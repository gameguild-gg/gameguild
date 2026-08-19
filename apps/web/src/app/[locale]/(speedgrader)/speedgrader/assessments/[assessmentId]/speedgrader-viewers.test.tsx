import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { IdeHandle } from '@game-guild/emception-ui';
import type { LearningAssessmentsAssessmentSubmission } from '@game-guild/client';
import { parseSubmittedModalities, SubmissionViewer } from './submission-viewer';
import { CodeGraderPanel, mergeWorkspaceWithSubmission } from './code-grader-panel';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/client';
import * as emceptionTesting from 'emception/testing';

const actionsMock = vi.hoisted(() => ({
  fetchSubmission: vi.fn(),
}));

const ideMock = vi.hoisted(() => {
  const setFiles = vi.fn<(files: Array<{ path: string; content: string }>) => Promise<void>>();
  setFiles.mockResolvedValue(undefined);
  const getFiles = vi.fn<() => Promise<Array<{ path: string; content: string }>>>();
  getFiles.mockResolvedValue([]);
  const runTests = vi.fn<(plan: unknown) => Promise<unknown>>();
  return { setFiles, getFiles, runTests };
});

vi.mock('./speedgrader-actions', () => ({
  fetchSubmissionAction: actionsMock.fetchSubmission,
}));

vi.mock('@game-guild/emception-ui', () => {
  const React = require('react') as typeof import('react');
  const Ide = React.forwardRef<IdeHandle, { manifestUrl?: string }>((props, ref) => {
    React.useImperativeHandle(ref, () => ({
      setFiles: ideMock.setFiles,
      getFiles: ideMock.getFiles,
      runTests: ideMock.runTests,
    }));
    return React.createElement('div', { 'data-testid': 'mock-ide', 'data-manifest-url': props.manifestUrl });
  });
  const TestResultsPanel = ({ report, maxScore }: { report: { cases: { name: string; passed: boolean }[] }; maxScore?: number }) =>
    React.createElement('div', { 'data-testid': 'mock-results' }, `cases=${report.cases.length} max=${maxScore ?? '?'}`);
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

// --- Fixtures ---------------------------------------------------------------

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
  Grading: { MaxScore: 100 },
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
    {
      name: 'case3',
      passed: false,
      durationMs: 30,
      diagnostic: 'expected c, got 0',
    },
  ],
};

function makeSubmission(overrides: Partial<LearningAssessmentsAssessmentSubmission> = {}): LearningAssessmentsAssessmentSubmission {
  return {
    id: 'sub-1',
    attemptNumber: 1,
    status: 'Submitted',
    ...overrides,
  };
}

function resolveSubmission(overrides: Partial<LearningAssessmentsAssessmentSubmission>) {
  actionsMock.fetchSubmission.mockResolvedValue({
    ok: true,
    submission: makeSubmission(overrides),
  });
}

// --- parseSubmittedModalities ----------------------------------------------

describe('parseSubmittedModalities', () => {
  it('parses comma-separated flag names', () => {
    const set = parseSubmittedModalities('Text, Code');
    expect(set.has('Text')).toBe(true);
    expect(set.has('Code')).toBe(true);
    expect(set.size).toBe(2);
  });

  it('parses a single name', () => {
    expect(parseSubmittedModalities('Url').has('Url')).toBe(true);
  });

  it('returns an empty set for undefined/None', () => {
    expect(parseSubmittedModalities(undefined).size).toBe(0);
    expect(parseSubmittedModalities('None').size).toBe(0);
    expect(parseSubmittedModalities('').size).toBe(0);
  });
});

// --- SubmissionViewer dispatcher -------------------------------------------

describe('SubmissionViewer — modality switch', () => {
  beforeEach(() => {
    actionsMock.fetchSubmission.mockReset();
  });

  it('renders ALL present payload viewers stacked for multi-modality submissions', async () => {
    resolveSubmission({
      submittedModalities: 'Text, Url',
      textPayload: 'My reflection',
      urlPayload: 'https://example.com/portfolio',
    });

    render(<SubmissionViewer submissionId="sub-1" />);

    await waitFor(() => expect(screen.getByTestId('text-viewer')).toBeInTheDocument());
    expect(screen.getByTestId('url-viewer')).toBeInTheDocument();
    expect(screen.getByTestId('text-viewer')).toHaveTextContent('My reflection');
  });

  it('renders the code viewer when Code modality + assignment are present', async () => {
    resolveSubmission({
      submittedModalities: 'Code',
      codePayload: JSON.stringify([{ path: '/home/user/main.cpp', content: '// student main' }]),
    });

    render(<SubmissionViewer submissionId="sub-1" codingAssignment={sampleAssignment} manifestUrl="/emception/manifest.json" />);

    await waitFor(() => expect(screen.getByTestId('code-grader-panel')).toBeInTheDocument());
  });

  it('renders a file fallback listing when code arrives without an assignment', async () => {
    resolveSubmission({
      submittedModalities: 'Code',
      codePayload: JSON.stringify([{ path: '/home/user/main.cpp', content: 'int main() {}' }]),
    });

    render(<SubmissionViewer submissionId="sub-1" />);

    await waitFor(() => expect(screen.getByTestId('code-fallback')).toBeInTheDocument());
    expect(screen.getByTestId('code-fallback')).toHaveTextContent('int main() {}');
  });

  it('shows an error state on fetch failure', async () => {
    actionsMock.fetchSubmission.mockResolvedValue({ ok: false, error: 'boom' });

    render(<SubmissionViewer submissionId="sub-1" />);

    expect(await screen.findByTestId('viewer-error')).toHaveTextContent('boom');
  });

  // Bug #1 diagnosability: a malformed codePayload must not vanish silently —
  // the parse error is logged so an empty IDE is traceable to payload drift.
  it('logs a console.error when codePayload fails to parse', async () => {
    const errSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    resolveSubmission({
      submittedModalities: 'Code',
      codePayload: 'not-valid-json{',
    });

    render(<SubmissionViewer submissionId="sub-1" codingAssignment={sampleAssignment} />);

    await waitFor(() => expect(screen.getByTestId('code-grader-panel')).toBeInTheDocument());
    expect(errSpy).toHaveBeenCalled();
    errSpy.mockRestore();
  });
});

// --- Manifest URL resolution -----------------------------------------------

describe('manifest URL resolution', () => {
  beforeEach(() => {
    actionsMock.fetchSubmission.mockReset();
  });

  it('defaults the manifest URL to the self-hosted /emception/ path', async () => {
    resolveSubmission({
      submittedModalities: 'Code',
      codePayload: JSON.stringify([{ path: '/home/user/main.cpp', content: '// student main' }]),
    });

    render(<SubmissionViewer submissionId="sub-1" codingAssignment={sampleAssignment} />);

    const ide = await screen.findByTestId('mock-ide');
    expect(ide).toHaveAttribute('data-manifest-url', '/emception/manifest.json');
  });

  it('passes an explicit manifestUrl through to the Ide', async () => {
    resolveSubmission({
      submittedModalities: 'Code',
      codePayload: JSON.stringify([{ path: '/home/user/main.cpp', content: '// student main' }]),
    });

    render(
      <SubmissionViewer
        submissionId="sub-1"
        codingAssignment={sampleAssignment}
        manifestUrl="https://cdn.example.test/emception/manifest.json"
      />,
    );

    const ide = await screen.findByTestId('mock-ide');
    expect(ide).toHaveAttribute('data-manifest-url', 'https://cdn.example.test/emception/manifest.json');
  });
});

// --- Individual viewers ----------------------------------------------------

describe('UrlViewer', () => {
  it('renders a _blank noopener anchor plus a sandboxed iframe', async () => {
    resolveSubmission({
      submittedModalities: 'Url',
      urlPayload: 'https://example.com/site',
    });

    render(<SubmissionViewer submissionId="sub-1" />);

    const anchor = (await screen.findByTestId('url-anchor')) as HTMLAnchorElement;
    expect(anchor).toHaveAttribute('href', 'https://example.com/site');
    expect(anchor).toHaveAttribute('target', '_blank');
    expect(anchor.getAttribute('rel')).toContain('noopener');
    const iframe = screen.getByTestId('url-embed') as HTMLIFrameElement;
    expect(iframe).toHaveAttribute('src', 'https://example.com/site');
    expect(iframe).toHaveAttribute('sandbox');
  });
});

describe('QuizViewer', () => {
  it('renders structured answers keyed by question id', async () => {
    resolveSubmission({
      submittedModalities: 'StructuredAnswer',
      structuredAnswerPayload: JSON.stringify({
        answers: {
          q1: { textAnswers: { a: '42' } },
          q2: { selectedOptionIds: ['opt-a', 'opt-b'] },
        },
      }),
    });

    render(<SubmissionViewer submissionId="sub-1" />);

    const viewer = await screen.findByTestId('quiz-viewer');
    expect(viewer).toHaveTextContent('q1');
    expect(viewer).toHaveTextContent('42');
    expect(viewer).toHaveTextContent('q2');
    expect(viewer).toHaveTextContent('opt-a');
  });

  it('renders per-item status when a GradeResult is embedded', async () => {
    resolveSubmission({
      submittedModalities: 'StructuredAnswer',
      structuredAnswerPayload: JSON.stringify({
        answers: { q1: { textAnswers: { a: '42' } } },
        gradeResult: {
          status: 'graded',
          score: 8,
          maxScore: 10,
          items: [
            {
              contentBlockId: 'q1',
              status: 'graded',
              score: 8,
              maxScore: 10,
              isCorrect: true,
            },
          ],
        },
      }),
    });

    render(<SubmissionViewer submissionId="sub-1" />);

    const viewer = await screen.findByTestId('quiz-viewer');
    expect(viewer).toHaveTextContent('8');
    expect(viewer).toHaveTextContent('correct');
  });
});

describe('FileViewer', () => {
  it('renders an <img> for image extensions', async () => {
    resolveSubmission({
      submittedModalities: 'File',
      filePayload: 'https://cdn.example.com/submissions/poster.png',
    });

    render(<SubmissionViewer submissionId="sub-1" />);

    const img = (await screen.findByTestId('file-image')) as HTMLImageElement;
    expect(img).toHaveAttribute('src', 'https://cdn.example.com/submissions/poster.png');
  });

  it('renders an <object> for PDFs', async () => {
    resolveSubmission({
      submittedModalities: 'File',
      filePayload: 'https://cdn.example.com/submissions/report.pdf',
    });

    render(<SubmissionViewer submissionId="sub-1" />);

    const object = (await screen.findByTestId('file-pdf')) as HTMLElement;
    expect(object).toHaveAttribute('data', 'https://cdn.example.com/submissions/report.pdf');
  });

  it('falls back to filename + download link for unknown extensions', async () => {
    resolveSubmission({
      submittedModalities: 'File',
      filePayload: 'https://cdn.example.com/submissions/dataset.csv',
    });

    render(<SubmissionViewer submissionId="sub-1" />);

    const fallback = await screen.findByTestId('file-fallback');
    expect(fallback).toHaveTextContent('dataset.csv');
    const link = screen.getByTestId('file-download') as HTMLAnchorElement;
    expect(link).toHaveAttribute('href', 'https://cdn.example.com/submissions/dataset.csv');
  });
});

describe('MediaViewer', () => {
  it('renders <audio controls> for audio extensions', async () => {
    resolveSubmission({
      submittedModalities: 'Media',
      mediaPayload: 'https://cdn.example.com/submissions/answer.mp3',
    });

    render(<SubmissionViewer submissionId="sub-1" />);

    const audio = (await screen.findByTestId('media-audio')) as HTMLElement;
    expect(audio).toHaveAttribute('src', 'https://cdn.example.com/submissions/answer.mp3');
    expect(audio).toHaveAttribute('controls');
  });

  it('renders <video controls> for non-audio media URLs', async () => {
    resolveSubmission({
      submittedModalities: 'Media',
      mediaPayload: 'https://cdn.example.com/submissions/demo.mp4',
    });

    render(<SubmissionViewer submissionId="sub-1" />);

    const video = (await screen.findByTestId('media-video')) as HTMLElement;
    expect(video).toHaveAttribute('src', 'https://cdn.example.com/submissions/demo.mp4');
    expect(video).toHaveAttribute('controls');
  });
});

// --- mergeWorkspaceWithSubmission (moved from grade-client) -----------------

describe('mergeWorkspaceWithSubmission', () => {
  it('overrides modifiable workspace files with submission content', () => {
    const merged = mergeWorkspaceWithSubmission(sampleAssignment, [{ path: '/home/user/main.cpp', content: 'student override' }]);
    expect(merged.find((f) => f.path === '/home/user/main.cpp')?.content).toBe('student override');
  });

  it('skips submission paths that match Private workspace files', () => {
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
    const merged = mergeWorkspaceWithSubmission(sampleAssignment, [{ path: '/home/user/secret.cpp', content: 'STUDENT ATTEMPT' }]);
    expect(merged.find((f) => f.path === '/home/user/secret.cpp')?.content).toBe('// secret solution — workspace only');
    warnSpy.mockRestore();
  });

  it('adds student-created files not present in the workspace', () => {
    const merged = mergeWorkspaceWithSubmission(sampleAssignment, [{ path: '/home/user/new.cpp', content: 'new file' }]);
    expect(merged.find((f) => f.path === '/home/user/new.cpp')?.content).toBe('new file');
  });
});

// --- CodeGraderPanel -------------------------------------------------------

describe('CodeGraderPanel', () => {
  let buildTestPlanSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    ideMock.setFiles.mockReset();
    ideMock.getFiles.mockReset();
    ideMock.runTests.mockReset();
    ideMock.setFiles.mockResolvedValue(undefined);
    ideMock.getFiles.mockResolvedValue([
      { path: '/home/user/main.cpp', content: '// student main' },
      {
        path: '/home/user/secret.cpp',
        content: '// secret solution — workspace only',
      },
    ]);
    buildTestPlanSpy?.mockRestore();
    buildTestPlanSpy = vi.spyOn(emceptionTesting, 'buildTestPlan').mockReturnValue({ plan: samplePlan, generatedFiles: [] });
  });

  it('seeds the IDE with the merged workspace (Private file NOT overridden)', async () => {
    render(
      <CodeGraderPanel
        assignment={sampleAssignment}
        submittedFiles={[
          { path: '/home/user/main.cpp', content: '// student main' },
          { path: '/home/user/secret.cpp', content: '// ATTEMPT' },
        ]}
        maxScore={100}
        manifestUrl="/emception/manifest.json"
      />,
    );

    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());
    const seeded = ideMock.setFiles.mock.calls[0][0];
    const byPath = Object.fromEntries(seeded.map((f) => [f.path, f.content]));
    expect(byPath['/home/user/main.cpp']).toBe('// student main');
    expect(byPath['/home/user/secret.cpp']).toBe('// secret solution — workspace only');
  });

  it('run tests reports the computed score via onComputedScore', async () => {
    const onComputedScore = vi.fn();
    ideMock.runTests.mockResolvedValue(sampleReport);

    render(
      <CodeGraderPanel
        assignment={sampleAssignment}
        submittedFiles={[{ path: '/home/user/main.cpp', content: '// student main' }]}
        maxScore={100}
        manifestUrl="/emception/manifest.json"
        onComputedScore={onComputedScore}
      />,
    );
    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());

    fireEvent.click(screen.getByTestId('run-tests-button'));

    await waitFor(() => expect(onComputedScore).toHaveBeenCalled());
    // 2/3 passing × 100 → 67
    expect(onComputedScore).toHaveBeenCalledWith(expect.objectContaining({ score: 67 }));
    expect(screen.getByTestId('computed-score')).toHaveTextContent('67');
    expect(screen.getByTestId('mock-results')).toBeInTheDocument();
  });

  it('shows an error alert when run tests rejects', async () => {
    ideMock.runTests.mockRejectedValue(new Error('boot failed'));

    render(<CodeGraderPanel assignment={sampleAssignment} submittedFiles={[]} maxScore={100} manifestUrl="/emception/manifest.json" />);
    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());

    fireEvent.click(screen.getByTestId('run-tests-button'));

    expect(await screen.findByRole('alert')).toHaveTextContent('boot failed');
  });

  // Regression guard (Metis #33 / original design): instructor run-tests must
  // build the FULL plan — Public + Private cases — not the student-visible
  // public-only plan. A flip back to 'public-only' would silently drop Private
  // cases from the instructor grade.
  it('run tests builds the test plan with mode "full" (Public + Private)', async () => {
    ideMock.runTests.mockResolvedValue(sampleReport);

    render(
      <CodeGraderPanel
        assignment={sampleAssignment}
        submittedFiles={[{ path: '/home/user/main.cpp', content: '// student main' }]}
        maxScore={100}
        manifestUrl="/emception/manifest.json"
      />,
    );
    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());

    fireEvent.click(screen.getByTestId('run-tests-button'));

    await waitFor(() => expect(buildTestPlanSpy).toHaveBeenCalled());
    expect(buildTestPlanSpy.mock.calls[0]![1]).toEqual({ mode: 'full' });
  });

  // End-to-end plan shape (no buildTestPlan mock): the real mapper must emit
  // one case per Public AND per Private test for the instructor 'full' plan.
  it('real buildTestPlan(full) includes BOTH Public and Private test cases', () => {
    buildTestPlanSpy.mockRestore();
    const { plan } = emceptionTesting.buildTestPlan(
      sampleAssignment as unknown as Parameters<typeof emceptionTesting.buildTestPlan>[0],
      { mode: 'full' },
    );
    const names = plan.cases.map((c) => (c as { name?: string }).name).sort();
    expect(names).toEqual(['case1', 'case2', 'case3']); // 2 Public + 1 Private
  });

  it('real buildTestPlan(public-only) excludes Private cases (contrast for the guard above)', () => {
    buildTestPlanSpy.mockRestore();
    const { plan } = emceptionTesting.buildTestPlan(
      sampleAssignment as unknown as Parameters<typeof emceptionTesting.buildTestPlan>[0],
      { mode: 'public-only' },
    );
    const names = plan.cases.map((c) => (c as { name?: string }).name).sort();
    expect(names).toEqual(['case1', 'case2']); // Public only
  });

  // Bug #1 robustness: when the student submitted no code (payload '{}' or null
  // → parsed to []), the panel must tell the instructor so the template-only
  // IDE is not mistaken for a rendering bug.
  it('shows a "no student code" notice when submittedFiles is empty', async () => {
    render(
      <CodeGraderPanel
        assignment={sampleAssignment}
        submittedFiles={[]}
        maxScore={100}
        manifestUrl="/emception/manifest.json"
      />,
    );
    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());
    expect(screen.getByTestId('no-student-code')).toBeInTheDocument();
  });

  it('does NOT show the "no student code" notice when submittedFiles are present', async () => {
    render(
      <CodeGraderPanel
        assignment={sampleAssignment}
        submittedFiles={[{ path: '/home/user/main.cpp', content: '// student main' }]}
        maxScore={100}
        manifestUrl="/emception/manifest.json"
      />,
    );
    await waitFor(() => expect(ideMock.setFiles).toHaveBeenCalled());
    expect(screen.queryByTestId('no-student-code')).not.toBeInTheDocument();
  });
});
