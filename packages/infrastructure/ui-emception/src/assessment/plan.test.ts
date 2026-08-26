import {
  buildAssessmentExecutionPlan,
} from './plan';
import { normalizeAssessmentWorkspacePath } from './paths';
import type { CodingAssessmentDefinition } from './types';

const assignment: CodingAssessmentDefinition = {
  Type: 'coding-assignment',
  Version: 1,
  Environment: {
    Language: 'cpp',
    Tools: 'clang',
    AllowStudentCreateFiles: true,
  },
  Data: {
    Files: {
      '/home/user/solution.cpp': {
        Content: 'int add(int a, int b) { return a + b; }',
        Encoding: 'text',
        Visibility: 'Public',
        Modifiable: true,
      },
      '/home/user/asset.bin': {
        Content: 'AA==',
        Encoding: 'base64',
        Visibility: 'Public',
        Modifiable: false,
      },
      '/home/user/private-fixture.cpp': {
        Content: 'int fixture() { return 42; }',
        Encoding: 'text',
        Visibility: 'Private',
        Modifiable: false,
      },
    },
  },
  Tests: {
    Public: [
      {
        kind: 'functional',
        Name: 'public-addition',
        Weight: 2,
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
              { Type: 'integer', Content: 2 },
              { Type: 'integer', Content: 3 },
            ],
            Expected: { Type: 'integer', Content: 5 },
          },
        ],
      },
    ],
    Private: [
      {
        kind: 'standard',
        Name: 'private-test-secret',
        Weight: 7,
        Stdin: '7 8\n',
        Stdout: '15\n',
      },
    ],
  },
  Grading: { MaxScore: 10 },
};

describe('buildAssessmentExecutionPlan', () => {
  it('normalizes legacy /user paths to the Toolchain workspace mount', () => {
    const legacyAssignment: CodingAssessmentDefinition = {
      ...assignment,
      Data: {
        Files: {
          '/user/main.cpp': assignment.Data.Files['/home/user/solution.cpp']!,
        },
      },
    };

    expect(normalizeAssessmentWorkspacePath('/user/main.cpp')).toBe('/home/user/main.cpp');
    expect(normalizeAssessmentWorkspacePath('/home/user/main.cpp')).toBe('/home/user/main.cpp');
    expect(buildAssessmentExecutionPlan(legacyAssignment, 'public').plan.build?.sources)
      .toEqual(['/home/user/main.cpp']);
  });

  it('excludes private tests and exposes generated harnesses only in public scope', () => {
    const execution = buildAssessmentExecutionPlan(assignment, 'public');

    expect(execution.plan.cases).toHaveLength(1);
    expect(execution.plan.cases[0]).toMatchObject({
      kind: 'doctest',
      name: 'public-addition',
      weight: 2,
    });
    expect(execution.overlay).toHaveLength(1);
    expect(execution.overlay[0]?.path).toMatch(/\/functional_0_test\.cpp$/);
    expect(execution.overlay[0]?.content).toContain('constexpr const char* testName = "0:public-addition"');
    expect(execution.overlay[0]?.content).toContain('check(add(2, 3) == 5');
    expect(JSON.stringify(execution)).not.toContain('private-test-secret');
    expect(execution.plan.build?.sources).toEqual(['/home/user/solution.cpp']);
    expect(execution.plan.build?.sources).not.toContain('/home/user/private-fixture.cpp');
  });

  it('includes private cases only in full scope', () => {
    const execution = buildAssessmentExecutionPlan(assignment, 'full');

    expect(execution.plan.cases).toHaveLength(2);
    expect(execution.plan.cases[1]).toMatchObject({
      kind: 'stdio',
      name: 'private-test-secret',
      expectedStdout: '15\n',
      weight: 7,
    });
    expect(execution.weights).toEqual([2, 7]);
    expect(execution.plan.build?.sources).toContain('/home/user/private-fixture.cpp');
    expect(execution.overlay).toContainEqual({
      path: '/home/user/private-fixture.cpp',
      content: 'int fixture() { return 42; }',
    });
  });
});
