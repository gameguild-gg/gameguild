import '@testing-library/jest-dom/vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { NextIntlClientProvider } from 'next-intl';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import enMessages from '@/i18n/messages/en-US.json';
import {
  EmailDeliverability,
  type DeliverabilityDeadLetter,
  type DeliverabilityEvent,
  type DeliverabilitySuppression,
} from './email-deliverability';

const actionMocks = vi.hoisted(() => ({
  unsuppressEmailAction: vi.fn(),
  requeueNotificationAction: vi.fn(),
  getNotificationTimelineAction: vi.fn(),
}));

vi.mock('@/lib/notifications/email-deliverability-actions', () => actionMocks);

const events: DeliverabilityEvent[] = [
  {
    id: 'e-3',
    occurredAt: '2026-08-19T10:03:00Z',
    eventType: 'Bounce',
    recipientEmail: 'hard@bouncer.co',
    providerMessageId: 'ses-1',
    bounceType: 'Permanent',
    diagnosticCode: '5.1.1 User unknown',
    payloadPreview: null,
  },
  {
    id: 'e-2',
    occurredAt: '2026-08-19T10:02:00Z',
    eventType: 'Delivery',
    recipientEmail: 'ok@example.com',
    providerMessageId: 'ses-2',
    bounceType: null,
    diagnosticCode: null,
    payloadPreview: null,
  },
  {
    id: 'e-1',
    occurredAt: '2026-08-19T10:01:00Z',
    eventType: 'Send',
    recipientEmail: 'ok@example.com',
    providerMessageId: 'ses-2',
    bounceType: null,
    diagnosticCode: null,
    payloadPreview: null,
  },
];

const suppressions: DeliverabilitySuppression[] = [
  {
    id: 's-1',
    emailAddress: 'hard@bouncer.co',
    reason: 'HardBounce',
    bounceType: 'Permanent',
    suppressedAt: '2026-08-19T10:03:30Z',
    releasedAt: null,
    isActive: true,
  },
  {
    id: 's-2',
    emailAddress: 'released@example.com',
    reason: 'Complaint',
    bounceType: null,
    suppressedAt: '2026-08-01T08:00:00Z',
    releasedAt: '2026-08-10T08:00:00Z',
    isActive: false,
  },
];

const deadLetters: DeliverabilityDeadLetter[] = [
  {
    id: 'n-1',
    title: 'Monthly statement ready',
    type: 'MonthlyStatement',
    channel: 'Email',
    recipientEmail: 'hard@bouncer.co',
    recipientId: 'u-1',
    lastError: 'suppressed: hard bounce',
    attemptCount: 2,
    requeueCount: 0,
    createdAt: '2026-08-19T10:03:31Z',
  },
  {
    id: 'n-2',
    title: 'Password reset',
    type: 'PasswordReset',
    channel: 'Email',
    recipientEmail: 'ok@example.com',
    recipientId: 'u-2',
    lastError: 'Too many attempts',
    attemptCount: 5,
    requeueCount: 1,
    createdAt: '2026-08-18T09:00:00Z',
  },
];

function renderPage(overrides?: {
  events?: DeliverabilityEvent[];
  suppressions?: DeliverabilitySuppression[];
  deadLetters?: DeliverabilityDeadLetter[];
}) {
  render(
    <NextIntlClientProvider locale="en-US" messages={enMessages}>
      <EmailDeliverability
        events={overrides?.events ?? events}
        suppressions={overrides?.suppressions ?? suppressions}
        deadLetters={overrides?.deadLetters ?? deadLetters}
      />
    </NextIntlClientProvider>,
  );
}

async function switchTab(name: string) {
  await userEvent.click(screen.getByRole('tab', { name }));
}

