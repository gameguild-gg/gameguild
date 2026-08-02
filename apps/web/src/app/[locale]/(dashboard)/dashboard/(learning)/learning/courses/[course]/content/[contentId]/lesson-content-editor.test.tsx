import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { LessonContentEditor } from './lesson-content-editor';

vi.mock('@/components/block-content-editor/lexical-surface', () => ({
  LexicalSurface: ({ accessibleLabel }: { accessibleLabel?: string }) => (
    <div role="textbox" aria-label={accessibleLabel} contentEditable />
  ),
}));

describe('LessonContentEditor', () => {
  it('gives the rich-text body editor an accessible name', async () => {
    render(<LessonContentEditor itemId="lesson-1" initialState={null} onChange={vi.fn()} />);

    expect(await screen.findByRole('textbox', { name: 'Body' })).toBeInTheDocument();
  });
});
