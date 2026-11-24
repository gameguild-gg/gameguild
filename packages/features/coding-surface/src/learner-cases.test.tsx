import { codingContractFixture } from '@game-guild/coding/testing';
import type { CodingExerciseType, CodingLearnerDelivery } from '@game-guild/coding';
import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { useState } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CodingLearnerCases } from './learner-cases';
import { codingLearnerPractice as practice } from './learner-practice';
import type { CodingLearnerPracticeState } from './learner-practice-types';

afterEach(cleanup);

function Harness({
  family = 'standard-io',
  disabled = false,
  changed = () => undefined,
  source,
  idFactory,
}: {
  family?: CodingExerciseType;
  disabled?: boolean;
  changed?: (state: CodingLearnerPracticeState) => void;
  source?: CodingLearnerDelivery;
  idFactory?: () => string;
}) {
  const [state, setState] = useState(() =>
    practice.create(
      'attempt-a',
      source ?? structuredClone(family === 'function' ? codingContractFixture.functionLearnerDelivery : codingContractFixture.learnerDelivery),
    ),
  );
  return (
    <CodingLearnerCases
      state={state}
      disabled={disabled}
      createId={idFactory ?? (() => `mine-${state.personalCases.length + 1}`)}
      onChange={(next) => {
        changed(next);
        setState(next);
      }}
    />
  );
}