describe('EmailDeliverability', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    actionMocks.unsuppressEmailAction.mockResolvedValue({ success: true, status: 'success' });
    actionMocks.requeueNotificationAction.mockResolvedValue({ success: true, status: 'success' });
  });

  it('renders the events tab from data: time, type badge, recipient, bounce diagnostic', () => {
    renderPage();

    expect(screen.getByText('hard@bouncer.co')).toBeInTheDocument();
    expect(screen.getByText('Permanent')).toBeInTheDocument();
    expect(screen.getByText('5.1.1 User unknown')).toBeInTheDocument();
    expect(screen.getByText('2026-08-19 10:03 UTC')).toBeInTheDocument();
    expect(screen.getByText('Delivery')).toBeInTheDocument();
    expect(screen.getByText('Send')).toBeInTheDocument();
  });

  it('renders the suppressions tab: email, reason, timestamps, unsuppress only for active rows', async () => {
    renderPage();
    await switchTab('Suppressions');

    expect(screen.getByText('released@example.com')).toBeInTheDocument();
    expect(screen.getByText('Hard bounce')).toBeInTheDocument();
    expect(screen.getByText('Complaint')).toBeInTheDocument();
    expect(screen.getByText('2026-08-01 08:00 UTC')).toBeInTheDocument();
    expect(screen.getByText('2026-08-10 08:00 UTC')).toBeInTheDocument();

    const unsuppressButtons = screen.getAllByRole('button', { name: 'Unsuppress' });
    expect(unsuppressButtons).toHaveLength(1); // released row gets no button
  });

  it('renders the dead letters tab: title, type, recipient, error, requeue states', async () => {
    renderPage();
    await switchTab('Dead letters');

    expect(screen.getByText('Monthly statement ready')).toBeInTheDocument();
    expect(screen.getByText('suppressed: hard bounce')).toBeInTheDocument();
    expect(screen.getByText('Password reset')).toBeInTheDocument();
  });

  it('disables requeue with a tooltip explanation when the recipient has an active suppression', async () => {
    renderPage();
    await switchTab('Dead letters');

    // n-1 recipient is actively suppressed → its requeue button is disabled
    const row = screen.getByText('Monthly statement ready').closest('tr');
    expect(row).not.toBeNull();
    const disabledButton = row?.querySelector('button:disabled');
    expect(disabledButton).not.toBeNull();
    expect(disabledButton).toHaveTextContent('Requeue');

    // tooltip content is rendered into a portal on hover/focus; assert the wrapper exists
    expect(row?.querySelector('.inline-flex')).not.toBeNull();

    // n-2 recipient is not suppressed → enabled
    const row2 = screen.getByText('Password reset').closest('tr');
    const enabledButtons = Array.from(row2?.querySelectorAll('button') ?? []).filter((b) => !b.disabled);
    expect(enabledButtons.some((b) => b.textContent === 'Requeue')).toBe(true);
  });

  it('empty state per tab when the feed has no rows', async () => {
    renderPage({ events: [], suppressions: [], deadLetters: [] });

    expect(screen.getByTestId('empty-events')).toHaveTextContent('No delivery events recorded yet.');
    await switchTab('Suppressions');
    expect(screen.getByTestId('empty-suppressions')).toHaveTextContent('No suppressed addresses.');
    await switchTab('Dead letters');
    expect(screen.getByTestId('empty-deadletters')).toHaveTextContent('No dead-lettered notifications.');
  });

  it('unsuppress confirms, calls the action with the email, and does not corrupt local state on failure', async () => {
    actionMocks.unsuppressEmailAction.mockResolvedValue({ success: false, status: 'error' });
    renderPage();
    await switchTab('Suppressions');

    await userEvent.click(screen.getByRole('button', { name: 'Unsuppress' }));

    expect(await screen.findByText('Release suppression?')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Release' }));

    await waitFor(() => {
      expect(actionMocks.unsuppressEmailAction).toHaveBeenCalledWith('hard@bouncer.co');
    });

    // failure path: table still intact (no rows removed, no crash)
    expect(screen.getByText('hard@bouncer.co')).toBeInTheDocument();
    expect(screen.getByText('released@example.com')).toBeInTheDocument();
  });

  it('requeue calls the action with the notification id', async () => {
    renderPage();
    await switchTab('Dead letters');

    const row = screen.getByText('Password reset').closest('tr');
    const requeueButton = Array.from(row?.querySelectorAll('button') ?? []).find(
      (b) => b.textContent === 'Requeue',
    );
    expect(requeueButton).toBeDefined();
    await userEvent.click(requeueButton as HTMLButtonElement);

    await waitFor(() => {
      expect(actionMocks.requeueNotificationAction).toHaveBeenCalledWith('n-2');
    });
  });

  it('timeline drawer renders events chronologically regardless of API order', async () => {
    actionMocks.getNotificationTimelineAction.mockResolvedValue({
      success: true,
      status: 'success',
      providerMessageId: 'ses-2',
      events: [
        { id: 'te-2', eventType: 'Delivery', occurredAt: '2026-08-19T10:02:00Z', recipientEmail: 'ok@example.com', bounceType: null, diagnosticCode: null, payloadPreview: null },
        { id: 'te-3', eventType: 'Open', occurredAt: '2026-08-19T11:00:00Z', recipientEmail: 'ok@example.com', bounceType: null, diagnosticCode: null, payloadPreview: '{"preview":"..."}' },
        { id: 'te-1', eventType: 'Send', occurredAt: '2026-08-19T10:01:00Z', recipientEmail: 'ok@example.com', bounceType: null, diagnosticCode: null, payloadPreview: null },
      ],
    });
    renderPage();
    await switchTab('Dead letters');

    await userEvent.click(screen.getByRole('button', { name: 'View timeline: Password reset' }));

    const list = await screen.findByTestId('timeline-events');
    const items = list.querySelectorAll('li');
    expect(items).toHaveLength(3);
    // chronological: Send, Delivery, Open — despite the shuffled API order
    expect(items[0]).toHaveAttribute('data-testid', 'timeline-event-te-1');
    expect(items[1]).toHaveAttribute('data-testid', 'timeline-event-te-2');
    expect(items[2]).toHaveAttribute('data-testid', 'timeline-event-te-3');
    expect(screen.getByText('Delivery timeline')).toBeInTheDocument();
    expect(screen.getByText('{"preview":"..."}')).toBeInTheDocument();
  });

  it('timeline drawer shows the no-correlation state for digest rows', async () => {
    actionMocks.getNotificationTimelineAction.mockResolvedValue({
      success: true,
      status: 'success',
      providerMessageId: null,
      events: [],
    });
    renderPage();
    await switchTab('Dead letters');

    await userEvent.click(screen.getByRole('button', { name: 'View timeline: Password reset' }));

    expect(await screen.findByTestId('timeline-no-correlation')).toBeInTheDocument();
  });

  it('timeline drawer shows an error state without crashing when the fetch fails', async () => {
    actionMocks.getNotificationTimelineAction.mockResolvedValue({
      success: false,
      status: 'error',
      providerMessageId: null,
      events: [],
    });
    renderPage();
    await switchTab('Dead letters');

    await userEvent.click(screen.getByRole('button', { name: 'View timeline: Password reset' }));

    await waitFor(() => {
      expect(screen.getByText("Action failed. Please try again.")).toBeInTheDocument();
    });
  });

  it('events tab dead-letter jump switches tab and filters by recipient', async () => {
    renderPage();

    // events[0] is the hard bounce for hard@bouncer.co, who owns dead letter n-1
    await userEvent.click(screen.getAllByRole('button', { name: 'View dead letters' })[0]);

    expect(screen.getByText('Monthly statement ready')).toBeInTheDocument();
    expect(screen.queryByText('Password reset')).not.toBeInTheDocument(); // other recipient filtered out

    await userEvent.click(screen.getByRole('button', { name: 'Show all' }));
    expect(screen.getByText('Password reset')).toBeInTheDocument();
  });
});
