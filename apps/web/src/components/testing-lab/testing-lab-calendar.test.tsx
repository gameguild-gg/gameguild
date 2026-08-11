import { fireEvent, render, screen } from '@testing-library/react';
import type { TestingLabTestingEventProjection } from '@game-guild/client';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...rest }: { children: ReactNode; href: string }) => <a href={href} {...rest}>{children}</a>,
}));

vi.mock('./testing-event-management', () => ({
  CreateTestingEventDialog: () => <button type="button">New event</button>,
}));

import { TestingLabCalendar } from './testing-lab-calendar';

const events = [
  {
    id: 'event-1',
    name: 'Campus playtest',
    status: 'Scheduled',
    mode: 'InPerson',
    startsAt: '2026-08-10T18:00:00.000Z',
    endsAt: '2026-08-10T20:00:00.000Z',
  },
] as TestingLabTestingEventProjection[];

describe('TestingLabCalendar', () => {
  it('replaces the operations cards with an icon menu and a Month calendar by default', () => {
    render(<TestingLabCalendar events={events} initialDate={new Date(2026, 7, 10)} />);

    expect(screen.getByRole('navigation', { name: 'Testing Lab operations' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /events workspace/i })).toHaveAttribute('href', '/dashboard/testing-lab/events');
    expect(screen.getByRole('link', { name: /projects workspace/i })).toHaveAttribute('href', '/dashboard/testing-lab/projects');
    expect(screen.getByRole('combobox', { name: 'Calendar view' })).toHaveValue('month');
    expect(screen.getByText('Campus playtest')).toBeInTheDocument();
  });

  it('offers the Google Calendar view set and can switch to the schedule', () => {
    render(<TestingLabCalendar events={events} initialDate={new Date(2026, 7, 10)} />);

    const view = screen.getByRole('combobox', { name: 'Calendar view' });
    expect([...view.querySelectorAll('option')].map((option) => option.value)).toEqual([
      'day', 'week', 'month', 'year', 'schedule', '3days',
    ]);

    fireEvent.change(view, { target: { value: 'schedule' } });

    expect(screen.getByRole('heading', { name: 'Schedule' })).toBeInTheDocument();
    expect(screen.getByText('Scheduled')).toBeInTheDocument();
  });
});
