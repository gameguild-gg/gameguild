import { StrictMode, useState } from 'react';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CodingAuthoringSurface } from './authoring-surface';
import { runCodingPreview } from './preview-runner';
import type { CodingPreviewResult } from './types';
import { exerciseValidationFixture } from './testing/exercise-validation-fixtures';

vi.mock('./preview-runner', () => ({ runCodingPreview: vi.fn() }));
vi.mock('@monaco-editor/react', () => ({
  default: ({ value, onChange, options }: { value: string; onChange: (value: string) => void; options?: { readOnly?: boolean; ariaLabel?: string } }) => (
    <textarea aria-label={options?.ariaLabel ?? 'Source code'} value={value} onChange={(event) => onChange(event.target.value)} readOnly={options?.readOnly} />
  ),
}));
const runner = vi.mocked(runCodingPreview);
afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});

function open(initial = exerciseValidationFixture(), strict = false) {
  let current = initial;
  let counter = 0;
  function Host() {
    const [draft, setDraft] = useState(initial);
    return (
      <CodingAuthoringSurface
        contextKey="course-content"
        draft={draft}
        createId={(kind) => `${kind}-${++counter}`}
        onChange={(next) => {
          current = next;
          setDraft(next);
        }}
      />
    );
  }
  const rendered = render(
    strict ? (
      <StrictMode>
        <Host />
      </StrictMode>
    ) : (
      <Host />
    ),
  );
  fireEvent.click(screen.getByRole('tab', { name: 'Validate exercise' }));
  return { ...rendered, current: () => structuredClone(current) };
}
function command(name: string) {
  fireEvent.click(screen.getByRole('button', { name }));
}
function edit(label: string, value: string) {
  fireEvent.change(screen.getByLabelText(label, { exact: true }), { target: { value } });
}
function fieldValue(label: string) {
  const field = screen.getByLabelText(label, { exact: true });
  if (!(field instanceof HTMLInputElement || field instanceof HTMLTextAreaElement || field instanceof HTMLSelectElement))
    throw new Error('Expected a value field.');
  return field.value;
}
function result(passes: boolean): CodingPreviewResult {
  return {
    academic: false,
    scope: 'full',
    score: { score: 99, passed: true, feedback: 'Never displayed as an exercise score.' },
    report: {
      passed: passes ? 2 : 0,
      failed: passes ? 0 : 2,
      totalDurationMs: 2,
      cases: ['Public addition', 'PRIVATE-CASE-CANARY'].map((name) => ({
        name,
        passed: passes,
        durationMs: 1,
        ...(passes ? {} : { diagnostic: 'Expected 5; received -1.' }),
      })),
    },
  };
}

