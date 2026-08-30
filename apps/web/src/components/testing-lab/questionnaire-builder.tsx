'use client';

import type {
  TestingLabQuestionnaireConditionOperator,
  TestingLabQuestionnaireOutput,
  TestingLabQuestionnaireQuestion,
  TestingLabQuestionnaireQuestionType,
  TestingLabQuestionnaireSchema,
} from '@game-guild/client';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { ArrowDown, ArrowUp, Eye, Plus, Trash2 } from 'lucide-react';
import { useId, useState } from 'react';
import { QuestionnaireFieldset } from './questionnaire-fieldset';

function stableId(prefix: string) {
  return `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 7)}`;
}

function normalizeQuestion(question: TestingLabQuestionnaireQuestion): TestingLabQuestionnaireQuestion {
  const type = question.type ?? 'FreeText';
  return {
    ...question,
    type,
    options: type === 'FreeText' ? [] : question.options ?? [],
  };
}

export function QuestionnaireBuilder({
  value,
  onChange,
  required,
}: {
  value: TestingLabQuestionnaireSchema;
  onChange: (value: TestingLabQuestionnaireSchema) => void;
  required?: boolean;
}) {
  const titleId = useId();
  const [preview, setPreview] = useState(false);
  const [previewAnswers, setPreviewAnswers] = useState<TestingLabQuestionnaireOutput>({ answers: [] });
  const questions = value.questions ?? [];

  function updateQuestion(index: number, patch: Partial<TestingLabQuestionnaireQuestion>) {
    const next = [...questions];
    next[index] = normalizeQuestion({ ...next[index], ...patch });
    onChange({ ...value, questions: next });
  }

  function move(index: number, offset: -1 | 1) {
    const target = index + offset;
    if (target < 0 || target >= questions.length) return;
    const next = [...questions];
    [next[index], next[target]] = [next[target], next[index]];
    onChange({ ...value, questions: next });
  }

  if (preview) {
    return (
      <div className="space-y-4 rounded-md border bg-muted/20 p-4">
        <div className="flex items-center justify-between gap-3">
          <Badge variant="secondary">Preview</Badge>
          <Button type="button" variant="outline" size="sm" onClick={() => setPreview(false)}>Back to builder</Button>
        </div>
        <QuestionnaireFieldset schema={value} value={previewAnswers} onChange={setPreviewAnswers} submitLabel="Finish preview" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor={titleId}>Questionnaire title</Label>
        <Input
          id={titleId}
          value={value.title ?? ''}
          onChange={(event) => onChange({ ...value, title: event.currentTarget.value })}
          placeholder="Playtest feedback"
        />
      </div>

      {questions.map((question, index) => (
        <section key={question.id ?? index} className="space-y-3 rounded-md border p-4">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <Badge variant="outline">Question {index + 1}</Badge>
            <div className="flex gap-1">
              <Button type="button" size="icon-sm" variant="ghost" aria-label="Move question up" disabled={index === 0} onClick={() => move(index, -1)}><ArrowUp /></Button>
              <Button type="button" size="icon-sm" variant="ghost" aria-label="Move question down" disabled={index === questions.length - 1} onClick={() => move(index, 1)}><ArrowDown /></Button>
              <Button type="button" size="icon-sm" variant="ghost" aria-label="Delete question" onClick={() => onChange({ ...value, questions: questions.filter((_, candidate) => candidate !== index) })}><Trash2 /></Button>
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-[minmax(0,1fr)_180px]">
            <div className="space-y-2">
              <Label htmlFor={`question-prompt-${question.id}`}>Prompt</Label>
              <Input id={`question-prompt-${question.id}`} value={question.prompt ?? ''} onChange={(event) => updateQuestion(index, { prompt: event.currentTarget.value })} />
            </div>
            <div className="space-y-2">
              <Label htmlFor={`question-type-${question.id}`}>Answer type</Label>
              <select
                id={`question-type-${question.id}`}
                value={question.type ?? 'FreeText'}
                onChange={(event) => updateQuestion(index, { type: event.currentTarget.value as TestingLabQuestionnaireQuestionType })}
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
              >
                <option value="FreeText">Free text</option>
                <option value="SingleChoice">Single choice</option>
                <option value="MultipleChoice">Multiple choice</option>
              </select>
            </div>
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={question.required ?? false} onChange={(event) => updateQuestion(index, { required: event.currentTarget.checked })} />
            Required question
          </label>

          {question.type !== 'FreeText' ? (
            <div className="space-y-2">
              <Label>Options</Label>
              {(question.options ?? []).map((option, optionIndex) => (
                <div key={option.id ?? optionIndex} className="flex gap-2">
                  <Input
                    value={option.label ?? ''}
                    aria-label={`Option ${optionIndex + 1}`}
                    onChange={(event) => {
                      const options = [...(question.options ?? [])];
                      options[optionIndex] = { ...option, label: event.currentTarget.value };
                      updateQuestion(index, { options });
                    }}
                  />
                  <Button type="button" variant="ghost" size="icon-sm" aria-label="Delete option" onClick={() => updateQuestion(index, { options: (question.options ?? []).filter((_, candidate) => candidate !== optionIndex) })}><Trash2 /></Button>
                </div>
              ))}
              <Button type="button" variant="outline" size="sm" onClick={() => updateQuestion(index, { options: [...(question.options ?? []), { id: stableId('option'), label: '' }] })}>
                <Plus className="mr-2 size-4" /> Add option
              </Button>
            </div>
          ) : null}

          {index > 0 ? (
            <div className="space-y-2 border-t pt-3">
              <Label htmlFor={`condition-source-${question.id}`}>Show condition (optional)</Label>
              <div className="grid gap-2 sm:grid-cols-3">
                <select
                  id={`condition-source-${question.id}`}
                  value={question.condition?.questionId ?? ''}
                  onChange={(event) => updateQuestion(index, { condition: event.currentTarget.value ? { questionId: event.currentTarget.value, operator: question.condition?.operator ?? 'Equals', value: question.condition?.value ?? '' } : undefined })}
                  className="flex h-9 rounded-md border border-input bg-background px-3 text-sm"
                >
                  <option value="">Always show</option>
                  {questions.slice(0, index).flatMap((source) => source.id
                    ? [<option key={source.id} value={source.id}>{source.prompt || source.id}</option>]
                    : [])}
                </select>
                <select
                  value={question.condition?.operator ?? 'Equals'}
                  disabled={!question.condition}
                  onChange={(event) => question.condition && updateQuestion(index, { condition: { ...question.condition, operator: event.currentTarget.value as TestingLabQuestionnaireConditionOperator } })}
                  className="flex h-9 rounded-md border border-input bg-background px-3 text-sm"
                >
                  <option value="Equals">equals</option>
                  <option value="NotEquals">does not equal</option>
                  <option value="Includes">includes</option>
                </select>
                <Input
                  value={question.condition?.value ?? ''}
                  disabled={!question.condition}
                  placeholder="Answer or option ID"
                  onChange={(event) => question.condition && updateQuestion(index, { condition: { ...question.condition, value: event.currentTarget.value } })}
                />
              </div>
            </div>
          ) : null}
        </section>
      ))}

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          variant="outline"
          onClick={() => onChange({
            ...value,
            questions: [...questions, { id: stableId('question'), prompt: '', type: 'FreeText', required: true, options: [] }],
          })}
        >
          <Plus className="mr-2 size-4" /> Add question
        </Button>
        <Button type="button" variant="ghost" disabled={questions.length === 0} onClick={() => setPreview(true)}>
          <Eye className="mr-2 size-4" /> Preview
        </Button>
      </div>
      {required && questions.length === 0 ? <p className="text-sm text-destructive">Add at least one feedback question.</p> : null}
    </div>
  );
}
