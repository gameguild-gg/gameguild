import fixture from '../../fixtures/coding-contract-cases-v1.json';
import { parseCodingAuthoringDefinition, parseCodingExecutionReport, parseCodingLearnerDelivery, parseCodingSubmission } from '../schemas';

export const codingContractFixtureV1 = Object.freeze({
  authoring: parseCodingAuthoringDefinition(fixture.authoring),
  learnerDelivery: parseCodingLearnerDelivery(fixture.learnerDelivery),
  submission: parseCodingSubmission(fixture.submission),
  executionReport: parseCodingExecutionReport(fixture.executionReport),
  invalid: structuredClone(fixture.invalid),
});
