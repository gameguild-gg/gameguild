import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';

const mockFiles = new Set<string>();
const mockRunTests = jest.fn(async (_plan: { cases: unknown[] }, _options?: unknown) => ({
  passed: 1,
  failed: 0,
  totalDurationMs: 5,
  cases: [{ name: 'public-addition', passed: true, durationMs: 5 }],
}));

const mockRuntime = {
  workspace: {
    readFile: jest.fn(async (path: string) => (mockFiles.has(path) ? new Uint8Array() : null)),
    writeFile: jest.fn(async (path: string) => {
      mockFiles.add(path);
    }),
    deleteFile: jest.fn(async (path: string) => {
      mockFiles.delete(path);
    }),
  },
  runTests: mockRunTests,
};

const mockController = {
  api: mockRuntime,
  getFiles: jest.fn(async () => [
    {
      path: '/home/user/solution.cpp',
      type: 'text' as const,
      content: 'int add(int a, int b) { return a + b; }',
    },
  ]),
  replaceFiles: jest.fn(),
  setFilesReadOnly: jest.fn(),
};

jest.mock('@gameguild/emception-ide', () => {
  const MockReact = require('react') as typeof import('react');
  function Ide({
    onReady,
    extensions,
  }: {
    onReady?: (nextController: typeof mockController) => void;
    extensions?: readonly {
      id: string;
      toolbarEnd?: (nextController: typeof mockController) => React.ReactNode;
      bottomPanel?: (nextController: typeof mockController) => React.ReactNode;
    }[];
  }) {
    MockReact.useEffect(() => {
      onReady?.(mockController);
    }, [onReady]);
    return (
      <div data-testid="vanilla-ide">
        {extensions?.map((extension) => (
          <MockReact.Fragment key={extension.id}>
            {extension.toolbarEnd?.(mockController)}
            {extension.bottomPanel?.(mockController)}
          </MockReact.Fragment>
        ))}
      </div>
    );
  }

  return { Ide };
});

import { CodingAssessmentEditor } from './CodingAssessmentEditor';

const definition = {
  Type: 'coding-assignment' as const,
  Version: 1 as const,
  Environment: { AllowStudentCreateFiles: true },
  Data: {
    Files: {
      '/home/user/solution.cpp': {
        Content: 'int add(int a, int b) { return a + b; }',
        Encoding: 'text' as const,
        Visibility: 'Public' as const,
        Modifiable: true,
      },
    },
  },
  Tests: {
    Public: [
      {
        kind: 'functional' as const,
        Name: 'public-addition',
        Function: {
          FunctionName: 'add',
          Parameters: [
            { Name: 'a', Type: 'integer' as const },
            { Name: 'b', Type: 'integer' as const },
          ],
          ReturnType: { Type: 'integer' as const },
        },
        Cases: [
          {
            Inputs: [
              { Type: 'integer' as const, Content: 1 },
              { Type: 'integer' as const, Content: 2 },
            ],
            Expected: { Type: 'integer' as const, Content: 3 },
          },
        ],
      },
    ],
    Private: [
      { kind: 'standard' as const, Name: 'private-test-secret', Stdout: 'secret' },
    ],
  },
  Grading: { MaxScore: 100 },
};

describe('CodingAssessmentEditor', () => {
  beforeEach(() => {
    mockFiles.clear();
    mockRuntime.workspace.readFile.mockClear();
    mockRuntime.workspace.writeFile.mockClear();
    mockRuntime.workspace.deleteFile.mockClear();
    mockRunTests.mockClear();
    mockController.setFilesReadOnly.mockClear();
  });

  it('uses the vanilla IDE controller and keeps generated test files out of the visible editor state', async () => {
    const onRunResult = jest.fn();
    render(
      <CodingAssessmentEditor
        mode="grader"
        definition={definition}
        onRunResult={onRunResult}
      />,
    );

    expect(await screen.findByTestId('vanilla-ide')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Run full tests' }));

    await waitFor(() => expect(onRunResult).toHaveBeenCalled());
    expect(mockRunTests).toHaveBeenCalledTimes(1);
    expect(mockRuntime.workspace.writeFile).toHaveBeenCalledWith(
      '/home/user/functional_0_test.cpp',
      expect.any(String),
    );
    expect(mockRuntime.workspace.deleteFile).toHaveBeenCalledWith(
      '/home/user/functional_0_test.cpp',
    );
    expect(mockController.getFiles).not.toHaveBeenCalledWith(
      expect.objectContaining({ path: '/home/user/functional_0_test.cpp' }),
    );
  });

  it('only lets learner mode execute the public plan', async () => {
    render(<CodingAssessmentEditor mode="learner" definition={definition} />);

    fireEvent.click(await screen.findByRole('button', { name: 'Run public tests' }));

    await waitFor(() => expect(mockRunTests).toHaveBeenCalled());
    const plan = mockRunTests.mock.calls[0]?.[0];
    expect(plan.cases).toHaveLength(1);
    expect(JSON.stringify(plan)).not.toContain('private-test-secret');
  });
});
