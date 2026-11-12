import '@testing-library/jest-dom/vitest';

import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { FormProvider, useForm } from 'react-hook-form';
import { createEssayEntry, safeParseQuizEntry, type EssayEntry } from '@game-guild/quiz';
import { EssayEditor } from './editor';

vi.mock('./lexical-adapter', () => ({
  EssayLexicalEditor: ({ initialState }: { initialState?: unknown }) => <output data-testid="model-answer-state">{JSON.stringify(initialState)}</output>,
}));

afterEach(cleanup);

function EssayEditorForm({
  onSubmit,
  defaultValues = createEssayEntry('Explain your reasoning.'),
}: {
  onSubmit: (entry: EssayEntry) => void;
  defaultValues?: EssayEntry;
}) {
  const form = useForm<EssayEntry>({
    defaultValues,
  });

  return (
    <FormProvider {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
        <EssayEditor />
        <button type="submit">Save</button>
      </form>
    </FormProvider>
  );
}

describe('EssayEditor', () => {
  it('keeps empty optional word limits undefined', async () => {
    const onSubmit = vi.fn();
    render(<EssayEditorForm onSubmit={onSubmit} />);

    fireEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
    const entry = onSubmit.mock.calls[0]![0];
    expect(entry.minWordCount).toBeUndefined();
    expect(entry.maxWordCount).toBeUndefined();
    expect(safeParseQuizEntry(entry).success).toBe(true);
  });

  it('converts provided word limits to numbers', async () => {
    const onSubmit = vi.fn();
    render(<EssayEditorForm onSubmit={onSubmit} />);
    const [minimum, maximum] = screen.getAllByRole('spinbutton');

    fireEvent.change(minimum!, { target: { value: '100' } });
    fireEvent.change(maximum!, { target: { value: '500' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
    expect(onSubmit.mock.calls[0]![0]).toEqual(
      expect.objectContaining({
        minWordCount: 100,
        maxWordCount: 500,
      }),
    );
  });

  it('hydrates a model answer from its canonical plain-text fallback', () => {
    const expected = 'Ownership gives each Rust value one owner.';
    render(
      <EssayEditorForm
        onSubmit={vi.fn()}
        defaultValues={{
          ...createEssayEntry('Explain ownership.'),
          correctAnswerPlain: expected,
        }}
      />,
    );

    const state = JSON.parse(screen.getByTestId('model-answer-state').textContent ?? 'null');
    expect(state.root.children[0].children[0].text).toBe(expected);
  });
});
