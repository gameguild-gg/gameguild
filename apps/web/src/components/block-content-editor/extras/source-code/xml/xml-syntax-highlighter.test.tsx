import { render } from '@testing-library/react';
import type { editor, languages } from 'monaco-editor';
import { describe, expect, it, vi } from 'vitest';
import { XMLSyntaxHighlighter } from './xml-syntax-highlighter';

describe('XML comment tokenization', () => {
  it('keeps comments in a dedicated state across lines until their closing delimiter', () => {
    const setMonarchTokensProvider = vi.fn<(language: string, tokens: languages.IMonarchLanguage) => { dispose: () => void }>(() => ({ dispose: vi.fn() }));
    const monaco = {
      languages: { getLanguages: () => [], register: vi.fn(), setMonarchTokensProvider },
      editor: { setModelLanguage: vi.fn() },
    } as unknown as typeof import('monaco-editor');
    const codeEditor = { getModel: () => null } as unknown as editor.IStandaloneCodeEditor;

    render(<XMLSyntaxHighlighter monaco={monaco} editor={codeEditor} />);

    const tokens = setMonarchTokensProvider.mock.calls[0]?.[1];
    expect(tokens?.tokenizer.root).toContainEqual([/<!--/, { token: 'comment', next: '@comment' }]);
    expect(tokens?.tokenizer.comment).toEqual([
      [/-->/, { token: 'comment', next: '@pop' }],
      [/[^-]+/, 'comment'],
      [/-/, 'comment'],
    ]);
    const closingRule = tokens?.tokenizer.comment?.[0];
    const closingPattern = Array.isArray(closingRule) ? closingRule[0] : undefined;
    expect(closingPattern).toBeInstanceOf(RegExp);
    if (!(closingPattern instanceof RegExp)) throw new Error('XML comment closing rule is missing');
    expect(closingPattern.test('-->')).toBe(true);
    expect(closingPattern.test('--!>')).toBe(false);
  });
});
