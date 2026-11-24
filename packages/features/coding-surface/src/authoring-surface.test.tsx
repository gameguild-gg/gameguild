import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { useState } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { compileCodingAuthoringDraft, parseCodingAuthoringDraft, type CodingAuthoringDraft } from '@game-guild/coding';
import drafts from '../../coding/fixtures/coding-authoring-drafts.json';
import { CodingAuthoringSurface } from './authoring-surface';
import type { CodingAuthoringIdentityKind } from './authoring-types';

const runtime = vi.hoisted(() => ({ boot: vi.fn() }));
vi.mock('@gameguild/emception-browser', () => ({ createEmception: runtime.boot }));
vi.mock('@monaco-editor/react', () => ({
  default: ({ value, onChange, options }: { value: string; onChange: (value: string) => void; options?: { readOnly?: boolean } }) => (
    <textarea aria-label="Source code" value={value} onChange={(event) => onChange(event.target.value)} readOnly={options?.readOnly} />
  ),
}));

afterEach(() => {
  cleanup();
  runtime.boot.mockClear();
});

function fixture(family: 'function' | 'standard-io' = 'function') {
  return parseCodingAuthoringDraft(drafts[family]);
}

function open(initial = fixture(), options: { disabled?: boolean; idFactory?: (kind: CodingAuthoringIdentityKind) => string } = {}) {
  let current = structuredClone(initial);
  let counter = 0;
  const changed = vi.fn<(draft: CodingAuthoringDraft) => void>();
  function Harness() {
    const [draft, setDraft] = useState(initial);
    return (
      <CodingAuthoringSurface
        contextKey="author-course-content"
        draft={draft}
        disabled={options.disabled}
        createId={options.idFactory ?? ((kind) => `${kind}-new-${++counter}`)}
        onChange={(next) => {
          current = next;
          changed(next);
          setDraft(next);
        }}
      />
    );
  }
  const result = render(<Harness />);
  return { ...result, current: () => current, changed };
}

function task(name: string) {
  fireEvent.click(screen.getByRole('tab', { name }));
}
function edit(label: string, value: string) {
  fireEvent.change(screen.getByLabelText(label, { exact: true }), { target: { value } });
}
function command(name: string) {
  fireEvent.click(screen.getByRole('button', { name }));
}
function confirm() {
  command('Confirm change');
}
function fieldValue(label: string): string {
  const field = screen.getByLabelText(label, { exact: true });
  if (!(field instanceof HTMLInputElement || field instanceof HTMLTextAreaElement || field instanceof HTMLSelectElement))
    throw new Error('Expected a value field.');
  return field.value;
}
function firstFunction(draft: CodingAuthoringDraft) {
  const test = draft.tests[0];
  if (test?.kind !== 'function') throw new Error('Function fixture missing.');
  return test;
}
function firstStdio(draft: CodingAuthoringDraft) {
  const test = draft.tests[0];
  if (test?.kind !== 'stdio') throw new Error('Standard I/O fixture missing.');
  return test;
}

