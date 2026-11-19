import {
  CODING_CONTENT_TYPE,
  CODING_DELIVERY_SCHEMA_VERSION,
  type CodingAuthoringDefinition,
  type CodingLearnerDelivery,
  type CodingSubmission,
} from './contracts';
import { parseCodingAuthoringDefinition, parseCodingLearnerDelivery } from './schemas';

export function projectCodingDefinitionForLearner(value: CodingAuthoringDefinition): CodingLearnerDelivery {
  const definition = parseCodingAuthoringDefinition(value);
  return parseCodingLearnerDelivery({
    schemaVersion: CODING_DELIVERY_SCHEMA_VERSION,
    type: CODING_CONTENT_TYPE,
    exerciseType: definition.exerciseType,
    toolchain: definition.toolchain,
    files: definition.files.filter(({ visibility }) => visibility === 'public').map(({ visibility, ...file }) => {
      void visibility;
      return file;
    }),
    publicTests: definition.tests.filter(({ visibility }) => visibility === 'public').map(({ visibility, ...test }) => {
      void visibility;
      return test;
    }),
    limits: definition.limits,
    entryPoint: definition.entryPoint,
    allowLearnerCreatedFiles: definition.allowLearnerCreatedFiles,
  });
}

export function validateCodingSubmissionAgainstDefinition(
  submission: CodingSubmission,
  definitionValue: CodingAuthoringDefinition,
): CodingSubmission {
  const definition = parseCodingAuthoringDefinition(definitionValue);
  const authoredById = new Map(definition.files.map((file) => [file.id, file]));
  const authoredByPath = new Map(definition.files.map((file) => [file.path, file]));

  for (const file of submission.files) {
    const authoredByFileId = authoredById.get(file.fileId);
    const authoredByFilePath = authoredByPath.get(file.path);
    if (authoredByFileId || authoredByFilePath) {
      if (!authoredByFileId || authoredByFileId !== authoredByFilePath) {
        throw new TypeError(`Coding submission file ${file.fileId} does not match its authored path.`);
      }
      if (!authoredByFileId.mutable || authoredByFileId.visibility !== 'public' || authoredByFileId.encoding !== 'text') {
        throw new TypeError(`Coding submission cannot replace file ${file.fileId}.`);
      }
      continue;
    }
    if (!definition.allowLearnerCreatedFiles) {
      throw new TypeError(`Coding submission cannot create file ${file.path}.`);
    }
  }

  if (definition.files.length + submission.files.filter(({ fileId }) => !authoredById.has(fileId)).length > definition.limits.maxFileCount) {
    throw new TypeError('Coding submission exceeds the authored file-count limit.');
  }
  return structuredClone(submission);
}
