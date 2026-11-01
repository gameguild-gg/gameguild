import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  submitContentCreatorApplication: vi.fn(),
}));

vi.mock('@/lib/content-creators/actions', async (importOriginal) => {
  const original = await importOriginal<typeof import('@/lib/content-creators/actions')>();
  return {
    ...original,
    submitContentCreatorApplicationAction: mocks.submitContentCreatorApplication,
  };
});

vi.mock('next/navigation', () => ({
  usePathname: vi.fn(() => '/content-creators/apply'),
}));

import ApplyPage from './page';

describe('/content-creators/apply wizard (issue #105)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.submitContentCreatorApplication.mockResolvedValue({
      success: true,
      message: 'Application received! Our team will review it and reach out with next steps.',
    });
  });

  afterEach(cleanup);

  it('renders the three application steps', () => {
    render(<ApplyPage />);

    expect(screen.getByRole('heading', { name: 'Apply to be a content creator' })).toBeInTheDocument();
    expect(screen.getByText('Step 1 of 3 — Application')).toBeInTheDocument();
  });

  it('walks through application, creator profile, and first course setup, then submits', async () => {
    const user = userEvent.setup();
    render(<ApplyPage />);

    // Step 1 — application.
    await user.type(screen.getByLabelText('Full name'), 'Ada Lovelace');
    await user.type(screen.getByLabelText('Email'), 'ada@example.com');
    await user.selectOptions(screen.getByLabelText('Content area'), 'Game programming');
    await user.type(
      screen.getByLabelText('Why do you want to create with us?'),
      'I have shipped three games and want to teach shader fundamentals.',
    );
    await user.click(screen.getByRole('button', { name: 'Continue' }));

    // Step 2 — creator profile.
    expect(screen.getByText('Step 2 of 3 — Creator profile')).toBeInTheDocument();
    await user.type(screen.getByLabelText('Creator handle'), 'shaderwizard');
    await user.type(
      screen.getByLabelText('Creator bio'),
      'Graphics programmer with eight years of experience across indie and AAA teams.',
    );
    await user.click(screen.getByText('Written courses'));
    await user.click(screen.getByRole('button', { name: 'Continue' }));

    // Step 3 — first course setup.
    expect(screen.getByText('Step 3 of 3 — First course setup')).toBeInTheDocument();
    await user.type(screen.getByLabelText('First course working title'), 'Shader Basics for Game Developers');
    await user.type(screen.getByLabelText('Who is it for?'), 'Intermediate Unity developers new to shaders');
    await user.type(
      screen.getByLabelText('Course outline sketch'),
      'Week 1: pipeline basics. Week 2: first shader. Week 3: lighting. Final: stylized water.',
    );

    await user.click(screen.getByRole('button', { name: 'Submit application' }));

    await waitFor(() =>
      expect(screen.getByText(/Application received!/i)).toBeInTheDocument(),
    );

    expect(mocks.submitContentCreatorApplication).toHaveBeenCalledTimes(1);
    const payload = mocks.submitContentCreatorApplication.mock.calls[0][0];
    expect(payload.name).toBe('Ada Lovelace');
    expect(payload.email).toBe('ada@example.com');
    expect(payload.focusArea).toBe('Game programming');
    expect(payload.creatorHandle).toBe('shaderwizard');
    expect(payload.preferredFormats).toEqual(['Written courses']);
    expect(payload.courseTitle).toBe('Shader Basics for Game Developers');
  });

  it('blocks continuing past step 1 with incomplete data', async () => {
    const user = userEvent.setup();
    render(<ApplyPage />);

    await user.click(screen.getByRole('button', { name: 'Continue' }));

    expect(screen.getByText('Your name is required.')).toBeInTheDocument();
    expect(screen.getByText('Step 1 of 3 — Application')).toBeInTheDocument();
  });
});
