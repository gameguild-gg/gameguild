import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { TestingEventDateRange } from './testing-event-date-range';

describe('TestingEventDateRange', () => {
  it('shows two aligned date and time blocks with the timezone only once', () => {
    const { container } = render(
      <TestingEventDateRange
        startsAt="2026-09-22T02:43:00.000Z"
        endsAt="2026-09-22T05:43:00.000Z"
        timeZone="UTC"
      />,
    );

    expect(screen.getAllByText('Sep 22, 2026')).toHaveLength(2);
    expect(screen.getByText('2:43 AM')).toBeInTheDocument();
    expect(screen.getByText('to')).toBeInTheDocument();
    expect(screen.getByText('5:43 AM')).toBeInTheDocument();
    expect(screen.getAllByText('UTC')).toHaveLength(1);
    expect(container.textContent).toContain('UTC');
  });

  it('converts the range to the requested viewer timezone and locale', () => {
    render(
      <TestingEventDateRange
        startsAt="2026-09-22T02:43:00.000Z"
        endsAt="2026-09-22T05:43:00.000Z"
        timeZone="America/Sao_Paulo"
        locale="pt-BR"
        hour12={false}
      />,
    );

    expect(screen.getByText('21 de set. de 2026')).toBeInTheDocument();
    expect(screen.getByText('22 de set. de 2026')).toBeInTheDocument();
    expect(screen.getByText('23:43')).toBeInTheDocument();
    expect(screen.getByText('2:43')).toBeInTheDocument();
    expect(screen.getByText('to')).toBeInTheDocument();
  });

  it('keeps an explicit pending state when a start time is unavailable or invalid', () => {
    render(<TestingEventDateRange startsAt="not-a-date" />);

    expect(screen.getByText('Schedule pending')).toBeInTheDocument();
  });
});
