import { describe, expect, it } from 'vitest';
import { buildAssessmentPayload, buildContentActivityPayload, getPreferredSubmissionModality } from './learner-activity-contracts';

describe('learner activity contracts', () => {
    it('normalizes quiz answers into structured assessment JSON', () => {
        expect(buildAssessmentPayload('StructuredAnswer', 'The selected answer')).toEqual({
            structuredAnswerPayload: JSON.stringify({ answer: 'The selected answer' }),
        });
    });

    it('maps project and file references without losing traceability', () => {
        expect(buildAssessmentPayload('Project', 'project-42')).toEqual({ projectPayload: 'project-42' });
        expect(buildAssessmentPayload('File', 'asset-reference-7')).toEqual({ filePayload: 'asset-reference-7' });
    });

    it('creates typed discussion, reflection, and survey response payloads', () => {
        expect(buildContentActivityPayload('discussion', 'A useful class contribution')).toEqual({ kind: 'discussion', body: 'A useful class contribution' });
        expect(buildContentActivityPayload('reflection', 'What I learned')).toEqual({ kind: 'reflection', body: 'What I learned' });
        expect(buildContentActivityPayload('survey', 'Very useful')).toEqual({ kind: 'survey', answers: { response: 'Very useful' } });
    });

    it('chooses a real submission modality for each assessment type', () => {
        expect(getPreferredSubmissionModality('Quiz', 'None')).toBe('StructuredAnswer');
        expect(getPreferredSubmissionModality('Project', 'Text')).toBe('Project');
        expect(getPreferredSubmissionModality('Assignment', 'Url')).toBe('Url');
    });
});