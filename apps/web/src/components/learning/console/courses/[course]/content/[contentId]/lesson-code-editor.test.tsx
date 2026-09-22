import { fireEvent, render, screen } from '@testing-library/react';
import type { ComponentProps, ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ monaco: vi.fn(() => null) }));

vi.mock('@game-guild/ui/components/label', () => ({
  Label: ({ children }: { children: ReactNode }) => <label>{children}</label>,
}));
vi.mock('@game-guild/ui/components/skeleton', () => ({
  Skeleton: () => <div data-testid="editor-skeleton" />,
}));
vi.mock('@/components/block-content-editor/extras/code-studio/monaco-code-editor', () => ({
  MonacoCodeEditor: (props: ComponentProps<'textarea'> & {
    value: string;
    onChange: (value: string) => void;
    onCursorOffsetChange?: (offset: number) => void;
    ariaLabel: string;
  }) => {
    mocks.monaco(props);
    return (
      <>
        <textarea
          aria-label={props.ariaLabel}
          value={props.value}
          onChange={(event) => props.onChange(event.target.value)}
        />
        <button type="button" onClick={() => props.onCursorOffsetChange?.(3)}>Move cursor</button>
      </>
    );
  },
}));

import { LessonCodeEditor } from './lesson-code-editor';

describe('LessonCodeEditor', () => {
  it('loads Monaco lazily and relays content and cursor changes', async () => {
    const onChange = vi.fn();
    const onCursorOffsetChange = vi.fn();
    render(
      <LessonCodeEditor
        initialValue="# Lesson"
        language="markdown"
        placeholder="Write the lesson body."
        onChange={onChange}
        onCursorOffsetChange={onCursorOffsetChange}
      />,
    );

    expect(screen.getAllByTestId('editor-skeleton')).toHaveLength(2);
    expect(screen.getByText('Write the lesson body.')).toBeInTheDocument();
    const editor = await screen.findByRole('textbox', { name: 'Lesson body' });
    expect(editor).toHaveValue('# Lesson');
    fireEvent.change(editor, { target: { value: '<h1>Updated</h1>' } });
    expect(onChange).toHaveBeenCalledWith('<h1>Updated</h1>');
    expect(editor).toHaveValue('<h1>Updated</h1>');
    fireEvent.click(screen.getByRole('button', { name: 'Move cursor' }));
    expect(onCursorOffsetChange).toHaveBeenCalledWith(3);
    expect(mocks.monaco).toHaveBeenLastCalledWith(expect.objectContaining({
      language: 'markdown', height: '100%', value: '<h1>Updated</h1>',
    }));
  });

  it('supports HTML without optional helper or cursor callback', async () => {
    render(<LessonCodeEditor initialValue="<p>Lesson</p>" language="html" onChange={vi.fn()} />);
    expect(screen.queryByText('Write the lesson body.')).not.toBeInTheDocument();
    expect(await screen.findByRole('textbox', { name: 'Lesson body' })).toHaveValue('<p>Lesson</p>');
    fireEvent.click(screen.getByRole('button', { name: 'Move cursor' }));
    expect(mocks.monaco).toHaveBeenLastCalledWith(expect.objectContaining({ language: 'html' }));
  });
});
