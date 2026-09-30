import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { cloneElement, isValidElement, type ComponentProps, type ReactElement, type ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { createCourseDiscussion } from '@/lib/learner/activity-actions';

const mocks = vi.hoisted(() => ({
  createDiscussion: vi.fn<typeof createCourseDiscussion>(),
  refresh: vi.fn(),
}));

vi.mock('@/i18n/navigation', () => ({
  useRouter: () => ({ refresh: mocks.refresh }),
  Link: ({ children, href, ...props }: ComponentProps<'a'> & { children: ReactNode }) => <a href={String(href)} {...props}>{children}</a>,
}));
vi.mock('@/lib/learner/activity-actions', () => ({ createCourseDiscussion: mocks.createDiscussion }));
vi.mock('@game-guild/ui/components/button', () => ({
  buttonVariants: () => '',
  Button: (props: ComponentProps<'button'> & {
    asChild?: boolean;
    render?: ReactElement;
    variant?: string;
    size?: string;
    children: ReactNode;
  }) => {
    const target = props.render ?? (props.asChild && isValidElement(props.children) ? props.children : null);
    const buttonProps = { ...props };
    delete buttonProps.asChild;
    delete buttonProps.render;
    delete buttonProps.variant;
    delete buttonProps.size;
    delete buttonProps.children;

    return target
      ? cloneElement(target, {
          ...buttonProps,
          children: props.asChild ? target.props.children : props.children,
        } as never)
      : <button {...buttonProps}>{props.children}</button>;
  },
}));
vi.mock('@game-guild/ui/components/dialog', () => ({
  Dialog: ({ children, onOpenChange }: { children: ReactNode; onOpenChange: (open: boolean) => void }) => (
    <div>
      <button type="button" onClick={() => onOpenChange(true)}>Open dialog state</button>
      <button type="button" onClick={() => onOpenChange(false)}>Close dialog state</button>
      {children}
    </div>
  ),
  DialogTrigger: ({ children }: { children: ReactNode }) => children,
  DialogContent: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  DialogDescription: ({ children }: { children: ReactNode }) => <p>{children}</p>,
  DialogHeader: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  DialogTitle: ({ children }: { children: ReactNode }) => <h2>{children}</h2>,
}));
vi.mock('@game-guild/ui/components/alert', () => ({
  Alert: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  AlertDescription: ({ children }: { children: ReactNode }) => <p>{children}</p>,
  AlertTitle: ({ children }: { children: ReactNode }) => <h3>{children}</h3>,
}));
vi.mock('@game-guild/ui/components/card', () => ({
  Card: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  CardContent: ({ children }: { children: ReactNode }) => <div>{children}</div>,
}));
vi.mock('@game-guild/ui/components/badge', () => ({ Badge: ({ children }: { children: ReactNode }) => <span>{children}</span> }));
vi.mock('@game-guild/ui/components/input', () => ({ Input: (props: ComponentProps<'input'>) => <input {...props} /> }));
vi.mock('@game-guild/ui/components/textarea', () => ({ Textarea: (props: ComponentProps<'textarea'>) => <textarea {...props} /> }));

import { CourseCommunity } from './course-community';

function renderCommunity(discussions: Record<string, unknown>[] = []) {
  return render(
    <CourseCommunity
      courseId="course-1"
      courseSlug="game-ai"
      courseTitle="Game AI"
      discussions={discussions as never}
    />,
  );
}

function fillAndSubmit() {
  fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Help with agents' } });
  fireEvent.change(screen.getByLabelText('Message'), { target: { value: 'How should this work?' } });
  fireEvent.click(screen.getByRole('button', { name: 'Publish discussion' }));
}

describe('CourseCommunity', () => {
  beforeEach(() => vi.clearAllMocks());

  it('renders the empty state and publishes a discussion once', async () => {
    const pending = Promise.withResolvers<{ success: boolean }>();
    mocks.createDiscussion.mockReturnValue(pending.promise);
    renderCommunity();
    expect(screen.getByText('No discussions yet')).toBeInTheDocument();

    fillAndSubmit();
    expect(await screen.findByRole('button', { name: 'Publishing...' })).toBeDisabled();
    pending.resolve({ success: true });
    expect(await screen.findByText('Discussion published')).toBeInTheDocument();
    expect(mocks.createDiscussion).toHaveBeenCalledOnce();
    const formData = mocks.createDiscussion.mock.calls[0]![0] as FormData;
    expect(Object.fromEntries(formData.entries())).toMatchObject({
      courseId: 'course-1', courseSlug: 'game-ai', title: 'Help with agents', content: 'How should this work?',
    });
    expect(mocks.refresh).toHaveBeenCalledOnce();
  });

  it.each([
    [{ success: false, error: 'Discussion is locked.' }, 'Discussion is locked.'],
    [{ success: false, error: '' }, 'The discussion could not be published.'],
  ])('shows API publication failures', async (result, expected) => {
    mocks.createDiscussion.mockResolvedValue(result);
    renderCommunity();
    fillAndSubmit();
    expect(await screen.findByText(expected)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Publish discussion' })).toBeEnabled();
  });

  it.each([
    [new Error('Network unavailable.'), 'Network unavailable.'],
    ['offline', 'The discussion could not be published.'],
  ])('normalizes unexpected publication failures', async (error, expected) => {
    mocks.createDiscussion.mockRejectedValue(error);
    renderCommunity();
    fillAndSubmit();
    expect(await screen.findByText(expected)).toBeInTheDocument();
  });

  it('clears transient success and error state when the dialog closes', async () => {
    mocks.createDiscussion.mockResolvedValue({ success: false, error: 'Failed.' });
    renderCommunity();
    fireEvent.click(screen.getByRole('button', { name: 'Open dialog state' }));
    fillAndSubmit();
    expect(await screen.findByText('Failed.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Close dialog state' }));
    await waitFor(() => expect(screen.queryByText('Failed.')).not.toBeInTheDocument());
  });

  it('renders discussions, default metadata, state badges, and links', () => {
    renderCommunity([
      { id: 'discussion-1', title: 'Pinned topic', content: 'Content', isPinned: true, isResolved: true, replyCount: 4 },
      { id: 'discussion-2', title: '', content: 'Fallback content', isPinned: false, isResolved: false, replyCount: null },
    ]);
    expect(screen.getByText('Pinned topic')).toBeInTheDocument();
    expect(screen.getByText('Course discussion')).toBeInTheDocument();
    expect(screen.getByText('Pinned')).toBeInTheDocument();
    expect(screen.getByText('Resolved')).toBeInTheDocument();
    expect(screen.getByText('4 replies')).toBeInTheDocument();
    expect(screen.getByText('0 replies')).toBeInTheDocument();
    const links = screen.getAllByRole('link', { name: /Open discussion/ });
    expect(links[0]).toHaveAttribute('href', '/learn/courses/game-ai/community/discussion-1');
  });
});
