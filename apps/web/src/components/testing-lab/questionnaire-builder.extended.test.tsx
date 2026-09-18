import type {
  TestingLabQuestionnaireQuestion,
  TestingLabQuestionnaireSchema,
} from '@game-guild/client';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it } from 'vitest';
import { QuestionnaireBuilder } from './questionnaire-builder';

function Harness({
  initial,
  required = false,
}: {
  initial: TestingLabQuestionnaireSchema;
  required?: boolean;
}) {
  const [value, setValue] = useState(initial);
  return (
    <>
      <QuestionnaireBuilder value={value} onChange={setValue} required={required} />
      <output data-testid="schema">{JSON.stringify(value)}</output>
    </>
  );
}

function schemaText() {
  return screen.getByTestId('schema').textContent ?? '';
}

describe('QuestionnaireBuilder extended behavior', () => {
  it('normalizes an incomplete free-text question while editing it', async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{
          questions: [
            { id: 'question-1' } as TestingLabQuestionnaireQuestion,
          ],
        }}
      />,
    );

    expect(screen.getByLabelText('Questionnaire title')).toHaveValue('');
    expect(screen.getByLabelText('Prompt')).toHaveValue('');
    expect(screen.getByLabelText('Answer type')).toHaveValue('FreeText');
    expect(screen.getByRole('checkbox', { name: 'Required question' })).not.toBeChecked();
    await user.click(screen.getByRole('checkbox', { name: 'Required question' }));
    await user.selectOptions(screen.getByLabelText('Answer type'), 'MultipleChoice');
    expect(screen.getByText('Options')).toBeInTheDocument();
    await user.selectOptions(screen.getByLabelText('Answer type'), 'FreeText');
    await user.type(screen.getByLabelText('Questionnaire title'), 'Exit survey');
    await user.type(screen.getByLabelText('Prompt'), 'What changed?');

    expect(schemaText()).toContain('"title":"Exit survey"');
    expect(schemaText()).toContain('"type":"FreeText"');
    expect(schemaText()).toContain('"options":[]');
    expect(schemaText()).toContain('"required":true');
  });

  it('moves questions in both directions and deletes a question', async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{
          title: 'Order',
          questions: [
            { id: 'first', prompt: 'First', type: 'FreeText', options: [] },
            { id: 'second', prompt: 'Second', type: 'FreeText', options: [] },
            { id: 'third', prompt: 'Third', type: 'FreeText', options: [] },
          ],
        }}
      />,
    );

    expect(screen.getAllByRole('button', { name: 'Move question up' })[0]).toBeDisabled();
    expect(screen.getAllByRole('button', { name: 'Move question down' })[2]).toBeDisabled();
    await user.click(screen.getAllByRole('button', { name: 'Move question up' })[1]!);
    expect(schemaText().indexOf('Second')).toBeLessThan(schemaText().indexOf('First'));
    await user.click(screen.getAllByRole('button', { name: 'Move question down' })[0]!);
    expect(schemaText().indexOf('First')).toBeLessThan(schemaText().indexOf('Second'));
    await user.click(screen.getAllByRole('button', { name: 'Delete question' })[1]!);
    expect(schemaText()).not.toContain('Second');
  });

  it('normalizes missing options on an incomplete choice question', async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{
          questions: [
            { id: 'choice', prompt: 'Choose', type: 'SingleChoice' },
          ],
        }}
      />,
    );

    await user.click(screen.getByRole('checkbox', { name: 'Required question' }));
    expect(schemaText()).toContain('"options":[]');
  });

  it('edits, adds, and removes choice options before returning to free text', async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{
          title: 'Choices',
          questions: [
            {
              id: 'choice',
              prompt: 'Choose',
              type: 'SingleChoice',
              options: [
                {},
                { id: 'existing', label: 'Existing' },
              ],
            },
          ],
        }}
      />,
    );

    const optionInputs = screen.getAllByRole('textbox', { name: /Option/ });
    await user.type(optionInputs[0]!, 'Generated label');
    await user.clear(optionInputs[1]!);
    expect(schemaText()).toContain('Generated label');
    await user.click(screen.getByRole('button', { name: 'Add option' }));
    expect(screen.getAllByRole('textbox', { name: /Option/ })).toHaveLength(3);
    await user.click(screen.getAllByRole('button', { name: 'Delete option' })[0]!);
    expect(screen.getAllByRole('textbox', { name: /Option/ })).toHaveLength(2);
    await user.selectOptions(screen.getByLabelText('Answer type'), 'FreeText');
    expect(screen.queryByText('Options')).not.toBeInTheDocument();
    expect(schemaText()).toContain('"options":[]');
  });

  it('creates and clears a display condition using earlier stable question identifiers', async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{
          title: 'Conditions',
          questions: [
            { prompt: 'Unidentified', type: 'FreeText', options: [] },
            { id: 'source', prompt: '', type: 'SingleChoice', options: [{ id: 'yes', label: 'Yes' }] },
            { id: 'target', prompt: 'Details', type: 'FreeText', options: [] },
          ],
        }}
      />,
    );

    const sources = screen.getAllByLabelText('Show condition (optional)');
    expect(sources[1]).toHaveTextContent('source');
    await user.selectOptions(sources[1]!, 'source');
    const conditionSelects = screen.getAllByRole('combobox');
    const operator = conditionSelects.find(
      (element) => element.textContent?.includes('does not equal') && !element.hasAttribute('disabled'),
    )!;
    expect(operator).not.toBeDisabled();
    await user.selectOptions(operator, 'NotEquals');
    const valueInput = screen
      .getAllByPlaceholderText('Answer or option ID')
      .find((element) => !element.hasAttribute('disabled'))!;
    await user.type(valueInput, 'yes');
    expect(schemaText()).toContain('"operator":"NotEquals"');
    expect(schemaText()).toContain('"value":"yes"');
    await user.selectOptions(sources[1]!, '');
    expect(schemaText()).not.toContain('"condition"');
  });

  it('updates an existing condition with its preserved value', async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{
          title: 'Existing condition',
          questions: [
            { id: 'source', prompt: 'Source', type: 'SingleChoice', options: [{ id: 'yes', label: 'Yes' }] },
            {
              id: 'target',
              prompt: 'Target',
              type: 'FreeText',
              options: [],
              condition: { questionId: 'source', operator: 'Includes', value: 'yes' },
            },
          ],
        }}
      />,
    );

    const source = screen.getByLabelText('Show condition (optional)');
    await user.selectOptions(source, 'source');
    const operator = screen.getAllByRole('combobox').find((element) => element.textContent?.includes('does not equal'))!;
    await user.selectOptions(operator, 'Equals');
    const conditionValue = screen.getByPlaceholderText('Answer or option ID');
    await user.clear(conditionValue);
    await user.type(conditionValue, 'no');
    expect(schemaText()).toContain('"operator":"Equals"');
    expect(schemaText()).toContain('"value":"no"');
  });

  it('previews a questionnaire and returns to the builder', async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{
          title: 'Preview title',
          questions: [
            { id: 'free', prompt: 'Preview prompt', type: 'FreeText', options: [] },
          ],
        }}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Preview' }));
    expect(screen.getByText('Preview title')).toBeInTheDocument();
    await user.type(screen.getByLabelText('Preview prompt'), 'Preview answer');
    await user.click(screen.getByRole('button', { name: 'Back to builder' }));
    expect(screen.getByLabelText('Questionnaire title')).toBeInTheDocument();
  });

  it('keeps preview disabled for an empty optional questionnaire', () => {
    render(<Harness initial={{}} />);
    expect(screen.getByRole('button', { name: 'Preview' })).toBeDisabled();
    expect(screen.queryByText(/add at least one feedback question/i)).not.toBeInTheDocument();
  });
});