describe('UX04-A controlled instructor authoring', () => {
  it.each(['function', 'standard-io'] as const)('authors a blank %s exercise without a compiler or academic command', (family) => {
    const initial = fixture(family);
    initial.problem = { statement: '', examples: [], constraints: [] };
    initial.files = [];
    initial.tests = [];
    initial.validationSolutions = [];
    initial.entryPoint.path = '';
    initial.functionSignature = null;
    const view = open(initial);
    edit('Statement', 'Add two integers.');
    task('Environment');
    edit('Time limit (ms)', '5000');
    task('Starter code');
    if (family === 'function') {
      edit('Function name', 'sum');
      command('Add parameter');
      edit('Parameter 1 name', 'left');
      command('Add parameter');
      edit('Parameter 2 name', 'right');
      command('Apply signature');
      confirm();
    }
    command('Generate starter');
    expect(screen.getByLabelText('Source code')).toBeTruthy();
    expect(view.current().files[0]?.content).toContain(family === 'function' ? 'int sum(int left, int right)' : 'int main()');
    task('Tests');
    command('Add test');
    edit('Test name', 'Two plus three');
    if (family === 'function') {
      edit('left (integer)', '2');
      edit('right (integer)', '3');
      edit('Expected result (integer)', '5');
    } else {
      edit('Standard input', '2 3\n');
      edit('Expected stdout', '5\n');
    }
    expect(() => compileCodingAuthoringDraft(view.current())).not.toThrow();
    task('Problem');
    expect(fieldValue('Statement')).toBe('Add two integers.');
    expect(runtime.boot).not.toHaveBeenCalled();
    expect(screen.queryByText('Tests and output')).toBeNull();
    expect(screen.queryByRole('button', { name: /submit|grade|release/i })).toBeNull();
  });

  it('keeps examples, constraints and text independent from private starter/reference data', () => {
    const view = open();
    command('Add example');
    const example = screen.getAllByRole('group', { name: /Example/ }).at(-1);
    if (!example) throw new Error('Example editor missing.');
    fireEvent.change(within(example).getByLabelText('Example input'), { target: { value: '<script>sample</script>' } });
    command('Add constraint');
    edit(`Constraint ${view.current().problem.constraints.length}`, 'Inputs are nonnegative.');
    task('Environment');
    task('Problem');
    expect(screen.getByDisplayValue('<script>sample</script>')).toBeTruthy();
    expect(view.current().validationSolutions).toEqual(fixture().validationSolutions);
    expect(document.querySelector('script')).toBeNull();
  });

  it('preserves pending signature fields and typed edits across task navigation', () => {
    const view = open();
    task('Starter code');
    edit('Function name', 'pendingName');
    task('Tests');
    task('Starter code');
    expect(fieldValue('Function name')).toBe('pendingName');
    expect(view.current().functionSignature?.name).toBe('sum');
    command('Apply signature');
    expect(screen.getByRole('dialog').textContent).toMatch(/cases|files/i);
    command('Cancel');
    expect(view.current()).toEqual(fixture());
    expect(fieldValue('Function name')).toBe('pendingName');
  });

  it('reconciles signature types through the domain transaction instead of losing cases', () => {
    const view = open();
    task('Starter code');
    edit('Parameter 1 type', 'string');
    command('Apply signature');
    confirm();
    const test = view.current().tests[0];
    if (test?.kind !== 'function') throw new Error('Function fixture missing.');
    expect(test.cases[0]?.inputs[0]).toEqual({ type: 'string', text: '' });
    expect(test.cases[0]?.inputs[1]).toEqual(firstFunction(fixture()).cases[0]?.inputs[1]);
    task('Tests');
    expect(fieldValue('a (string)')).toBe('');
  });

  it('distinguishes missing expected stdout from intentional empty output without trimming whitespace', () => {
    const view = open(fixture('standard-io'));
    task('Tests');
    edit('Output expectation', 'none');
    expect(firstStdio(view.current()).expected.stdout).toBeNull();
    expect(screen.getByText(/requires expected stdout/i)).toBeTruthy();
    edit('Output expectation', 'exact');
    edit('Expected stdout', '');
    expect(firstStdio(view.current()).expected.stdout).toBe('');
    edit('Standard input', ' a\n\n');
    edit('Expected stdout', ' a\n');
    expect(firstStdio(view.current()).stdin).toBe(' a\n\n');
    expect(firstStdio(view.current()).expected.stdout).toBe(' a\n');
  });

  it('retains incomplete integer/boolean buffers and shows domain field errors', () => {
    const view = open();
    task('Tests');
    edit('a (integer)', '-');
    expect(firstFunction(view.current()).cases[0]?.inputs[0]?.text).toBe('-');
    expect(screen.getByLabelText('a (integer)').getAttribute('aria-invalid')).toBe('true');
    expect(screen.getByText(/complete safe integer/i)).toBeTruthy();
  });

  it('duplicates and deletes tests with fresh stable identities and independent values', () => {
    const view = open();
    task('Tests');
    const before = structuredClone(view.current().tests[0]);
    command('Duplicate test');
    const duplicate = view.current().tests.at(-1);
    expect(duplicate?.id).not.toBe(before?.id);
    if (before?.kind !== 'function' || duplicate?.kind !== 'function') throw new Error('Function fixture missing.');
    expect(duplicate.cases[0]?.id).not.toBe(before.cases[0]?.id);
    edit('Expected result (integer)', '99');
    expect(view.current().tests[0]).toEqual(before);
    command('Delete test');
    confirm();
    expect(view.current().tests).toEqual(fixture().tests);
  });

  it('edits all cases of an existing functional group rather than silently dropping all but the first', () => {
    const initial = fixture();
    const test = initial.tests[0];
    if (test?.kind !== 'function') throw new Error('Function fixture missing.');
    test.cases.push({ ...structuredClone(test.cases[0]!), id: 'second-value-case' });
    const view = open(initial);
    task('Tests');
    edit('Function case', 'second-value-case');
    edit('Expected result (integer)', '42');
    const edited = view.current().tests[0];
    expect(edited?.kind === 'function' && edited.cases[0]?.expected?.text).not.toBe('42');
    expect(edited?.kind === 'function' && edited.cases[1]?.expected?.text).toBe('42');
    task('Problem');
    task('Tests');
    expect(fieldValue('Function case')).toBe('second-value-case');
  });

  it('switches families only after impact review and leaves cancelled changes byte-equivalent', () => {
    const view = open();
    const original = JSON.stringify(view.current());
    edit('Exercise type', 'standard-io');
    expect(screen.getByRole('dialog').textContent).toMatch(/test/i);
    command('Cancel');
    expect(JSON.stringify(view.current())).toBe(original);
    edit('Exercise type', 'standard-io');
    confirm();
    expect(view.current().exerciseType).toBe('standard-io');
    expect(view.current().tests).toEqual([]);
    expect(view.current().files).toEqual(fixture().files);
    expect(view.current().validationSolutions).toEqual(fixture().validationSolutions);
    task('Tests');
    expect(screen.queryByLabelText('Expected result (integer)')).toBeNull();
    command('Add test');
    expect(screen.getByLabelText('Standard input')).toBeTruthy();
  });

  it('changes language without resetting authored files or enabling unverified graphics', () => {
    const view = open();
    task('Environment');
    edit('Language', 'c');
    confirm();
    expect(view.current().environment).toEqual({ language: 'c', runtime: 'wasi' });
    expect(view.current().toolchain.key).toBe('c-emception');
    expect(view.current().files).toEqual(fixture().files);
    const runtimeSelect = screen.getByLabelText('Execution environment');
    expect(within(runtimeSelect).queryByRole('option', { name: /SDL/i })).toBeNull();
    edit('Language', 'cpp');
    confirm();
    expect(within(screen.getByLabelText('Execution environment')).getByRole('option', { name: /SDL/i }).hasAttribute('disabled')).toBe(true);
    expect(runtime.boot).not.toHaveBeenCalled();
  });

  it('edits offline files, names and learner policies without affecting protected solution buffers', () => {
    const view = open();
    task('Starter code');
    edit('Source code', 'int sum(int a, int b) { return a + b; }');
    edit('File path', '/home/user/solution.cpp');
    expect(view.current().entryPoint.path).toBe('/home/user/solution.cpp');
    command('Add file');
    edit('File path', '/home/user/helper.hpp');
    edit('Source code', '// helper');
    task('Tests');
    task('Starter code');
    expect(fieldValue('Source code')).toBe('// helper');
    expect(view.current().validationSolutions[0]?.files[0]?.content).toBe(fixture().validationSolutions[0]?.files[0]?.content);
    expect(view.current().validationSolutions[0]?.files[0]?.path).toBe('/home/user/solution.cpp');
    expect(runtime.boot).not.toHaveBeenCalled();
  });

  it('does not overwrite authored source when starter replacement is cancelled', () => {
    const view = open();
    task('Starter code');
    const original = JSON.stringify(view.current());
    command('Generate starter');
    command('Cancel');
    expect(JSON.stringify(view.current())).toBe(original);
  });

  it('keeps a renamed file and its protected replacement bound by identity without changing protected code', () => {
    const view = open();
    const protectedCode = fixture().validationSolutions[0]?.files[0]?.content;
    task('Starter code');
    edit('File path', '/home/user/renamed.cpp');
    expect(view.current().validationSolutions[0]?.files[0]?.path).toBe('/home/user/renamed.cpp');
    expect(view.current().validationSolutions[0]?.files[0]?.content).toBe(protectedCode);
    expect(() => compileCodingAuthoringDraft(view.current())).not.toThrow();
  });

  it('resets task selection and pending review when the host changes authoring context', () => {
    const initial = fixture();
    const onChange = vi.fn();
    const props = { draft: initial, createId: (kind: CodingAuthoringIdentityKind) => `${kind}-context`, onChange };
    const view = render(<CodingAuthoringSurface {...props} contextKey="first" />);
    edit('Exercise type', 'standard-io');
    expect(screen.getByRole('dialog')).toBeTruthy();
    view.rerender(<CodingAuthoringSurface {...props} contextKey="second" />);
    expect(screen.queryByRole('dialog')).toBeNull();
    expect(screen.getByRole('tab', { name: 'Problem' }).getAttribute('aria-selected')).toBe('true');
    expect(onChange).not.toHaveBeenCalled();
  });

  it('edits booleans through a finite value selector and keeps integer/float/string values distinct', () => {
    const initial = fixture();
    initial.functionSignature = { name: 'identity', parameters: [{ name: 'flag', type: 'boolean' }], returnType: 'boolean' };
    initial.tests = [
      {
        id: 'boolean-test',
        name: 'Boolean case',
        kind: 'function',
        visibility: 'private',
        weightUnits: '100',
        cases: [{ id: 'boolean-value', inputs: [{ type: 'boolean', text: '' }], expected: { type: 'boolean', text: '' } }],
      },
    ];
    const view = open(initial);
    task('Tests');
    edit('flag (boolean)', 'true');
    edit('Expected result (boolean)', 'false');
    expect(firstFunction(view.current()).cases[0]?.inputs[0]).toEqual({ type: 'boolean', text: 'true' });
    expect(firstFunction(view.current()).cases[0]?.expected).toEqual({ type: 'boolean', text: 'false' });
  });

  it('retains a host update failure as a visible failure without mutating the supplied draft', () => {
    const initial = fixture();
    const original = JSON.stringify(initial);
    render(
      <CodingAuthoringSurface
        contextKey="failed-host"
        draft={initial}
        createId={(kind) => `${kind}-1`}
        onChange={() => {
          throw new Error('Host rejected edit.');
        }}
      />,
    );
    edit('Statement', 'An unacknowledged statement.');
    expect(screen.getByRole('alert').textContent).toContain('Host rejected edit.');
    expect(JSON.stringify(initial)).toBe(original);
  });

  it('keeps read-only authoring inert and creates no browser draft storage', () => {
    const saved = vi.spyOn(Storage.prototype, 'setItem');
    const view = open(fixture(), { disabled: true });
    expect(screen.getByLabelText('Statement').hasAttribute('disabled')).toBe(true);
    task('Tests');
    expect(screen.getByRole('button', { name: 'Add test' }).hasAttribute('disabled')).toBe(true);
    task('Starter code');
    expect(screen.getByLabelText('Source code').hasAttribute('readonly')).toBe(true);
    expect(view.changed).not.toHaveBeenCalled();
    expect(saved).not.toHaveBeenCalled();
    saved.mockRestore();
  });

  it('handles duplicate IDs as a visible failed edit without mutating the draft', () => {
    const initial = fixture();
    const view = open(initial, { idFactory: () => initial.tests[0]!.id });
    task('Tests');
    command('Add test');
    expect(screen.getByRole('alert').textContent).toMatch(/duplicate/i);
    expect(view.current()).toEqual(initial);
  });

  it('supports keyboard task navigation and lossless escape from change review', () => {
    const view = open();
    const first = screen.getByRole('tab', { name: 'Problem' });
    first.focus();
    fireEvent.keyDown(first, { key: 'End' });
    expect(screen.getByRole('tab', { name: 'Validate exercise' }).getAttribute('aria-selected')).toBe('true');
    fireEvent.keyDown(screen.getByRole('tab', { name: 'Validate exercise' }), { key: 'Home' });
    edit('Exercise type', 'standard-io');
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });
    expect(screen.queryByRole('dialog')).toBeNull();
    expect(view.current()).toEqual(fixture());
  });

  it('rejects a stale reviewed transformation instead of replacing a newer host draft', () => {
    let counter = 0;
    const change = vi.fn();
    const initial = fixture();
    const props = { contextKey: 'host', createId: (kind: CodingAuthoringIdentityKind) => `${kind}-${++counter}`, onChange: change };
    const view = render(<CodingAuthoringSurface {...props} draft={initial} />);
    edit('Exercise type', 'standard-io');
    const updated = { ...initial, problem: { ...initial.problem, statement: 'A newer host draft.' } };
    view.rerender(<CodingAuthoringSurface {...props} draft={updated} />);
    confirm();
    expect(change).not.toHaveBeenCalled();
    expect(screen.getByRole('alert').textContent).toMatch(/changed|stale/i);
  });

  it('returns focus to the signature summary when confirmation closes its fields', () => {
    open();
    task('Starter code');
    edit('Function name', 'renamed');
    const apply = screen.getByRole('button', { name: 'Apply signature' });
    apply.focus();
    command('Apply signature');
    confirm();
    expect(document.activeElement).toBe(document.querySelector('.cs-author-signature > summary'));
  });
});
