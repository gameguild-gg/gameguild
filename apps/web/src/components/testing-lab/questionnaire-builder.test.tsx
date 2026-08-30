import type { TestingLabQuestionnaireSchema } from '@game-guild/client';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it } from 'vitest';
import { QuestionnaireBuilder } from './questionnaire-builder';

function Harness() {
  const [value, setValue] = useState<TestingLabQuestionnaireSchema>({ title: 'Feedback', questions: [] });
  return <><QuestionnaireBuilder value={value} onChange={setValue} required /><output data-testid="schema">{JSON.stringify(value)}</output></>;
}

describe('QuestionnaireBuilder', () => {
  it('assigns each builder a unique title input ID', () => {
    const questionnaire = { title: 'Feedback', questions: [] } satisfies TestingLabQuestionnaireSchema;
    render(
      <>
        <QuestionnaireBuilder value={questionnaire} onChange={() => undefined} />
        <QuestionnaireBuilder value={questionnaire} onChange={() => undefined} />
      </>,
    );

    const titleIds = screen.getAllByLabelText('Questionnaire title').map((input) => input.id);
    expect(new Set(titleIds).size).toBe(2);
  });

  it('creates choice questions with stable options and opens preview', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    expect(screen.getByText(/add at least one feedback question/i)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /add question/i }));
    await user.type(screen.getByLabelText('Prompt'), 'Which level was strongest?');
    await user.selectOptions(screen.getByLabelText('Answer type'), 'SingleChoice');
    await user.click(screen.getByRole('button', { name: /add option/i }));
    await user.click(screen.getByRole('button', { name: /add option/i }));
    const options = screen.getAllByRole('textbox', { name: /option/i });
    await user.type(options[0], 'Forest');
    await user.type(options[1], 'Castle');

    expect(screen.getByTestId('schema')).toHaveTextContent('"type":"SingleChoice"');
    expect(screen.getByTestId('schema')).toHaveTextContent('"label":"Forest"');
    await user.click(screen.getByRole('button', { name: /preview/i }));
    expect(screen.getByText('Which level was strongest?')).toBeInTheDocument();
  });
});
