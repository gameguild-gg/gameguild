import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('next-intl', () => ({ useTranslations: () => (key: string, values?: Record<string, number>) => values ? `${key}:${values.count}` : key }));
vi.mock('@/i18n/navigation', () => ({ Link: ({ children, href, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement>) => <a href={String(href)} {...props}>{children}</a> }));
vi.mock('./economy-console-actions', () => ({ EconomyConsoleActions: ({ surface }: { surface: string }) => <div data-testid="console-actions">{surface}</div> }));

import { EconomyOperationalConsole } from './economy-operational-console';
import { EconomyOverview } from './economy-overview';
import {
  EconomyActionNotice,
  EconomyIssue,
  EconomyPageHeader,
  EconomyWorkspace,
  formatEconomyDate,
  formatEconomyUnits,
} from './economy-ui';

describe('Economy presentational surfaces', () => {
  it('renders headers, issues, action outcomes, workspace content, and formatters', () => {
    const { rerender } = render(<EconomyPageHeader title="Wallet" description="Safe reads" badge="Disabled" />);
    expect(screen.getByText('Wallet')).toBeInTheDocument();
    expect(screen.getByText('Disabled')).toBeInTheDocument();
    rerender(<EconomyPageHeader title="Wallet" description="Safe reads" />);
    expect(screen.queryByText('Disabled')).not.toBeInTheDocument();

    rerender(<EconomyIssue issue="provider unavailable" />);
    expect(screen.getByText('provider unavailable')).toBeInTheDocument();
    rerender(<EconomyIssue issue={null} />);
    expect(screen.queryByText('provider unavailable')).not.toBeInTheDocument();

    rerender(<EconomyActionNotice result={{ success: true, message: 'recorded' }} />);
    expect(screen.getByText('Recorded safely')).toBeInTheDocument();
    rerender(<EconomyActionNotice result={{ success: false, message: 'blocked' }} />);
    expect(screen.getByText('Action not completed')).toBeInTheDocument();
    rerender(<EconomyActionNotice result={null} />);
    expect(screen.queryByText('Action not completed')).not.toBeInTheDocument();

    rerender(<EconomyWorkspace><span>content</span></EconomyWorkspace>);
    expect(screen.getByText('content')).toBeInTheDocument();
    expect(formatEconomyDate(null)).toBe('Not available');
    expect(formatEconomyDate('2026-08-30T12:00:00.000Z')).not.toBe('Not available');
    expect(formatEconomyUnits(null)).toBe('0');
    expect(formatEconomyUnits(1234).replace(/\D/g, '')).toBe('1234');
  });

  it('renders the complete wallet overview with readiness and safe fallback values', () => {
    const data = {
      issue: 'safe read warning',
      wallet: { withdrawableHard: 10, availableHardToSpend: 20, availableSoftToSpend: 30, pendingHard: 4, heldHard: 6 },
      transactions: [], payoutRequests: [], payoutOperations: [],
      capabilities: [
        { capability: 'Payout', state: 'Ready', diagnostics: ['provider ready'] },
        { capability: 'Marketplace', state: 'Disabled', diagnostics: [] },
      ],
    };
    const { rerender } = render(<EconomyOverview data={data as never} />);
    expect(screen.getByText('safe read warning')).toBeInTheDocument();
    expect(screen.getByText('provider ready')).toBeInTheDocument();
    expect(screen.getByText('common.empty')).toBeInTheDocument();
    expect(screen.getAllByRole('link')).toHaveLength(4);

    rerender(<EconomyOverview data={{ ...data, issue: null, wallet: null, capabilities: [] } as never} />);
    expect(screen.queryByText('safe read warning')).not.toBeInTheDocument();
    expect(screen.getAllByText('0').length).toBeGreaterThanOrEqual(4);
  });

  it('renders console records, empty sections, redacted value shapes, and action surface', () => {
    render(<EconomyOperationalConsole
      title="Ledger"
      description="Integrity evidence"
      surface="ledger"
      data={{
        issue: 'anchor stale',
        records: [],
        sections: [
          {
            label: 'Runs',
            records: [
              { id: 'run-1', state: 'Ready', version: 2, updatedAt: null, isHealthy: true, ready: false },
              { capability: 'Payout', state: ['one', 'two'], status: { redacted: true } },
              { type: 'Anchor', status: false },
              { amountUnits: 10 },
            ],
          },
          { label: 'Empty', records: [] },
        ],
      }}
    />);
    expect(screen.getByTestId('console-actions')).toHaveTextContent('ledger');
    expect(screen.getByText('anchor stale')).toBeInTheDocument();
    expect(screen.getAllByText('run-1').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Payout').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Anchor').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('common.empty').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('common.itemCount:2').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('common.detailAvailable').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('common.yes').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('common.no').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('—')).toBeInTheDocument();
  });
});
