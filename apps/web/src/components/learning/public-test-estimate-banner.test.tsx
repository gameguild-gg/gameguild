import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ computeScore: vi.fn() }));
vi.mock('@/lib/emception/scoring', () => ({ computeScore: mocks.computeScore }));

import { PublicTestEstimateBanner } from './public-test-estimate-banner';

const report = { passed: 2, failed: 1, cases: [], totalDurationMs: 0 };

describe('PublicTestEstimateBanner', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows the public result and finite score estimate', () => {
    mocks.computeScore.mockReturnValue({ score: 80 });
    render(<PublicTestEstimateBanner report={report} plan={{ cases: [{ name: 'test' }] } as never} maxScore={100} passingScore={60} />);
    expect(screen.getByRole('status')).toHaveTextContent('2/3 passed');
    expect(screen.getByRole('status')).toHaveTextContent('estimated score: 80/100');
    expect(mocks.computeScore).toHaveBeenCalledWith(report, expect.anything(), 100, 60);
  });

  it.each([
    [{ cases: [] }, null],
    [{ cases: [{ name: 'test' }] }, { score: Number.NaN }],
  ])('shows unavailable when a score cannot be estimated', (plan, score) => {
    if (score) mocks.computeScore.mockReturnValue(score);
    render(<PublicTestEstimateBanner report={report} plan={plan as never} maxScore={100} passingScore={60} />);
    expect(screen.getByRole('alert')).toHaveTextContent('Estimate unavailable.');
  });

  it('shows unavailable when scoring throws', () => {
    mocks.computeScore.mockImplementation(() => { throw new Error('invalid report'); });
    render(<PublicTestEstimateBanner report={report} plan={{ cases: [{}] } as never} maxScore={100} passingScore={60} />);
    expect(screen.getByRole('alert')).toHaveTextContent('Estimate unavailable.');
  });
});
