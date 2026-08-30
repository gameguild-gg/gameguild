import type { TestingLabQuestionnaireOutput, TestingLabQuestionnaireSchema } from '@game-guild/client';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { QuestionnaireFieldset } from './questionnaire-fieldset';

function Harness({ schema, onComplete }: { schema: TestingLabQuestionnaireSchema; onComplete?: () => void }) {
  const [value, setValue] = useState<TestingLabQuestionnaireOutput>({ answers: [] });
  return <><QuestionnaireFieldset schema={schema} value={value} onChange={setValue} onComplete={onComplete} submitLabel="Finish" /><output data-testid="answers">{JSON.stringify(value)}</output></>;
}

describe('QuestionnaireFieldset', () => {
  it('serializes a controlled free-text answer and completes the questionnaire', async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();
    render(<Harness schema={{ title: 'Feedback', questions: [{ id: 'clarity', prompt: 'What was clear?', type: 'FreeText', required: true, options: [] }] }} onComplete={onComplete} />);

    await user.type(screen.getByLabelText('What was clear?'), 'The controls');
    expect(screen.getByTestId('answers')).toHaveTextContent('"questionId":"clarity"');
    expect(screen.getByTestId('answers')).toHaveTextContent('"textValue":"The controls"');
    await user.click(screen.getByRole('button', { name: 'Finish' }));
    expect(onComplete).toHaveBeenCalledOnce();
  });

  it('reveals conditional questions from stable option identifiers', async () => {
    const user = userEvent.setup();
    render(<Harness schema={{ title: 'Conditional', questions: [
      { id: 'played', prompt: 'Did you finish?', type: 'SingleChoice', required: true, options: [{ id: 'yes', label: 'Yes' }, { id: 'no', label: 'No' }] },
      { id: 'details', prompt: 'What worked?', type: 'FreeText', required: false, options: [], condition: { questionId: 'played', operator: 'Equals', value: 'yes' } },
    ] }} />);

    expect(screen.queryByText('What worked?')).not.toBeInTheDocument();
    await user.click(screen.getByText('Yes'));
    expect(screen.getByText('What worked?')).toBeInTheDocument();
  });
});
