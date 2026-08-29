'use client';

import type {
  TestingLabQuestionnaireAnswer,
  TestingLabQuestionnaireOutput,
  TestingLabQuestionnaireQuestion,
  TestingLabQuestionnaireSchema,
} from '@game-guild/client';
import {
  Questionnaire,
  QuestionnaireActions,
  QuestionnaireChoice,
  QuestionnaireChoices,
  QuestionnaireError,
  QuestionnaireInput,
  QuestionnaireItem,
  QuestionnaireNext,
  QuestionnairePrevious,
  QuestionnaireProgress,
  QuestionnaireSkip,
  QuestionnaireSubmit,
  QuestionnaireTitle,
} from '@game-guild/ui/components/questionnaire';

function valuesFor(answer?: TestingLabQuestionnaireAnswer) {
  if ((answer?.selectedOptionIds?.length ?? 0) > 0) return answer?.selectedOptionIds ?? [];
  return answer?.textValue?.trim() ? [answer.textValue] : [];
}

function isActive(question: TestingLabQuestionnaireQuestion, answers: TestingLabQuestionnaireAnswer[]) {
  const condition = question.condition;
  if (!condition?.questionId || !condition.value) return true;
  const sourceValues = valuesFor(answers.find((answer) => answer.questionId === condition.questionId));
  const contains = sourceValues.includes(condition.value);
  if (condition.operator === 'NotEquals') return sourceValues.length > 0 && !contains;
  if (condition.operator === 'Equals') return sourceValues.length === 1 && contains;
  return contains;
}

function setAnswer(
  output: TestingLabQuestionnaireOutput,
  questionId: string,
  answer: Omit<TestingLabQuestionnaireAnswer, 'questionId'>,
) {
  const answers = [...(output.answers ?? [])];
  const index = answers.findIndex((candidate) => candidate.questionId === questionId);
  const next = { questionId, ...answer };
  if (index >= 0) answers[index] = next;
  else answers.push(next);
  return { answers } satisfies TestingLabQuestionnaireOutput;
}

export function QuestionnaireFieldset({
  schema,
  value,
  onChange,
  onComplete,
  submitLabel = 'Continue',
  description,
}: {
  schema?: TestingLabQuestionnaireSchema | null;
  value: TestingLabQuestionnaireOutput;
  onChange: (value: TestingLabQuestionnaireOutput) => void;
  onComplete?: () => void;
  submitLabel?: string;
  description?: string;
}) {
  const questions = (schema?.questions ?? []).filter(
    (question): question is TestingLabQuestionnaireQuestion & { id: string } =>
      Boolean(question.id) && isActive(question, value.answers ?? []),
  );

  if (questions.length === 0) {
    return (
      <div className="space-y-3 rounded-md border border-dashed p-4 text-sm text-muted-foreground">
        <p>This questionnaire has no questions.</p>
        {onComplete ? <button type="button" className="text-sm font-medium text-foreground underline underline-offset-4" onClick={onComplete}>{submitLabel}</button> : null}
      </div>
    );
  }

  return (
    <Questionnaire
      items={questions.map((question) => ({
        name: question.id,
        required: question.required,
        choices: (question.options ?? []).flatMap((option) =>
          option.id ? [{ value: option.id }] : [],
        ),
      }))}
      shortcuts="letters"
      onSubmit={(event) => {
        event.preventDefault();
        onComplete?.();
      }}
    >
      <div className="flex items-center justify-between gap-3 text-xs text-muted-foreground">
        <span>{schema?.title || 'Questionnaire'}</span>
        <QuestionnaireProgress>
          {questions.length} {questions.length === 1 ? 'question' : 'questions'}
        </QuestionnaireProgress>
      </div>
      {description ? <p className="text-sm text-pretty text-muted-foreground">{description}</p> : null}
      {questions.map((question) => {
        const answer = value.answers?.find((candidate) => candidate.questionId === question.id);
        const selected = answer?.selectedOptionIds ?? [];
        const multiple = question.type === 'MultipleChoice';
        return (
          <QuestionnaireItem
            key={question.id}
            name={question.id}
            required={question.required}
            multiple={multiple}
          >
            <QuestionnaireTitle>
              {question.prompt}
              {question.required ? <span className="ml-1 text-destructive" aria-hidden="true">*</span> : null}
            </QuestionnaireTitle>
            {question.type === 'FreeText' ? (
              <QuestionnaireInput
                aria-label={question.prompt ?? question.id}
                value={answer?.textValue ?? ''}
                placeholder="Type your answer"
                onChange={(event) =>
                  onChange(setAnswer(value, question.id, { textValue: event.currentTarget.value, selectedOptionIds: [] }))
                }
              />
            ) : (
              <QuestionnaireChoices>
                {(question.options ?? []).map((option) =>
                  option.id ? (
                    <QuestionnaireChoice
                      key={option.id}
                      value={option.id}
                      checked={selected.includes(option.id)}
                      onChange={(event) => {
                        const nextSelected = multiple
                          ? event.currentTarget.checked
                            ? [...selected, option.id!]
                            : selected.filter((id) => id !== option.id)
                          : event.currentTarget.checked
                            ? [option.id!]
                            : [];
                        onChange(setAnswer(value, question.id, { textValue: null, selectedOptionIds: nextSelected }));
                      }}
                    >
                      {option.label || option.id}
                    </QuestionnaireChoice>
                  ) : null,
                )}
              </QuestionnaireChoices>
            )}
            <QuestionnaireError>Please answer this required question.</QuestionnaireError>
          </QuestionnaireItem>
        );
      })}
      <QuestionnaireActions>
        <QuestionnairePrevious>Previous</QuestionnairePrevious>
        <QuestionnaireSkip>Skip</QuestionnaireSkip>
        <QuestionnaireNext>Next</QuestionnaireNext>
        <QuestionnaireSubmit>{submitLabel}</QuestionnaireSubmit>
      </QuestionnaireActions>
    </Questionnaire>
  );
}
