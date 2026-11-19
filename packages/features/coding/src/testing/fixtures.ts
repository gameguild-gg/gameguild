import fixture from '../../fixtures/coding-contract-cases-v1.json';
import { parseCodingAuthoringDefinition, parseCodingExecutionReport, parseCodingLearnerDelivery, parseCodingSubmission } from '../schemas';
import { projectCodingDefinitionForLearnerV2 } from '../projection';
import { parseCodingAuthoringDefinitionV2 } from '../schemas';

export const codingContractFixtureV1 = Object.freeze({
  authoring: parseCodingAuthoringDefinition(fixture.authoring),
  learnerDelivery: parseCodingLearnerDelivery(fixture.learnerDelivery),
  submission: parseCodingSubmission(fixture.submission),
  executionReport: parseCodingExecutionReport(fixture.executionReport),
  invalid: structuredClone(fixture.invalid),
});

const authoringV2 = parseCodingAuthoringDefinitionV2({
  ...codingContractFixtureV1.authoring,
  schemaVersion: 2,
  exerciseType: 'standard-io',
});

const functionAuthoringV2 = parseCodingAuthoringDefinitionV2({
  ...codingContractFixtureV1.authoring,
  schemaVersion: 2,
  exerciseType: 'function',
  entryPoint: {
    ...codingContractFixtureV1.authoring.entryPoint,
    symbol: 'sum',
  },
  tests: [
    {
      id: 'function-public',
      name: 'Public addition cases',
      visibility: 'public',
      weightUnits: 50,
      kind: 'function',
      function: {
        name: 'sum',
        parameters: [
          { name: 'left', type: 'integer' },
          { name: 'right', type: 'integer' },
        ],
        returnType: 'integer',
      },
      cases: [
        {
          id: 'function-public-positive',
          inputs: [
            { type: 'integer', value: 2 },
            { type: 'integer', value: 3 },
          ],
          expected: { type: 'integer', value: 5 },
        },
      ],
    },
    {
      id: 'function-private',
      name: 'Private boundary cases',
      visibility: 'private',
      weightUnits: 50,
      kind: 'function',
      function: {
        name: 'sum',
        parameters: [
          { name: 'left', type: 'integer' },
          { name: 'right', type: 'integer' },
        ],
        returnType: 'integer',
      },
      cases: [
        {
          id: 'function-private-negative',
          inputs: [
            { type: 'integer', value: -2 },
            { type: 'integer', value: -3 },
          ],
          expected: { type: 'integer', value: -5 },
        },
      ],
    },
  ],
});

export const codingContractFixtureV2 = Object.freeze({
  authoring: authoringV2,
  learnerDelivery: projectCodingDefinitionForLearnerV2(authoringV2),
  functionAuthoring: functionAuthoringV2,
  functionLearnerDelivery: projectCodingDefinitionForLearnerV2(functionAuthoringV2),
  submission: codingContractFixtureV1.submission,
  executionReport: codingContractFixtureV1.executionReport,
});
