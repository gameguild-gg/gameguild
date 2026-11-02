import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  isGitHubLinked: vi.fn(),
}));

vi.mock('@/lib/community/github-star-prompt', () => ({
  isGitHubLinkedAction: mocks.isGitHubLinked,
}));

import { GitHubStarPrompt } from './github-star-prompt';

const DISMISSED_KEY = 'gg-github-star-prompt-dismissed';
const SESSION_KEY = 'gg-github-star-prompt-checked';

describe('GitHubStarPrompt (issue #90)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
    mocks.isGitHubLinked.mockReset();
    // Issue #106 phase 1: the repository metadata fetch lives on the server
    // now — the browser stub asserts the client never calls api.github.com.
    vi.stubGlobal('fetch', vi.fn());
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it('shows the prompt with the server-provided star count for GitHub-linked members', async () => {
    mocks.isGitHubLinked.mockResolvedValue(true);

    render(<GitHubStarPrompt starCount={128} />);

    const prompt = await screen.findByTestId('github-star-prompt');
    expect(prompt).toBeInTheDocument();
    expect(screen.getByText(/join 128 stargazers/)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Star us' })).toHaveAttribute(
      'href',
      'https://github.com/gameguild-gg/gameguild',
    );
    expect(globalThis.fetch).not.toHaveBeenCalled();
  });

  it('renders without a count when the server lookup failed (starCount null)', async () => {
    mocks.isGitHubLinked.mockResolvedValue(true);

    render(<GitHubStarPrompt starCount={null} />);

    const prompt = await screen.findByTestId('github-star-prompt');
    expect(prompt).toBeInTheDocument();
    expect(screen.queryByText(/stargazers/)).not.toBeInTheDocument();
  });

  it('never renders for members without a GitHub link', async () => {
    mocks.isGitHubLinked.mockResolvedValue(false);

    render(<GitHubStarPrompt starCount={128} />);

    await waitFor(() => expect(mocks.isGitHubLinked).toHaveBeenCalledTimes(1));
    expect(screen.queryByTestId('github-star-prompt')).not.toBeInTheDocument();
  });

  it('stays hidden for the rest of the session once checked (rate limit)', async () => {
    mocks.isGitHubLinked.mockResolvedValue(true);
    sessionStorage.setItem(SESSION_KEY, '1');

    render(<GitHubStarPrompt starCount={128} />);

    // The session guard short-circuits before any server call.
    await new Promise((resolve) => setTimeout(resolve, 25));
    expect(mocks.isGitHubLinked).not.toHaveBeenCalled();
    expect(screen.queryByTestId('github-star-prompt')).not.toBeInTheDocument();
  });

  it('stays hidden after dismissal persists to localStorage', async () => {
    mocks.isGitHubLinked.mockResolvedValue(true);
    localStorage.setItem(DISMISSED_KEY, '1');

    render(<GitHubStarPrompt starCount={128} />);

    await new Promise((resolve) => setTimeout(resolve, 25));
    expect(mocks.isGitHubLinked).not.toHaveBeenCalled();
    expect(screen.queryByTestId('github-star-prompt')).not.toBeInTheDocument();
  });

  it('dismissal from the banner persists and hides it', async () => {
    mocks.isGitHubLinked.mockResolvedValue(true);

    render(<GitHubStarPrompt starCount={128} />);
    await screen.findByTestId('github-star-prompt');

    fireEvent.click(screen.getByRole('button', { name: 'Dismiss star prompt' }));

    expect(localStorage.getItem(DISMISSED_KEY)).toBe('1');
    expect(screen.queryByTestId('github-star-prompt')).not.toBeInTheDocument();
  });
});
