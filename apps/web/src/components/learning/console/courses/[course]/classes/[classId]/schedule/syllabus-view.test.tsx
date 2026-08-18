import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { scheduleFixture } from './schedule-test-fixtures';
import { SyllabusView } from './syllabus-view';

describe('SyllabusView', () => {
  it('groups items into instructional weeks', () => {
    render(<SyllabusView schedule={scheduleFixture} onShift={vi.fn()} />);

    expect(screen.getByRole('heading', { name: 'Week 1 - Foundations' })).toBeVisible();
    expect(screen.getByRole('heading', { name: 'Week 2 - Decision systems' })).toBeVisible();
    expect(screen.getByText('Available Aug 12, 08:00')).toBeVisible();
    expect(screen.getByText('Foundations quiz')).toBeVisible();
  });

  it('hides mutation controls when the class is read only', () => {
    render(<SyllabusView schedule={scheduleFixture} readOnly onShift={vi.fn()} />);

    expect(screen.queryByRole('button', { name: /Shift Foundations/i })).not.toBeInTheDocument();
  });
});
