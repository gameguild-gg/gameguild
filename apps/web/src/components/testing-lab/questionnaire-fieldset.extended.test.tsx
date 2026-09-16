import type {
  TestingLabQuestionnaireOutput,
  TestingLabQuestionnaireQuestion,
  TestingLabQuestionnaireSchema,
} from '@game-guild/client';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { QuestionnaireFieldset } from './questionnaire-fieldset';

function Harness({
  schema,
  initial = { answers: [] },
  description,
  onComplete,
  submitLabel,
}: {
  schema?: TestingLabQuestionnaireSchema | null;
  initial?: TestingLabQuestionnaireOutput;
  description?: string;
  onComplete?: () => void;
  submitLabel?: string;
}) {
  const [value, setValue] = useState(initial);
  return (
    <>
      <QuestionnaireFieldset
        schema={schema}
        value={value}
        onChange={setValue}
        onComplete={onComplete}
        submitLabel={submitLabel}
        description={description}
      />
      <output data-testid="answers">{JSON.stringify(value)}</output>
    </>
  );
}

function conditionalQuestion(
  operator: 'Equals' | 'NotEquals' | 'Includes',
): TestingLabQuestionnaireQuestion {
  return {
    id: 'target',
    prompt: 'Conditional target',
    type: 'FreeText',
    options: [],
    condition: { questionId: 'source', operator, value: 'yes' },
  };
}

function renderConditional(
  operator: 'Equals' | 'NotEquals' | 'Includes',
  answer?: TestingLabQuestionnaireOutput['answers'][number],
) {
  return render(
    <QuestionnaireFieldset
      schema={{ questions: [conditionalQuestion(operator)] }}
      value={{ answers: answer ? [answer] : [] }}
      onChange={() => undefined}
    />,
  );
}

