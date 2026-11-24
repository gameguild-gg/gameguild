import { compileCodingAuthoringDraft, parseCodingAuthoringDraft } from '@game-guild/coding';
import drafts from '../../../../../packages/features/coding/fixtures/coding-authoring-drafts.json';
import { describe, expect, it } from 'vitest';
import type { AuthoringContentPayload } from '@/lib/learning/authoring';
import { codingAssignmentToAuthoringDefinition, codingAuthoringDefinitionToAssignmentContent } from './coding-surface-bridge';
import { narrowCodingAssignmentContent } from './types';
import {
  assertLearningCodingSaveAcknowledgement,
  prepareLearningCodingPublication,
  readLearningCodingDocument,
  type LearningCodingSaveExpectation,
} from './learning-authoring-document';

const payload = (): AuthoringContentPayload => ({
  type: 'Code',
  title: 'Add two values',
  slug: 'add-values',
  description: 'Host description',
  jsonBody: { ...parseCodingAuthoringDraft(drafts.function) },
  body: null,
  lessonFormat: null,
  isRequired: true,
  estimatedMinutesSource: 'Manual',
  estimatedMinutes: 12,
  visibility: 'Private',
  activitySettings: { gradingEnabled: true },
});

const assignment = () => codingAuthoringDefinitionToAssignmentContent(compileCodingAuthoringDraft(drafts.function), { MaxScore: 25 });

describe('UX04-C: Learning coding documents', () => {
  it.each(['function', 'standard-io'] as const)(
    '[CS-REF-01/02] reopens incomplete %s buffers without requiring publication or changing their values',
    (family) => {
      const document = parseCodingAuthoringDraft(drafts[family]);
      const result = readLearningCodingDocument({ document, publishedAssignment: null });
      expect(result).toEqual(document);
      result.files[0]!.content = 'isolated buffer';
      expect(document.files[0]!.content).not.toBe('isolated buffer');
    },
  );

  it('starts a genuinely empty exercise without a graphics demo, cases or manufactured answer', () => {
    const result = readLearningCodingDocument({ document: null, publishedAssignment: null });
    expect(result.exerciseType).toBe('standard-io');
    expect(result.environment).toEqual({ language: 'cpp', runtime: 'wasi' });
    expect(result.files).toEqual([]);
    expect(result.tests).toEqual([]);
    expect(result.validationSolutions).toEqual([]);
    expect(result.problem).toEqual({ statement: '', examples: [], constraints: [] });
    expect(result.limits.memoryLimitBytes).toBe('');
    expect(result.entryPoint.path).toBe('');
  });

  it('reopens explicit published data without writing or losing stable identities and protected solutions', () => {
    const publishedAssignment = assignment();
    const before = structuredClone(publishedAssignment);
    const result = readLearningCodingDocument({ document: publishedAssignment, publishedAssignment: null });
    expect(compileCodingAuthoringDraft(result)).toEqual(codingAssignmentToAuthoringDefinition(publishedAssignment));
    expect(result.files[0]!.id).toBe('main-file');
    expect(result.validationSolutions[0]!.files[0]!.content).toContain('SYNTHETIC-TEACHER-REFERENCE');
    expect(publishedAssignment).toEqual(before);
  });

  it('uses the prior assignment only when no draft document exists', () => {
    expect(readLearningCodingDocument({ document: null, publishedAssignment: assignment() }).exerciseType).toBe('function');
    const incomplete = parseCodingAuthoringDraft(drafts['standard-io']);
    expect(readLearningCodingDocument({ document: incomplete, publishedAssignment: assignment() })).toEqual(incomplete);
    expect(() => readLearningCodingDocument({ document: { ...incomplete, schemaVersion: 99 }, publishedAssignment: assignment() })).toThrow();
  });

  it('rejects obsolete test configuration without adoption, fallback or rewriting it with defaults', () => {
    const obsolete = { ...assignment(), Execution: undefined, Environment: { Language: 'cpp', Tools: 'clang', AllowStudentCreateFiles: false } };
    const before = structuredClone(obsolete);
    expect(() => readLearningCodingDocument({ document: obsolete, publishedAssignment: null })).toThrow('current authoring contract');
    expect(obsolete).toEqual(before);
    expect(() => readLearningCodingDocument({ document: { unexpected: true }, publishedAssignment: assignment() })).toThrow();
  });

  it('compiles only a complete candidate to the existing wire contract, preserving host metadata and grading', () => {
    const authored = payload();
    const before = structuredClone(authored);
    const result = prepareLearningCodingPublication({ document: authored.jsonBody, publishedAssignment: null }, authored, { MaxScore: 25 });
    expect(result).toEqual({ ...authored, jsonBody: assignment() });
    expect(result.jsonBody?.Type).toBe('coding-assignment');
    expect(result.jsonBody?.Grading).toEqual({ MaxScore: 25 });
    expect(authored).toEqual(before);
  });

  it('publishes standard I/O with an intentional empty expectation, not an absent oracle', () => {
    const document = parseCodingAuthoringDraft(drafts['standard-io']);
    document.limits.timeLimitMs = '4321';
    document.tests = [
      {
        id: 'empty-output',
        name: 'Empty output',
        kind: 'stdio',
        visibility: 'public',
        weightUnits: '100',
        stdin: '',
        expected: { stdout: '', stderr: '', exitCode: '0' },
      },
    ];
    const authored = { ...payload(), jsonBody: { ...document } };
    const result = prepareLearningCodingPublication({ document, publishedAssignment: null }, authored, { MaxScore: 25 });
    const wire = narrowCodingAssignmentContent(result.jsonBody);
    expect(wire?.Execution?.Limits.TimeLimitMs).toBe(4321);
    expect(wire?.Tests.Public).toEqual([
      { kind: 'standard', Id: 'empty-output', Name: 'Empty output', Weight: 1, Stdin: '', Stdout: '', Stderr: '', ExitCode: 0 },
    ]);
  });

  it('rejects a stale valid document instead of publishing it with newer host metadata', () => {
    const authored = payload();
    const original = structuredClone(authored.jsonBody);
    authored.jsonBody = {
      ...parseCodingAuthoringDraft(drafts.function),
      problem: { statement: 'A newer problem', examples: [], constraints: [] },
    };
    expect(() => prepareLearningCodingPublication({ document: original, publishedAssignment: null }, authored, { MaxScore: 25 })).toThrow();
  });

  it('rejects incomplete drafts and foreign lesson types without publishing the author-only discriminator', () => {
    const authored = payload();
    expect(() => prepareLearningCodingPublication({ document: drafts['standard-io'], publishedAssignment: null }, authored, { MaxScore: 25 })).toThrow();
    expect(() =>
      prepareLearningCodingPublication({ document: authored.jsonBody, publishedAssignment: null }, { ...authored, type: 'Lesson' }, { MaxScore: 25 }),
    ).toThrow();
    expect(() => prepareLearningCodingPublication({ document: authored.jsonBody, publishedAssignment: null }, authored, { MaxScore: 0 })).toThrow();
  });
});