describe('UX05-A1 offline learner case panel', () => {
  it.each(['standard-io', 'function'] as const)('shows immutable public %s cases and copies one into personal data', (family) => {
    const changed = vi.fn();
    render(<Harness family={family} changed={changed} />);
    expect(screen.getByRole('tab', { name: 'Public tests' }).getAttribute('aria-selected')).toBe('true');
    const fields = screen.getByRole('region', { name: 'Case details' }).querySelectorAll('textarea, input[type="text"]');
    expect(fields.length).toBeGreaterThan(0);
    for (const field of fields) expect(field).toHaveProperty('readOnly', true);
    fireEvent.click(screen.getByRole('button', { name: 'Copy to my cases' }));
    expect(screen.getByRole('tab', { name: 'My cases' }).getAttribute('aria-selected')).toBe('true');
    expect(screen.getByLabelText('Case name')).not.toHaveProperty('readOnly', true);
    expect(changed.mock.calls.at(-1)?.[0].personalCases).toHaveLength(1);
    expect(screen.queryByLabelText('Case visibility')).toBeNull();
    expect(screen.queryByLabelText('Weight units')).toBeNull();
  });

  it('creates a case with no oracle, then retains an intentional empty stdout expectation', () => {
    const changed = vi.fn();
    render(<Harness changed={changed} />);
    fireEvent.click(screen.getByRole('button', { name: 'Add personal case' }));
    expect(screen.queryByLabelText('Expected stdout', { selector: 'textarea' })).toBeNull();
    fireEvent.change(screen.getByLabelText('Standard input'), { target: { value: 'Ada\n' } });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Compare stdout' }));
    expect(fieldValue('Expected stdout')).toBe('');
    expect(changed.mock.calls.at(-1)?.[0].personalCases[0]).toMatchObject({ stdin: 'Ada\n', expected: { stdout: '', stderr: null, exitCode: null } });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Compare stdout' }));
    expect(changed.mock.calls.at(-1)?.[0].personalCases[0]).toMatchObject({ expected: { stdout: null } });
  });

  it('keeps invalid numeric text editable and explicit optional function expectations', () => {
    render(<Harness family="function" />);
    fireEvent.click(screen.getByRole('button', { name: 'Add personal case' }));
    fireEvent.change(screen.getByLabelText('left (integer)'), { target: { value: '-' } });
    expect(fieldValue('left (integer)')).toBe('-');
    expect(screen.queryByLabelText('Expected result (integer)')).toBeNull();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Compare return value' }));
    fireEvent.change(screen.getByLabelText('Expected result (integer)'), { target: { value: '3e' } });
    fireEvent.click(screen.getByRole('tab', { name: 'Public tests' }));
    fireEvent.click(screen.getByRole('tab', { name: 'My cases' }));
    expect(fieldValue('left (integer)')).toBe('-');
    expect(fieldValue('Expected result (integer)')).toBe('3e');
  });

  it('preserves public data while renaming and duplicating a personal case', () => {
    const source = structuredClone(codingContractFixture.learnerDelivery);
    const original = structuredClone(source);
    render(<Harness source={source} />);
    fireEvent.click(screen.getByRole('button', { name: 'Add personal case' }));
    fireEvent.change(screen.getByLabelText('Case name'), { target: { value: 'My edge case' } });
    fireEvent.change(screen.getByLabelText('Standard input'), { target: { value: '\n' } });
    fireEvent.click(screen.getByRole('button', { name: 'Duplicate personal case' }));
    expect(fieldValue('Case name')).toBe('My edge case copy');
    expect(fieldValue('Standard input')).toBe('\n');
    expect(source).toEqual(original);
  });

  it('requires confirmation and restores focus after cancelling case deletion', () => {
    render(<Harness />);
    fireEvent.click(screen.getByRole('button', { name: 'Add personal case' }));
    const remove = screen.getByRole('button', { name: 'Delete personal case' });
    fireEvent.click(remove);
    const dialog = screen.getByRole('alertdialog', { name: 'Delete personal case?' });
    expect(document.activeElement).toBe(within(dialog).getByRole('button', { name: 'Cancel' }));
    fireEvent.keyDown(dialog, { key: 'Escape' });
    expect(screen.queryByRole('alertdialog')).toBeNull();
    expect(document.activeElement).toBe(remove);
    expect(fieldValue('Case name')).toBe('Case 1');
    fireEvent.click(remove);
    fireEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Delete case' }));
    expect(screen.queryByLabelText('Case name')).toBeNull();
    expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Add personal case' }));
  });

  it('uses keyboard tabs without losing personal fields', () => {
    render(<Harness />);
    const publicTab = screen.getByRole('tab', { name: 'Public tests' });
    publicTab.focus();
    fireEvent.keyDown(publicTab, { key: 'ArrowRight' });
    expect(document.activeElement).toBe(screen.getByRole('tab', { name: 'My cases' }));
    expect(screen.getByRole('tab', { name: 'My cases' }).getAttribute('aria-selected')).toBe('true');
    fireEvent.keyDown(screen.getByRole('tab', { name: 'My cases' }), { key: 'Home' });
    expect(document.activeElement).toBe(publicTab);
  });

  it('does not offer editing, runtime, academic or persistence actions when disabled', () => {
    const changed = vi.fn();
    render(<Harness disabled changed={changed} />);
    fireEvent.click(screen.getByRole('button', { name: 'Add personal case' }));
    fireEvent.click(screen.getByRole('button', { name: 'Copy to my cases' }));
    expect(changed).not.toHaveBeenCalled();
    for (const name of ['Run', 'Submit', 'Release', 'Save']) expect(screen.queryByRole('button', { name: new RegExp(name, 'i') })).toBeNull();
  });

  it('handles an empty public function list without exposing an invented signature or expected answer', () => {
    const source = structuredClone(codingContractFixture.functionLearnerDelivery);
    source.publicTests = [];
    render(<Harness source={source} />);
    expect(screen.getByRole('button', { name: 'Add personal case' })).toHaveProperty('disabled', true);
    expect(screen.queryByLabelText('Expected result (integer)')).toBeNull();
  });

  it('surfaces identity failures without losing a valid case or issuing a state update', () => {
    const changed = vi.fn();
    const { rerender } = render(<Harness changed={changed} idFactory={() => 'mine-1'} />);
    fireEvent.click(screen.getByRole('button', { name: 'Add personal case' }));
    fireEvent.change(screen.getByLabelText('Standard input'), { target: { value: 'Keep this input' } });
    changed.mockClear();
    fireEvent.click(screen.getByRole('button', { name: 'Add personal case' }));
    expect(screen.getByRole('alert').textContent).toContain('identity is already in use');
    expect(fieldValue('Standard input')).toBe('Keep this input');
    expect(changed).not.toHaveBeenCalled();
    rerender(
      <Harness
        changed={changed}
        idFactory={() => {
          throw new Error('Identity unavailable');
        }}
      />,
    );
    fireEvent.click(screen.getByRole('button', { name: 'Add personal case' }));
    expect(screen.getByRole('alert').textContent).toBe('Identity unavailable');
    expect(changed).not.toHaveBeenCalled();
  });

  it('keeps optional stderr and exit-code buffers independent from stdout', () => {
    const changed = vi.fn();
    render(<Harness changed={changed} />);
    fireEvent.click(screen.getByRole('button', { name: 'Add personal case' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Compare stderr' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Compare exit code' }));
    fireEvent.change(screen.getByLabelText('Expected stderr'), { target: { value: 'diagnostic\n' } });
    fireEvent.change(screen.getByLabelText('Expected exit code'), { target: { value: '-' } });
    expect(changed.mock.calls.at(-1)?.[0].personalCases[0]).toMatchObject({ expected: { stdout: null, stderr: 'diagnostic\n', exitCode: '-' } });
  });

  it('locks existing personal inputs and expectation toggles without dropping their values', () => {
    const changed = vi.fn();
    const { rerender } = render(<Harness changed={changed} />);
    fireEvent.click(screen.getByRole('button', { name: 'Add personal case' }));
    fireEvent.change(screen.getByLabelText('Standard input'), { target: { value: 'Keep while busy' } });
    changed.mockClear();
    rerender(<Harness changed={changed} disabled />);
    fireEvent.change(screen.getByLabelText('Standard input'), { target: { value: 'Unexpected edit' } });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Compare stdout' }));
    expect(fieldValue('Standard input')).toBe('Keep while busy');
    expect(screen.getByLabelText('Standard input')).toHaveProperty('readOnly', true);
    expect(screen.getByRole('checkbox', { name: 'Compare stdout' })).toHaveProperty('disabled', true);
    expect(changed).not.toHaveBeenCalled();
  });

  it('traps confirmation keyboard focus in both directions without deleting early', () => {
    render(<Harness />);
    fireEvent.click(screen.getByRole('button', { name: 'Add personal case' }));
    fireEvent.click(screen.getByRole('button', { name: 'Delete personal case' }));
    const dialog = screen.getByRole('alertdialog');
    const cancel = within(dialog).getByRole('button', { name: 'Cancel' });
    const confirm = within(dialog).getByRole('button', { name: 'Delete case' });
    fireEvent.keyDown(cancel, { key: 'Tab', shiftKey: true });
    expect(document.activeElement).toBe(confirm);
    fireEvent.keyDown(confirm, { key: 'Tab' });
    expect(document.activeElement).toBe(cancel);
    fireEvent.keyDown(dialog, { key: 'Escape' });
    expect(fieldValue('Case name')).toBe('Case 1');
  });
});

function fieldValue(label: string): string {
  const field = screen.getByLabelText(label);
  if (!(field instanceof HTMLInputElement || field instanceof HTMLTextAreaElement)) throw new Error('Expected a text buffer.');
  return field.value;
}
