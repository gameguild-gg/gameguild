import { act, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/client';

const mocks = vi.hoisted(() => ({ editor: vi.fn(() => null), createWorkspace: vi.fn(), formatFeedback: vi.fn() }));
vi.mock('@game-guild/emception-ui', () => ({
  CodingAssessmentEditor: (props: Record<string, unknown>) => {
    mocks.editor(props);
    return <div data-testid="assessment-editor" />;
  },
}));
vi.mock('@game-guild/emception-ui/assessment/presets', () => ({ createAssessmentWorkspaceConfig: mocks.createWorkspace }));
vi.mock('@/lib/emception/scoring', () => ({ formatFeedback: mocks.formatFeedback }));

import { AssessmentGrader, mergeWorkspaceWithSubmission } from './assessment-grader';

function assignment(language: string | undefined = 'cpp') {
  return {
    Environment: { Language: language },
    Data: {
      Files: {
        'main.cpp': { Content: '// template', Encoding: 'text', Visibility: 'Public' },
        'private.test.cpp': { Content: '// hidden tests', Encoding: 'text', Visibility: 'Private' },
      },
    },
  } as unknown as CodingAssignmentContent;
}

describe('assessment grader workspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.createWorkspace.mockImplementation((language, files) => ({ id: language, files }));
    mocks.formatFeedback.mockReturnValue('2/2 private and public tests passed.');
  });

  it('merges public student changes without trusting private-path overrides', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    expect(mergeWorkspaceWithSubmission(assignment(), [
      { path: 'main.cpp', content: '// student solution' },
      { path: 'private.test.cpp', content: '// forged passing test' },
      { path: 'notes.txt', content: 'notes' },
    ])).toEqual([
      { path: 'main.cpp', content: '// student solution' },
      { path: 'private.test.cpp', content: '// hidden tests' },
      { path: 'notes.txt', content: 'notes' },
    ]);
    expect(warn).toHaveBeenCalledWith(expect.stringContaining('private.test.cpp'));
    warn.mockRestore();
  });

  it('shows an empty-submission warning and computes a grade with the default language', () => {
    const onComputedScore = vi.fn();
    render(
      <AssessmentGrader
        assignment={assignment(null as unknown as string)}
        submittedFiles={[]}
        maxScore={100}
        manifestUrl="/manifest.json"
        onComputedScore={onComputedScore}
      />,
    );

    expect(screen.getByTestId('no-student-code')).toBeInTheDocument();
    const props = mocks.editor.mock.calls.at(-1)![0] as Record<string, unknown>;
    expect(mocks.createWorkspace).toHaveBeenCalledWith('cpp', {
      'main.cpp': { encoding: 'text', content: '// template' },
    });
    expect(props).toMatchObject({
      mode: 'grader', manifestUrl: '/manifest.json', title: 'Grade submission',
      workspaceStorageKey: undefined, enableWorkspace: false, maxScore: 100, passingScore: 0,
    });

    const report = { passed: 2, failed: 0 };
    act(() => {
      (props.onRunResult as (value: unknown) => void)({ report, score: { score: 95 } });
    });
    expect(screen.getByTestId('computed-score')).toHaveTextContent('Computed score: 95 / 100');
    expect(mocks.formatFeedback).toHaveBeenCalledWith(report, 95);
    expect(onComputedScore).toHaveBeenCalledWith({ score: 95, autoFeedback: '2/2 private and public tests passed.' });
  });

  it('restores a submitted workspace, omits private files, and supports an anonymous score listener', () => {
    render(
      <AssessmentGrader
        assignment={assignment('rust')}
        submittedFiles={[
          { path: 'main.cpp', content: '// solution' },
          { path: 'notes.txt', content: 'student notes' },
        ]}
        maxScore={50}
        manifestUrl="/rust-manifest.json"
        submissionId="submission-1"
      />,
    );

    expect(screen.queryByTestId('no-student-code')).not.toBeInTheDocument();
    expect(mocks.createWorkspace).toHaveBeenCalledWith('rust', {
      'main.cpp': { encoding: 'text', content: '// solution' },
      'notes.txt': { encoding: 'text', content: 'student notes' },
    });
    const props = mocks.editor.mock.calls.at(-1)![0] as Record<string, unknown>;
    expect(props.workspaceStorageKey).toBe('emception:grader:submission-1');
    act(() => {
      (props.onRunResult as (value: unknown) => void)({ report: { passed: 0, failed: 1 }, score: { score: 0 } });
    });
    expect(screen.getByTestId('computed-score')).toHaveTextContent('Computed score: 0 / 50');
  });
});
