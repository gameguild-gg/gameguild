import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

vi.mock('sonner', () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

const fetchViewerReaction = vi.fn();
const setReaction = vi.fn();
vi.mock('@/components/blogs/blog-reactions', async (importOriginal) => {
  const original = await importOriginal<typeof import('@/components/blogs/blog-reactions')>();
  return original;
});

const { BlogReactions } = await import('@/components/blogs/blog-reactions');
type BlogReactionsProps = import('@/components/blogs/blog-reactions').BlogReactionsProps;

const baseProps: BlogReactionsProps = {
  postId: 'post-1',
  currentUserId: null,
  initialReactionCount: 4,
};

beforeEach(() => {
  vi.clearAllMocks();
});

describe('BlogReactions', () => {
  it('shows the count and a sign-in prompt for anonymous visitors, with the toggle disabled', () => {
    render(<BlogReactions {...baseProps} />);

    const toggle = screen.getByRole('button', { name: /sign in to like/i });
    expect(toggle).toBeDisabled();
    expect(toggle).toHaveTextContent('4');
    expect(screen.getByRole('link', { name: /sign in to like/i })).toHaveAttribute('href', expect.stringContaining('/sign-in'));
  });

  it('hydrates the viewer reaction state for signed-in users', async () => {
    const mod = await import('@/components/blogs/blog-reactions');
    // -expect-error test seam on the component module
    mod.__testHooks.fetchViewerReaction = fetchViewerReaction;
    fetchViewerReaction.mockResolvedValue({ reacted: true });

    render(<BlogReactions {...baseProps} currentUserId="user-1" />);

    await waitFor(() =>
      expect(screen.getByRole('button', { name: /remove like/i })).toHaveAttribute('aria-pressed', 'true'),
    );
  });

  it('toggles like on click with optimistic count and reconciles on failure', async () => {
    const mod = await import('@/components/blogs/blog-reactions');
    // -expect-error test seam on the component module
    mod.__testHooks.fetchViewerReaction = fetchViewerReaction;
    mod.__testHooks.setReaction = setReaction;
    fetchViewerReaction.mockResolvedValue({ reacted: false });
    setReaction.mockResolvedValue({ ok: false });

    const user = userEvent.setup();
    render(<BlogReactions {...baseProps} currentUserId="user-1" />);
    await waitFor(() => expect(screen.getByRole('button', { name: /like this post/i })).toHaveAttribute('aria-pressed', 'false'));

    await user.click(screen.getByRole('button', { name: /like this post/i }));

    expect(setReaction).toHaveBeenCalledWith('post-1', true);
    await waitFor(() =>
      expect(screen.getByRole('button', { name: /like this post/i })).toHaveTextContent('4'),
    );
  });

  it('rolls the optimistic state back when the mutation rejects', async () => {
    const mod = await import('@/components/blogs/blog-reactions');
    // -expect-error test seam on the component module
    mod.__testHooks.fetchViewerReaction = fetchViewerReaction;
    mod.__testHooks.setReaction = setReaction;
    fetchViewerReaction.mockResolvedValue({ reacted: false });
    setReaction.mockRejectedValue(new Error('network down'));

    const user = userEvent.setup();
    render(<BlogReactions {...baseProps} currentUserId="user-1" />);
    await waitFor(() => expect(screen.getByRole('button', { name: /like this post/i })).toBeInTheDocument());

    await user.click(screen.getByRole('button', { name: /like this post/i }));

    await waitFor(() =>
      expect(screen.getByRole('button', { name: /like this post/i })).toHaveAttribute('aria-pressed', 'false'),
    );
    expect(screen.getByRole('button', { name: /like this post/i })).toHaveTextContent('4');
  });
});