describe('UX04-C: exact host draft acknowledgement', () => {
  const expected = (): LearningCodingSaveExpectation => ({
    programId: 'course',
    contentId: 'content',
    revision: 7,
    basePublishedVersion: 3,
    payload: payload(),
  });
  const acknowledgement = () => ({ programId: 'course', contentId: 'content', revision: 8, basePublishedVersion: 3, payload: payload() });

  it('accepts only the frozen payload at the exact next revision', () => {
    expect(() => assertLearningCodingSaveAcknowledgement(acknowledgement(), expected())).not.toThrow();
  });

  it.each([
    { revision: 7 },
    { revision: 9 },
    { revision: Number.NaN },
    { basePublishedVersion: 4 },
    { programId: 'other-course' },
    { contentId: 'other-content' },
    { payload: null },
    { payload: { ...payload(), title: 'Changed by another author' } },
    { payload: { ...payload(), jsonBody: { ...parseCodingAuthoringDraft(drafts['standard-io']) } } },
  ])('does not claim Saved for a mismatched acknowledgement %#', (change) => {
    expect(() => assertLearningCodingSaveAcknowledgement({ ...acknowledgement(), ...change }, expected())).toThrow();
  });

  it('compares JSON object structure rather than property insertion order', () => {
    const value = acknowledgement();
    const { title, ...rest } = value.payload;
    value.payload = { ...rest, title };
    expect(() => assertLearningCodingSaveAcknowledgement(value, expected())).not.toThrow();
  });

  it('rejects empty, unscoped and unsafe expected revisions', () => {
    expect(() => assertLearningCodingSaveAcknowledgement(null, expected())).toThrow();
    expect(() => assertLearningCodingSaveAcknowledgement(acknowledgement(), { ...expected(), programId: '' })).toThrow();
    expect(() => assertLearningCodingSaveAcknowledgement(acknowledgement(), { ...expected(), revision: Number.MAX_SAFE_INTEGER })).toThrow();
  });
});