describe('QuestionnaireFieldset extended behavior', () => {
  it('handles empty schemas, id-less questions, and optional completion', async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();
    const { rerender } = render(
      <QuestionnaireFieldset value={{}} onChange={() => undefined} />,
    );

    expect(screen.getByText('This questionnaire has no questions.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Continue' })).not.toBeInTheDocument();

    rerender(
      <QuestionnaireFieldset
        schema={{ questions: [{ prompt: 'Missing ID', type: 'FreeText' }] }}
        value={{ answers: [] }}
        onChange={() => undefined}
        onComplete={onComplete}
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Continue' }));
    expect(onComplete).toHaveBeenCalledOnce();
  });

  it('renders fallbacks, descriptions, plural progress, and incomplete options', async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{}}
        description="Answer every applicable question."
        schema={{
          title: '',
          questions: [
            { id: 'free', type: 'FreeText', required: true },
            {
              id: 'choice',
              prompt: 'Pick one',
              type: 'SingleChoice',
              options: [{ label: 'Missing ID' }, { id: 'stable-option' }],
            },
          ],
        }}
      />,
    );

    expect(screen.getByText('Questionnaire')).toBeInTheDocument();
    expect(screen.getByText('Answer every applicable question.')).toBeInTheDocument();
    expect(screen.getByText('2 questions')).toBeInTheDocument();
    await user.type(screen.getByLabelText('free'), 'Answer');
    await user.click(screen.getByRole('button', { name: 'Next' }));
    expect(screen.queryByText('Missing ID')).not.toBeInTheDocument();
    await user.click(screen.getByText('stable-option'));
    expect(screen.getByTestId('answers')).toHaveTextContent('"selectedOptionIds":["stable-option"]');
    await user.click(screen.getByRole('button', { name: 'Continue' }));
  });

  it('adds and removes multiple-choice selections without losing other answers', async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{
          answers: [{ questionId: 'existing', textValue: 'Preserved', selectedOptionIds: [] }],
        }}
        schema={{
          title: 'Multiple',
          questions: [
            {
              id: 'multiple',
              prompt: 'Select all',
              type: 'MultipleChoice',
              options: [
                { id: 'a', label: 'Alpha' },
                { id: 'b', label: 'Beta' },
              ],
            },
          ],
        }}
      />,
    );

    await user.click(screen.getByText('Alpha'));
    await user.click(screen.getByText('Beta'));
    expect(screen.getByTestId('answers')).toHaveTextContent('"selectedOptionIds":["a","b"]');
    await user.click(screen.getByText('Alpha'));
    expect(screen.getByTestId('answers')).toHaveTextContent('"selectedOptionIds":["b"]');
    expect(screen.getByTestId('answers')).toHaveTextContent('Preserved');
  });

  it('replaces the current single-choice selection', async () => {
    const user = userEvent.setup();
    render(
      <Harness
        schema={{
          questions: [
            {
              id: 'single',
              prompt: 'Pick one',
              type: 'SingleChoice',
              options: [
                { id: 'a', label: 'Alpha' },
                { id: 'b', label: 'Beta' },
              ],
            },
          ],
        }}
      />,
    );

    await user.click(screen.getByText('Alpha'));
    await user.click(screen.getByText('Beta'));
    expect(screen.getByTestId('answers')).toHaveTextContent('"selectedOptionIds":["b"]');
  });

  it('evaluates Equals against text and selected option values', () => {
    const { rerender } = renderConditional('Equals', {
      questionId: 'source',
      textValue: ' yes ',
      selectedOptionIds: [],
    });
    expect(screen.getByText('Conditional target')).toBeInTheDocument();

    rerender(
      <QuestionnaireFieldset
        schema={{ questions: [conditionalQuestion('Equals')] }}
        value={{ answers: [{ questionId: 'source', selectedOptionIds: ['yes', 'other'] }] }}
        onChange={() => undefined}
      />,
    );
    expect(screen.getByText('This questionnaire has no questions.')).toBeInTheDocument();
  });

  it('evaluates NotEquals only when the source has a value', () => {
    const { rerender } = renderConditional('NotEquals');
    expect(screen.getByText('This questionnaire has no questions.')).toBeInTheDocument();

    rerender(
      <QuestionnaireFieldset
        schema={{ questions: [conditionalQuestion('NotEquals')] }}
        value={{ answers: [{ questionId: 'source', textValue: 'no', selectedOptionIds: [] }] }}
        onChange={() => undefined}
      />,
    );
    expect(screen.getByText('Conditional target')).toBeInTheDocument();

    rerender(
      <QuestionnaireFieldset
        schema={{ questions: [conditionalQuestion('NotEquals')] }}
        value={{ answers: [{ questionId: 'source', textValue: 'yes', selectedOptionIds: [] }] }}
        onChange={() => undefined}
      />,
    );
    expect(screen.getByText('This questionnaire has no questions.')).toBeInTheDocument();
  });

  it('evaluates Includes and treats whitespace-only text as empty', () => {
    const { rerender } = renderConditional('Includes', {
      questionId: 'source',
      selectedOptionIds: ['no', 'yes'],
    });
    expect(screen.getByText('Conditional target')).toBeInTheDocument();

    rerender(
      <QuestionnaireFieldset
        schema={{ questions: [conditionalQuestion('Includes')] }}
        value={{ answers: [{ questionId: 'source', textValue: '   ' }] }}
        onChange={() => undefined}
      />,
    );
    expect(screen.getByText('This questionnaire has no questions.')).toBeInTheDocument();
  });

  it('always shows questions with incomplete conditions', () => {
    render(
      <QuestionnaireFieldset
        schema={{
          questions: [
            {
              id: 'missing-source',
              prompt: 'Missing source',
              type: 'FreeText',
              condition: { operator: 'Equals', value: 'yes' },
            },
            {
              id: 'missing-value',
              prompt: 'Missing value',
              type: 'FreeText',
              condition: { questionId: 'source', operator: 'Equals' },
            },
          ],
        }}
        value={{ answers: [] }}
        onChange={() => undefined}
      />,
    );
    expect(screen.getByText('2 questions')).toBeInTheDocument();
  });

  it('renders a choice question whose option collection is absent', () => {
    render(
      <QuestionnaireFieldset
        schema={{
          questions: [
            { id: 'choice', prompt: 'No options yet', type: 'SingleChoice' },
          ],
        }}
        value={{ answers: [] }}
        onChange={() => undefined}
      />,
    );
    expect(screen.getByText('No options yet')).toBeInTheDocument();
  });
});
