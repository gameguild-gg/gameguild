import type { CodingExerciseTypeV2 } from '@game-guild/coding';
import type { CodingSurfaceProfile } from './types';
import {
  FunctionAuthorProfile,
  FunctionAuthorTestProfile,
  FunctionGraderProfile,
  FunctionLearnerProfile,
  StdioAuthorProfile,
  StdioAuthorTestProfile,
  StdioGraderProfile,
  StdioLearnerProfile,
} from './profile-renderers';

const profiles = Object.freeze({
  function: Object.freeze({
    type: 'function',
    label: 'Function exercise',
    description: 'Implement the requested function and validate it against typed cases.',
    capabilities: Object.freeze({ files: true, terminal: false, testResults: true, preview: true }),
    guidance: Object.freeze({
      author: 'Define the function signature, starter files, public examples, and private expectations.',
      'author-test': 'Run the complete candidate revision without creating an academic attempt.',
      learner: 'Implement the requested function and verify it against the public cases.',
      grader: 'Inspect the submitted function and run the authorized complete test set.',
    }),
    renderers: Object.freeze({
      author: FunctionAuthorProfile,
      'author-test': FunctionAuthorTestProfile,
      learner: FunctionLearnerProfile,
      grader: FunctionGraderProfile,
    }),
  }),
  'standard-io': Object.freeze({
    type: 'standard-io',
    label: 'Input and output exercise',
    description: 'Build a program that reads standard input and produces the expected output.',
    capabilities: Object.freeze({ files: true, terminal: true, testResults: true, preview: true }),
    guidance: Object.freeze({
      author: 'Define starter files, standard input, expected output, and private expectations.',
      'author-test': 'Run the complete candidate revision without creating an academic attempt.',
      learner: 'Run the program with the public inputs and compare its output locally.',
      grader: 'Inspect the submitted program and run the authorized complete test set.',
    }),
    renderers: Object.freeze({
      author: StdioAuthorProfile,
      'author-test': StdioAuthorTestProfile,
      learner: StdioLearnerProfile,
      grader: StdioGraderProfile,
    }),
  }),
} satisfies Record<CodingExerciseTypeV2, CodingSurfaceProfile>);

export function getCodingSurfaceProfile(type: CodingExerciseTypeV2): CodingSurfaceProfile {
  const profile = profiles[type];
  if (!profile) throw new TypeError(`Coding exercise type "${String(type)}" has no registered surface.`);
  return profile;
}

export function listCodingSurfaceProfiles(): readonly CodingSurfaceProfile[] {
  return Object.values(profiles);
}