describe('UX04-B instructor preparation task', () => {
  it('survives React StrictMode setup/cleanup replay without a disposed owner or compiler boot', () => {
    const view = open(exerciseValidationFixture(), true);
    edit('Solution name', 'StrictMode reference');
    expect(view.current().validationSolutions[0]?.name).toBe('StrictMode reference');
    expect(runner).not.toHaveBeenCalled();
    expect(screen.queryByRole('alert')).toBeNull();
  });
  it('edits protected source copies without mutating starter code or booting a compiler', () => {
    const view = open();
    edit('Protected solution code', 'int sum(int a, int b) { return a + b + 1; }');
    edit('Solution name', 'Off by one');
    expect(view.current().validationSolutions[0]?.files[0]?.content).toContain('+ 1');
    expect(view.current().files[0]?.content).toBe(exerciseValidationFixture().files[0]?.content);
    expect(runner).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('tab', { name: 'Problem' }));
    fireEvent.click(screen.getByRole('tab', { name: 'Validate exercise' }));
    expect(screen.getByDisplayValue('Off by one')).toBeTruthy();
    expect(fieldValue('Protected solution code')).toBe('int sum(int a, int b) { return a + b + 1; }');
  });

  it('adds explicit solution roles and confirms destructive deletion without touching starter or cases', () => {
    const view = open();
    command('Add incorrect solution');
    expect(fieldValue('Solution role')).toBe('incorrect');
    const before = view.current();
    command('Delete solution');
    command('Cancel');
    expect(view.current()).toEqual(before);
    command('Delete solution');
    command('Confirm change');
    expect(view.current().validationSolutions).toHaveLength(2);
    expect(view.current().tests).toEqual(exerciseValidationFixture().tests);
    expect(view.current().files).toEqual(exerciseValidationFixture().files);
  });

  it('shows per-solution/case evidence, not academic scores, and marks changes stale', async () => {
    open();
    runner.mockResolvedValueOnce(result(true)).mockResolvedValueOnce(result(false));
    command('Check all solutions');
    expect(await screen.findByText('Reference passed')).toBeTruthy();
    expect(await screen.findByText('Incorrect solution detected')).toBeTruthy();
    const results = screen.getByRole('region', { name: 'Exercise check results' });
    expect(within(results).getAllByText('Expected 5; received -1.')).toHaveLength(2);
    expect(results.textContent).not.toMatch(/Estimate:|99|official grade/);
    fireEvent.click(screen.getByRole('tab', { name: 'Problem' }));
    edit('Statement', 'An edited exercise.');
    fireEvent.click(screen.getByRole('tab', { name: 'Validate exercise' }));
    expect(await screen.findByText('Outdated checks. Exercise changed.')).toBeTruthy();
  });

  it('renders compilation/infrastructure errors without reporting a caught incorrect solution', async () => {
    open();
    command('Subtracts instead');
    runner.mockRejectedValueOnce(new Error('Solution compilation failed.'));
    command('Run selected solution');
    expect(await screen.findByText('Solution compilation failed.')).toBeTruthy();
    expect(screen.queryByText('Incorrect solution detected')).toBeNull();
  });

  it('opens the real learner composition with only public current data and isolated scratch', async () => {
    const initial = exerciseValidationFixture();
    initial.problem.statement = 'An edited learner problem.';
    initial.problem.examples[0]!.explanation = 'Use addition, not subtraction.';
    initial.problem.constraints = ['The values are single digits.'];
    const view = open(initial);
    const saved = vi.spyOn(Storage.prototype, 'setItem');
    command('Learner preview');
    await screen.findByRole('heading', { name: 'Learner preview' });
    const preview = screen.getByRole('region', { name: 'Learner preview workspace' });
    expect(within(preview).getByText('An edited learner problem.')).toBeTruthy();
    expect(within(preview).getByText('Use addition, not subtraction.')).toBeTruthy();
    expect(within(preview).getByText('The values are single digits.')).toBeTruthy();
    expect(preview.innerHTML).not.toMatch(/PRIVATE-|REFERENCE-CANARY|INCORRECT-CANARY/);
    expect(within(preview).queryByRole('button', { name: /submit|release|approve|grade/i })).toBeNull();
    edit('Source code', 'int sum(int a, int b) { return a + b; }');
    command('Back to authoring');
    await screen.findByLabelText('Protected solution code');
    expect(view.current()).toEqual(initial);
    command('Learner preview');
    await screen.findByRole('heading', { name: 'Learner preview' });
    expect(fieldValue('Source code')).toBe(initial.files[0]!.content);
    expect(saved).not.toHaveBeenCalled();
    expect(runner).not.toHaveBeenCalled();
    saved.mockRestore();
  });

  it('cannot open preview until the active author runtime acknowledges cancellation', async () => {
    open();
    let finish!: (value: CodingPreviewResult) => void;
    runner.mockReturnValueOnce(
      new Promise((done) => {
        finish = done;
      }),
    );
    command('Check all solutions');
    await waitFor(() => expect(runner).toHaveBeenCalledTimes(1));
    command('Learner preview');
    expect(runner.mock.calls[0]?.[0].signal.aborted).toBe(true);
    expect(screen.queryByRole('heading', { name: 'Learner preview' })).toBeNull();
    finish(result(true));
    await screen.findByRole('heading', { name: 'Learner preview' });
    expect(screen.queryByText('Reference passed')).toBeNull();
  });

  it('cancels a pending preview request when the instructor leaves its task', async () => {
    open();
    let finish!: (value: CodingPreviewResult) => void;
    runner.mockReturnValueOnce(
      new Promise((done) => {
        finish = done;
      }),
    );
    command('Check all solutions');
    await waitFor(() => expect(runner).toHaveBeenCalledTimes(1));
    command('Learner preview');
    fireEvent.click(screen.getByRole('tab', { name: 'Problem' }));
    finish(result(true));
    await waitFor(() => expect(screen.getByLabelText('Statement')).toBeTruthy());
    fireEvent.click(screen.getByRole('tab', { name: 'Validate exercise' }));
    await screen.findByLabelText('Protected solution code');
    expect(screen.queryByRole('heading', { name: 'Learner preview' })).toBeNull();
  });

  it('waits for the learner scratch runtime to drain before returning to authoring', async () => {
    open();
    command('Learner preview');
    await screen.findByRole('heading', { name: 'Learner preview' });
    const run = screen.getByRole('button', { name: 'Run public tests' });
    await waitFor(() => expect(run.hasAttribute('disabled')).toBe(false));
    let finish!: (value: CodingPreviewResult) => void;
    runner.mockReturnValueOnce(
      new Promise((done) => {
        finish = done;
      }),
    );
    fireEvent.click(run);
    await waitFor(() => expect(runner).toHaveBeenCalledTimes(1));
    command('Back to authoring');
    expect(runner.mock.calls[0]?.[0].signal.aborted).toBe(true);
    expect(screen.queryByLabelText('Protected solution code')).toBeNull();
    finish({ ...result(true), scope: 'public' });
    await screen.findByLabelText('Protected solution code');
    expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Learner preview' }));
  });

  it('keeps an exercise without prepared solutions editable and does not manufacture answers', () => {
    const initial = exerciseValidationFixture();
    initial.validationSolutions = [];
    open(initial);
    expect(screen.getByText('No protected solutions prepared.')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Check all solutions' }).hasAttribute('disabled')).toBe(true);
    expect(screen.getByRole('button', { name: 'Learner preview' }).hasAttribute('disabled')).toBe(false);
    expect(runner).not.toHaveBeenCalled();
  });
});
