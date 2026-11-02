import { describe, it, expect, vi, beforeEach } from 'vitest';
import { act, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

vi.mock('sonner', () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

const addComment = vi.fn();
const deleteComment = vi.fn();
const getBlogPostCommentsPage = vi.fn();
vi.mock('@/lib/blogs/actions', () => ({
  addComment: (...args: unknown[]) => addComment(...args),
  deleteComment: (...args: unknown[]) => deleteComment(...args),
  getBlogPostCommentsPage: (...args: unknown[]) => getBlogPostCommentsPage(...args),
}));

const widgetProps = vi.fn();
vi.mock('@/components/antibot/turnstile-widget', () => ({
  TurnstileWidget: (props: { onToken: (token: string | null) => void; id?: string }) => {
    widgetProps(props);
    return null;
  },
}));

const { BlogComments } = await import('@/components/blogs/blog-comments');
type BlogCommentsProps = import('@/components/blogs/blog-comments').BlogCommentsProps;

function comment(overrides: Partial<{
  id: string;
  authorHandle: string;
  authorDisplayName: string;
  content: string;
  parentCommentId: string | null;
}> = {}) {
  return {
    id: overrides.id ?? 'c1',
    authorHandle: overrides.authorHandle ?? 'alice',
    authorDisplayName: overrides.authorDisplayName ?? 'Alice',
    content: overrides.content ?? 'First comment',
    createdAt: overrides.id === 'c2' ? '2026-01-02T00:00:00Z' : '2026-01-01T00:00:00Z',
    parentCommentId: overrides.parentCommentId ?? null,
  };
}

const baseProps: BlogCommentsProps = {
  postId: 'post-1',
  allowComments: true,
  currentUserId: null,
  isPostAuthor: false,
  coAuthorIds: [],
};

function seed(page = { items: [], hasMore: false }) {
  getBlogPostCommentsPage.mockResolvedValue(page);
}

beforeEach(() => {
  vi.clearAllMocks();
  seed();
});

describe('BlogComments', () => {
  it('renders comment threads with replies nested and oldest-first ordering preserved', async () => {
    seed({
      items: [
        comment({ id: 'c1' }),
        comment({ id: 'c2', parentCommentId: 'c1', authorHandle: 'bob', authorDisplayName: 'Bob', content: 'A reply' }),
        comment({ id: 'c3', authorHandle: 'carol', authorDisplayName: 'Carol', content: 'Second root' }),
      ],
      hasMore: false,
    });

    render(<BlogComments {...baseProps} />);

    await waitFor(() => expect(screen.getByText('First comment')).toBeInTheDocument());
    expect(screen.getByText('A reply')).toBeInTheDocument();
    expect(screen.getByText('Second root')).toBeInTheDocument();
    expect(getBlogPostCommentsPage).toHaveBeenCalledWith('post-1');
  });

  it('hides the reply button on replies (depth-1 enforcement) while keeping it on roots', async () => {
    seed({
      items: [
        comment({ id: 'c1', authorHandle: 'alice', authorDisplayName: 'Alice' }),
        comment({ id: 'c2', parentCommentId: 'c1', authorHandle: 'bob', authorDisplayName: 'Bob', content: 'A reply' }),
      ],
      hasMore: false,
    });

    render(<BlogComments {...baseProps} currentUserId="user-1" />);

    await waitFor(() => expect(screen.getByText('A reply')).toBeInTheDocument());

    const rootRegion = screen.getByText('First comment').closest('[data-comment-id]') as HTMLElement;
    expect(within(rootRegion).getByRole('button', { name: /reply to alice/i })).toBeInTheDocument();

    const replyRegion = screen.getByText('A reply').closest('[data-comment-id]') as HTMLElement;
    expect(within(replyRegion).queryByRole('button', { name: /reply/i })).not.toBeInTheDocument();
  });

  it('paginates with load more using the cursor of the last item', async () => {
    seed({
      items: [comment({ id: 'c1', content: 'Root one' })],
      hasMore: true,
    });

    render(<BlogComments {...baseProps} />);
    await waitFor(() => expect(screen.getByText('Root one')).toBeInTheDocument());

    const user = userEvent.setup();
    getBlogPostCommentsPage.mockResolvedValueOnce({
      items: [comment({ id: 'c9', content: 'Root two' })],
      hasMore: false,
    });

    await user.click(screen.getByRole('button', { name: /load more comments/i }));

    await waitFor(() => expect(screen.getByText('Root two')).toBeInTheDocument());
    expect(getBlogPostCommentsPage).toHaveBeenLastCalledWith('post-1', {
      afterCreatedAt: '2026-01-01T00:00:00Z',
      afterId: 'c1',
    });
  });

  it('renders composer for signed-in users and submits through addComment', async () => {
    render(<BlogComments {...baseProps} currentUserId="user-1" />);
    await waitFor(() => expect(screen.queryByText(/loading comments/i)).not.toBeInTheDocument());

    const user = userEvent.setup();
    addComment.mockResolvedValue({
      success: true,
      data: comment({ id: 'new', authorHandle: 'me', authorDisplayName: 'Me', content: 'Fresh' }),
    });

    await user.type(screen.getByLabelText(/write a comment/i), 'Fresh');
    await user.click(screen.getByRole('button', { name: /publish comment/i }));

    await waitFor(() => expect(addComment).toHaveBeenCalledWith('post-1', expect.objectContaining({ content: 'Fresh' })));
    expect(await screen.findByText('Fresh')).toBeInTheDocument();
  });

  it('sends the turnstile token and resets it to null after a failed submit', async () => {
    const user = userEvent.setup();
    addComment
      .mockResolvedValueOnce({ success: false, error: 'Bot check failed. Please refresh and try again.', status: 403 })
      .mockResolvedValueOnce({ success: true, data: comment({ id: 'c9', content: 'Retry' }) });

    render(<BlogComments {...baseProps} currentUserId="user-1" />);
    await waitFor(() => expect(widgetProps).toHaveBeenCalled());
    const onToken = (widgetProps.mock.calls.at(-1)?.[0] as { onToken: (token: string | null) => void }).onToken;
    act(() => onToken('tok-1'));

    await user.type(screen.getByLabelText(/write a comment/i), 'First try');
    await user.click(screen.getByRole('button', { name: /publish comment/i }));
    await waitFor(() => expect(addComment).toHaveBeenCalledTimes(1));
    expect(addComment.mock.calls[0]?.[1]).toMatchObject({ turnstileToken: 'tok-1' });
    expect(await screen.findByRole('alert')).toHaveTextContent(/bot check failed/i);

    // Single-use token: the failed submit consumed it, so the retry must send null.
    await user.type(screen.getByLabelText(/write a comment/i), ' ');
    await user.click(screen.getByRole('button', { name: /publish comment/i }));
    await waitFor(() => expect(addComment).toHaveBeenCalledTimes(2));
    expect(addComment.mock.calls[1]?.[1]).toMatchObject({ turnstileToken: null });
  });

  it('clears the single-use turnstile token after a successful submit', async () => {
    const user = userEvent.setup();
    addComment
      .mockResolvedValueOnce({ success: true, data: comment({ id: 'c8', content: 'First' }) })
      .mockResolvedValueOnce({ success: true, data: comment({ id: 'c9', content: 'Second' }) });

    render(<BlogComments {...baseProps} currentUserId="user-1" />);
    await waitFor(() => expect(widgetProps).toHaveBeenCalled());
    const onToken = (widgetProps.mock.calls.at(-1)?.[0] as { onToken: (token: string | null) => void }).onToken;
    act(() => onToken('tok-1'));

    await user.type(screen.getByLabelText(/write a comment/i), 'First');
    await user.click(screen.getByRole('button', { name: /publish comment/i }));
    await waitFor(() => expect(addComment).toHaveBeenCalledTimes(1));
    expect(addComment.mock.calls[0]?.[1]).toMatchObject({ turnstileToken: 'tok-1' });
    expect(await screen.findByText('First')).toBeInTheDocument();

    // Single-use token: the successful submit consumed it, so the next submit sends null.
    await user.type(screen.getByLabelText(/write a comment/i), 'Second');
    await user.click(screen.getByRole('button', { name: /publish comment/i }));
    await waitFor(() => expect(addComment).toHaveBeenCalledTimes(2));
    expect(addComment.mock.calls[1]?.[1]).toMatchObject({ turnstileToken: null });
  });

  it('keeps a token re-issued by the widget during a pending submit', async () => {
    const user = userEvent.setup();
    let resolveFirst!: (value: { success: boolean; error?: string; status?: number }) => void;
    addComment.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          resolveFirst = resolve;
        }),
    );
    addComment.mockResolvedValueOnce({ success: true, data: comment({ id: 'c9', content: 'Retry' }) });

    render(<BlogComments {...baseProps} currentUserId="user-1" />);
    await waitFor(() => expect(widgetProps).toHaveBeenCalled());
    const onToken = (widgetProps.mock.calls.at(-1)?.[0] as { onToken: (token: string | null) => void }).onToken;
    act(() => onToken('tok-1'));

    await user.type(screen.getByLabelText(/write a comment/i), 'First try');
    await user.click(screen.getByRole('button', { name: /publish comment/i }));
    await waitFor(() => expect(addComment).toHaveBeenCalledTimes(1));

    // The widget re-issues a fresh token while the submit is still in flight.
    act(() => onToken('tok-2'));
    act(() => resolveFirst({ success: false, error: 'Bot check failed.', status: 403 }));
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent(/bot check failed/i));

    await user.type(screen.getByLabelText(/write a comment/i), 'Retry');
    await user.click(screen.getByRole('button', { name: /publish comment/i }));
    await waitFor(() => expect(addComment).toHaveBeenCalledTimes(2));
    expect(addComment.mock.calls[1]?.[1]).toMatchObject({ turnstileToken: 'tok-2' });
  });

  it('shows the comments-disabled notice and hides the composer when AllowComments=false', async () => {
    render(<BlogComments {...baseProps} allowComments={false} currentUserId="user-1" />);

    await waitFor(() => expect(screen.queryByText(/loading comments/i)).not.toBeInTheDocument());
    expect(screen.getByTestId('comments-disabled-notice')).toHaveTextContent(/comments are disabled/i);
    expect(screen.queryByLabelText(/write a comment/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /publish comment/i })).not.toBeInTheDocument();
  });

  it('delete button matrix: comment author yes, co-author yes, post author yes, stranger no', async () => {
    seed({
      items: [comment({ id: 'c1', authorHandle: 'alice', authorDisplayName: 'Alice' })],
      hasMore: false,
    });

    function asserts(currentUserId: string | null, isPostAuthor: boolean, coAuthorIds: string[], currentUserHandle?: string | null) {
      return (
        <BlogComments
          {...baseProps}
          currentUserId={currentUserId}
          isPostAuthor={isPostAuthor}
          coAuthorIds={coAuthorIds}
          currentUserHandle={currentUserHandle}
        />
      );
    }

    const view = render(asserts('user-1', false, [], 'alice'));
    expect(
      await view.findByRole('button', { name: /delete comment by alice/i }),
    ).toBeInTheDocument();
    view.unmount();

    const coAuthorView = render(asserts('user-2', false, ['user-2'], 'someone-else'));
    expect(await coAuthorView.findByRole('button', { name: /delete comment by alice/i })).toBeInTheDocument();
    coAuthorView.unmount();

    const postAuthorView = render(asserts('user-3', true, [], 'unrelated'));
    expect(await postAuthorView.findByRole('button', { name: /delete comment by alice/i })).toBeInTheDocument();
    postAuthorView.unmount();

    const strangerView = render(asserts('user-4', false, [], 'stranger'));
    await waitFor(() => expect(strangerView.getByText('First comment')).toBeInTheDocument());
    expect(strangerView.queryByRole('button', { name: /delete comment by alice/i })).not.toBeInTheDocument();
    strangerView.unmount();
  });

  it('hides delete entirely for anonymous visitors', async () => {
    seed({ items: [comment({ id: 'c1' })], hasMore: false });
    const view = render(<BlogComments {...baseProps} />);
    await waitFor(() => expect(view.getByText('First comment')).toBeInTheDocument());
    expect(view.queryByRole('button', { name: /delete comment/i })).not.toBeInTheDocument();
  });

  it('confirms before delete, removes optimistically, and reconciles from the server on failure', async () => {
    seed({ items: [comment({ id: 'c1' })], hasMore: false });
    const user = userEvent.setup();

    const view = render(<BlogComments {...baseProps} currentUserId="user-1" currentUserHandle="alice" />);
    await user.click(await view.findByRole('button', { name: /delete comment by alice/i }));

    expect(view.getByRole('alertdialog')).toBeInTheDocument();

    deleteComment.mockResolvedValueOnce({ success: false, error: 'Denied', status: 403 });
    getBlogPostCommentsPage.mockResolvedValueOnce({
      items: [comment({ id: 'c1', content: 'First comment' })],
      hasMore: false,
    });

    await user.click(within(view.getByRole('alertdialog')).getByRole('button', { name: /confirm delete comment/i }));

    await waitFor(() => expect(deleteComment).toHaveBeenCalledWith('c1'));
    await waitFor(() => expect(getBlogPostCommentsPage).toHaveBeenCalledTimes(2));
    expect(await view.findByText('First comment')).toBeInTheDocument();
    await waitFor(() => expect(view.getByRole('alert')).toHaveTextContent('Denied'));
  });

  it('replies attach under the selected root via addComment with parentCommentId', async () => {
    seed({
      items: [comment({ id: 'c1', authorHandle: 'alice', authorDisplayName: 'Alice' })],
      hasMore: false,
    });
    const user = userEvent.setup();

    render(<BlogComments {...baseProps} currentUserId="user-1" />);
    await waitFor(() => expect(screen.getByText('First comment')).toBeInTheDocument());

    addComment.mockResolvedValue({
      success: true,
      data: comment({ id: 'new', parentCommentId: 'c1', authorHandle: 'me', authorDisplayName: 'Me', content: 'Reply body' }),
    });

    await user.click(screen.getByRole('button', { name: /reply to alice/i }));
    await user.type(screen.getByLabelText(/write a reply/i), 'Reply body');
    await user.click(screen.getByRole('button', { name: /publish reply/i }));

    await waitFor(() =>
      expect(addComment).toHaveBeenCalledWith('post-1', expect.objectContaining({ content: 'Reply body', parentCommentId: 'c1' })),
    );
    expect(await screen.findByText('Reply body')).toBeInTheDocument();
  });
});
