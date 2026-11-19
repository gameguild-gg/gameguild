import { describe, expect, it } from 'vitest';
import {
  applySubmissionToDefinition,
  codingAssignmentToAuthoringDefinition,
  codingAssignmentToLearnerDelivery,
} from './coding-surface-bridge';
import type { CodingAssignmentContent } from './types';

function fixture(exerciseType: CodingAssignmentContent['ExerciseType'] = 'standard-io'): CodingAssignmentContent {
  return {
    Type: 'coding-assignment',
    Version: 1,
    ExerciseType: exerciseType,
    Environment: { Language: 'cpp', Tools: 'clang', AllowStudentCreateFiles: false },
    Data: { Files: {
      'main.cpp': { Content: 'int main() {}', Encoding: 'text', Visibility: 'Public', Modifiable: true },
      'hidden.cpp': { Content: 'secret', Encoding: 'text', Visibility: 'Private', Modifiable: false },
    } },
    Tests: {
      Public: [{ kind: 'standard', Stdout: 'ok', Weight: 1 }],
      Private: [{ kind: 'standard', Stdout: 'secret', Weight: 2 }],
    },
    Grading: { MaxScore: 100 },
  };
}

describe('coding surface wire bridge', () => {
  it('preserves the explicit exercise type independently from test kind', () => {
    expect(codingAssignmentToAuthoringDefinition(fixture('function')).exerciseType).toBe('function');
    expect(codingAssignmentToAuthoringDefinition(fixture('standard-io')).exerciseType).toBe('standard-io');
  });

  it('rejects a missing exercise type instead of inferring a profile', () => {
    expect(() => codingAssignmentToAuthoringDefinition({
      ...fixture(),
      ExerciseType: undefined,
    } as unknown as CodingAssignmentContent)).toThrow('exercise type is required');
  });

  it('redacts private files and tests structurally for learner delivery', () => {
    const serialized = JSON.stringify(codingAssignmentToLearnerDelivery(fixture()));
    expect(serialized).not.toContain('secret');
    expect(serialized).not.toContain('hidden.cpp');
    expect(serialized).not.toContain('visibility');
  });

  it('never lets a submission replace private files', () => {
    const content = fixture();
    const definition = codingAssignmentToAuthoringDefinition({
      ...content,
      Environment: { ...content.Environment, AllowStudentCreateFiles: true },
    });
    const merged = applySubmissionToDefinition(definition, [
      { path: 'main.cpp', content: 'int main() { return 1; }' },
      { path: 'hidden.cpp', content: 'learner override' },
      { path: 'notes.cpp', content: '// learner file' },
    ]);

    expect(merged.files.find((file) => file.path.endsWith('/main.cpp'))?.content).toContain('return 1');
    expect(merged.files.find((file) => file.path.endsWith('/hidden.cpp'))?.content).toBe('secret');
    expect(merged.files.find((file) => file.path.endsWith('/notes.cpp'))).toMatchObject({
      content: '// learner file',
      mutable: true,
      visibility: 'public',
    });
  });
});
