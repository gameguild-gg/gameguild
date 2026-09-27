import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getPublicTestingEventsDirectory: vi.fn(),
  getLocalizationPreference: vi.fn(),
}));

vi.mock('@/lib/testing-lab/events-queries', () => ({ getPublicTestingEventsDirectory: mocks.getPublicTestingEventsDirectory }));
vi.mock('@/lib/user-settings/queries', () => ({ getLocalizationPreference: mocks.getLocalizationPreference }));
vi.mock('@/components/testing-lab/landing/testing-sessions', () => ({
  TestingEventsBrowser: ({ events }: { events: Array<{ timeZoneId: string; dateLocale: string; hour12: boolean }> }) => (
    <div data-testid="events" data-time-zone={events[0]?.timeZoneId} data-locale={events[0]?.dateLocale} data-hour12={events[0]?.hour12} />
  ),
}));

import Page from './page';

describe('Testing Lab directory locale', () => {
  it('formats directory dates using the signed-in user preferences', async () => {
    mocks.getPublicTestingEventsDirectory.mockResolvedValue({ events: [{ id: 'event-1', status: 'Completed' }], accessIssues: [] });
    mocks.getLocalizationPreference.mockResolvedValue({
      language: 'pt-BR',
      timezone: 'America/Sao_Paulo',
      timeFormat: '24h',
    });

    render(await Page({ searchParams: Promise.resolve({ projectId: 'project-1' }) }));

    expect(screen.getByTestId('events')).toHaveAttribute('data-time-zone', 'America/Sao_Paulo');
    expect(screen.getByTestId('events')).toHaveAttribute('data-locale', 'pt-BR');
    expect(screen.getByTestId('events')).toHaveAttribute('data-hour12', 'false');
  });
});
