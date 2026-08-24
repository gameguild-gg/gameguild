import {
  createAssessmentSession,
  type AssessmentSessionOptions,
} from './session';

type MockedSessionOptions = AssessmentSessionOptions & {
  readonly controller: AssessmentSessionOptions['controller'] & {
    readonly api: AssessmentSessionOptions['controller']['api'] & {
      readonly runTests: jest.Mock;
    };
  };
};

function createOptions(
  mode: AssessmentSessionOptions['mode'] = 'grader',
): MockedSessionOptions {
  const files = new Set<string>();
  const workspace = {
    readFile: jest.fn(async (path: string) => {
      return files.has(path) ? new Uint8Array() : null;
    }),
    writeFile: jest.fn(async (path: string, _content: Uint8Array | string) => {
      files.add(path);
    }),
    deleteFile: jest.fn(async (path: string) => {
      files.delete(path);
    }),
  };
  const api = {
    workspace,
    runTests: jest.fn(async () => ({
      passed: 1,
      failed: 0,
      totalDurationMs: 4,
      cases: [{ name: 'public-addition', passed: true, durationMs: 4 }],
    })),
  };

  return {
    mode,
    controller: {
      api,
      getFiles: jest.fn(async () => [
        {
          path: '/home/user/solution.cpp',
          type: 'text' as const,
          content: 'int add(int a, int b) { return a + b; }',
        },
      ]),
    },
    definition: {
      Type: 'coding-assignment',
      Version: 1,
      Environment: { AllowStudentCreateFiles: true },
      Data: {
        Files: {
          '/home/user/solution.cpp': {
            Content: 'int add(int a, int b) { return a + b; }',
            Encoding: 'text',
            Visibility: 'Public',
            Modifiable: true,
          },
        },
      },
      Tests: {
        Public: [
          {
            kind: 'functional',
            Name: 'public-addition',
            Function: {
              FunctionName: 'add',
              Parameters: [
                { Name: 'a', Type: 'integer' },
                { Name: 'b', Type: 'integer' },
              ],
              ReturnType: { Type: 'integer' },
            },
            Cases: [
              {
                Inputs: [
                  { Type: 'integer', Content: 1 },
                  { Type: 'integer', Content: 2 },
                ],
                Expected: { Type: 'integer', Content: 3 },
              },
            ],
          },
        ],
        Private: [
          { kind: 'standard', Name: 'private-test-secret', Stdout: 'secret' },
        ],
      },
      Grading: { MaxScore: 100 },
    },
  };
}

describe('createAssessmentSession', () => {
  it('runs the full plan with an ephemeral VFS overlay and never publishes harnesses to IDE files', async () => {
    const options = createOptions();
    const session = createAssessmentSession(options);

    const result = await session.run('full');

    expect(options.controller.api.runTests).toHaveBeenCalledWith(
      expect.objectContaining({ cases: expect.any(Array) }),
      expect.objectContaining({ signal: undefined }),
    );
    expect(options.controller.api.workspace.writeFile).toHaveBeenCalledWith(
      '/home/user/functional_0_test.cpp',
      expect.any(String),
    );
    expect(options.controller.api.workspace.deleteFile).toHaveBeenCalledWith(
      '/home/user/functional_0_test.cpp',
    );
    await expect(options.controller.getFiles()).resolves.not.toContainEqual(
      expect.objectContaining({ path: '/home/user/functional_0_test.cpp' }),
    );
    expect(result.scope).toBe('full');
    expect(result.score.score).toBe(50);
  });

  it('rejects a full run for a learner before it touches the VFS', async () => {
    const options = createOptions('learner');
    const session = createAssessmentSession(options);

    await expect(session.run('full')).rejects.toThrow(
      'Learner sessions may only run public tests',
    );
    expect(options.controller.api.workspace.writeFile).not.toHaveBeenCalled();
    expect(options.controller.api.runTests).not.toHaveBeenCalled();
  });

  it('removes generated harnesses when test execution fails', async () => {
    const options = createOptions();
    options.controller.api.runTests.mockRejectedValueOnce(new Error('toolchain failed'));
    const session = createAssessmentSession(options);

    await expect(session.run('full')).rejects.toThrow('toolchain failed');
    expect(options.controller.api.workspace.deleteFile).toHaveBeenCalledWith(
      '/home/user/functional_0_test.cpp',
    );
  });
});
