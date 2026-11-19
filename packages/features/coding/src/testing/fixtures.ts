import fixture from '../../fixtures/coding-contract-cases.json';
import { projectCodingDefinitionForLearner } from '../projection';
import {
  parseCodingAuthoringDefinition,
  parseCodingExecutionReport,
  parseCodingLearnerDelivery,
  parseCodingSubmission,
} from '../schemas';

const authoring = parseCodingAuthoringDefinition(fixture.authoring);

const functionAuthoring = parseCodingAuthoringDefinition({
  ...authoring,
  exerciseType: 'function',
  entryPoint: {
    ...authoring.entryPoint,
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

export const codingContractFixture = Object.freeze({
  authoring,
  learnerDelivery: parseCodingLearnerDelivery(fixture.learnerDelivery),
  functionAuthoring,
  functionLearnerDelivery: projectCodingDefinitionForLearner(functionAuthoring),
  submission: parseCodingSubmission(fixture.submission),
  executionReport: parseCodingExecutionReport(fixture.executionReport),
  invalid: structuredClone(fixture.invalid),
});
