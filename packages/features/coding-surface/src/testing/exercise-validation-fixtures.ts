import { parseCodingAuthoringDraft, type CodingExerciseType } from '@game-guild/coding';
import drafts from '../../../coding/fixtures/coding-authoring-drafts.json';

/** Synthetic protected bytes for independent projection and actual runtime checks. */
export function exerciseValidationFixture(family: CodingExerciseType = 'function') {
  const draft = parseCodingAuthoringDraft(drafts.function);
  draft.exerciseType = family;
  draft.problem.statement = family === 'function' ? 'Return the sum of two integers.' : 'Read two integers and print their sum.';
  draft.problem.examples = [{ id: 'example-1', input: '2 3', output: '5', explanation: 'Add both values.' }];
  draft.problem.constraints = ['The values are between -10 and 10.'];
  const source = (correct: boolean) =>
    family === 'function'
      ? `int sum(int a, int b) { return a ${correct ? '+' : '-'} b; }`
      : `#include <cstdio>\nint main() { int a, b; if (std::scanf("%d %d", &a, &b) != 2) return 1; std::printf("%d\\n", a ${correct ? '+' : '-'} b); }`;
  draft.files = [
    {
      id: 'main-file',
      path: '/home/user/main.cpp',
      content: family === 'function' ? 'int sum(int a, int b) { return 0; }' : 'int main() { return 0; }',
      encoding: 'text',
      visibility: 'public',
      mutable: true,
    },
    { id: 'private-file', path: '/home/user/private.h', content: '// PRIVATE-FILE-CANARY', encoding: 'text', visibility: 'private', mutable: false },
  ];
  draft.entryPoint.path = '/home/user/main.cpp';
  if (family === 'standard-io') draft.functionSignature = null;
  draft.tests = (
    [
      ['public-case', 'Public addition', 'public', 2, 3, 5],
      ['private-case', 'PRIVATE-CASE-CANARY', 'private', 4, 5, 9],
    ] as const
  ).map(([id, name, visibility, a, b, sum]) =>
    family === 'function'
      ? {
          id,
          name,
          visibility,
          kind: 'function',
          weightUnits: '1',
          cases: [
            {
              id: `${id}-values`,
              inputs: [
                { type: 'integer', text: String(a) },
                { type: 'integer', text: String(b) },
              ],
              expected: { type: 'integer', text: String(sum) },
            },
          ],
        }
      : { id, name, visibility, kind: 'stdio', weightUnits: '1', stdin: `${a} ${b}\n`, expected: { stdout: `${sum}\n`, stderr: null, exitCode: '0' } },
  );
  draft.validationSolutions = [
    {
      id: 'reference',
      name: 'Correct sum',
      role: 'reference',
      files: [{ fileId: 'main-file', path: '/home/user/main.cpp', content: `// REFERENCE-CANARY\n${source(true)}` }],
    },
    {
      id: 'incorrect',
      name: 'Subtracts instead',
      role: 'incorrect',
      files: [{ fileId: 'main-file', path: '/home/user/main.cpp', content: `// INCORRECT-CANARY\n${source(false)}` }],
    },
  ];
  return parseCodingAuthoringDraft(draft);
}
