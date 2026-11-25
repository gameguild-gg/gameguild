import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { useState } from 'react';
import { afterEach, describe, expect, it } from 'vitest';
import { CodingSolvingLayout, type CodingSolvingView } from './solving-layout';

afterEach(cleanup);

function Fixture() {
  const [view, setView] = useState<CodingSolvingView>('problem');
  return (
    <CodingSolvingLayout
      view={view}
      onViewChange={setView}
      problem={<p>Problem text</p>}
      editor={<textarea aria-label="Source buffer" defaultValue="starter" />}
      cases={<textarea aria-label="Case buffer" defaultValue="input" />}
      results={<p>Result evidence</p>}
    />
  );
}

describe('shared focused solving layout', () => {
  it('exposes four views and keeps one mounted editor/case buffer across navigation', () => {
    render(<Fixture />);
    fireEvent.change(screen.getByLabelText('Source buffer'), { target: { value: 'my code' } });
    fireEvent.change(screen.getByLabelText('Case buffer'), { target: { value: 'my input' } });
    for (const name of ['Code', 'Cases', 'Results', 'Problem']) fireEvent.click(screen.getByRole('button', { name }));
    expect(screen.getAllByLabelText('Source buffer')).toHaveLength(1);
    expect(screen.getByDisplayValue('my code')).toBeTruthy();
    expect(screen.getByDisplayValue('my input')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Problem' }).getAttribute('aria-pressed')).toBe('true');
  });

  it('uses keyboard view navigation and a reversible focus mode without dropping buffers', () => {
    render(<Fixture />);
    const problem = screen.getByRole('button', { name: 'Problem' });
    problem.focus();
    fireEvent.keyDown(problem, { key: 'ArrowRight' });
    const code = screen.getByRole('button', { name: 'Code' });
    expect(document.activeElement).toBe(code);
    expect(code.getAttribute('aria-pressed')).toBe('true');
    const focus = screen.getByRole('button', { name: 'Focus selected view' });
    fireEvent.click(focus);
    expect(focus.getAttribute('aria-pressed')).toBe('true');
    expect(screen.getByRole('region', { name: 'Coding solving workspace' }).getAttribute('data-focused')).toBe('true');
    fireEvent.click(focus);
    expect(focus.getAttribute('aria-pressed')).toBe('false');
    fireEvent.keyDown(code, { key: 'End' });
    expect(screen.getByRole('button', { name: 'Results' })).toBe(document.activeElement);
    expect(screen.getByLabelText('Source buffer')).toBeTruthy();
  });

  it('adjusts the problem share without reinitializing the editor', () => {
    render(<Fixture />);
    const source = screen.getByLabelText('Source buffer');
    fireEvent.change(source, { target: { value: 'retained' } });
    fireEvent.change(screen.getByRole('slider', { name: 'Problem width' }), { target: { value: '55' } });
    expect(screen.getByRole('region', { name: 'Coding solving workspace' }).style.getPropertyValue('--coding-problem-share')).toBe('55%');
    expect(screen.getByDisplayValue('retained')).toBe(source);
  });
});
