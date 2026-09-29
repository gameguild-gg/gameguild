import { describe, expect, it } from 'vitest';
import { selectProposalKind } from './copilot-kinds';

describe('selectProposalKind matrix', () => {
  it('Markdown + selection → InsertAtCursor', () => {
    expect(selectProposalKind('Markdown', true, 'content')).toBe('InsertAtCursor');
  });

  it('Markdown + no selection → ReplaceDocument', () => {
    expect(selectProposalKind('Markdown', false, 'content')).toBe('ReplaceDocument');
  });

  it('Lexical + selection → LexicalPatch (never InsertAtCursor)', () => {
    const kind = selectProposalKind('Lexical', true, 'content');
    expect(kind).toBe('LexicalPatch');
    expect(kind).not.toBe('InsertAtCursor');
  });

  it('Lexical + no selection → LexicalPatch (never ReplaceDocument)', () => {
    const kind = selectProposalKind('Lexical', false, 'content');
    expect(kind).toBe('LexicalPatch');
    expect(kind).not.toBe('ReplaceDocument');
  });

  it('metadata mode wins over selection and format', () => {
    expect(selectProposalKind('Markdown', true, 'metadata')).toBe('MetadataPatch');
    expect(selectProposalKind('Lexical', true, 'metadata')).toBe('MetadataPatch');
  });

  it('every matrix cell maps to a kind the backend allows', () => {
    const allowed: Record<string, string[]> = {
      Markdown: ['ReplaceDocument', 'InsertAtCursor', 'MetadataPatch'],
      Lexical: ['LexicalPatch', 'MetadataPatch'],
    };
    for (const format of ['Markdown', 'Lexical'] as const) {
      for (const hasSelection of [true, false]) {
        for (const mode of ['content', 'metadata'] as const) {
          expect(allowed[format]).toContain(selectProposalKind(format, hasSelection, mode));
        }
      }
    }
  });
});
