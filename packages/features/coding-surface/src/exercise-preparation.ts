import {
  compileCodingAuthoringDraft,
  parseCodingAuthoringDefinition,
  projectCodingDefinitionForLearner,
  type CodingAuthoringDefinition,
  type CodingAuthoringDraft,
  type CodingLearnerDelivery,
} from '@game-guild/coding';

export function prepareCodingValidationSolution(draft: CodingAuthoringDraft, solutionId: string): CodingAuthoringDefinition {
  const definition = compileCodingAuthoringDraft(draft);
  const solution = definition.validationSolutions?.find((entry) => entry.id === solutionId);
  if (!solution || !solution.files.length) throw new TypeError('Select a solution with at least one source replacement.');
  const replacements = new Map(solution.files.map((file) => [file.fileId, file.content]));
  const { validationSolutions, ...candidate } = definition;
  void validationSolutions;
  return parseCodingAuthoringDefinition({
    ...candidate,
    files: candidate.files.map((file) => ({ ...file, content: replacements.get(file.id) ?? file.content })),
  });
}

export function prepareCodingLearnerPreview(draft: CodingAuthoringDraft): CodingLearnerDelivery {
  return projectCodingDefinitionForLearner(compileCodingAuthoringDraft(draft));
}
