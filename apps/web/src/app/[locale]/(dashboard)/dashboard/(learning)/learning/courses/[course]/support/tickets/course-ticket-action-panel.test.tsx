import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { addCourseSupportTicketMessage, resolveCourseSupportTicket } from '@/lib/learning/actions';
import { CourseTicketActionPanel } from './course-ticket-action-panel';

const refresh = vi.fn();
vi.mock('next/navigation', () => ({ useRouter: () => ({ refresh }) }));
vi.mock('@/lib/learning/actions', () => ({
  addCourseSupportTicketMessage: vi.fn(),
  resolveCourseSupportTicket: vi.fn(),
}));

describe('CourseTicketActionPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(addCourseSupportTicketMessage).mockResolvedValue({ success: true, data: null });
    vi.mocked(resolveCourseSupportTicket).mockResolvedValue({ success: true, data: null });
  });

  it('replies to a persisted support ticket', async () => {
    const user = userEvent.setup();
    render(<CourseTicketActionPanel courseId="course-1" ticketId="ticket-1" resolved={false} />);

    fireEvent.change(screen.getByLabelText('Reply'), { target: { value: 'Please retry after refreshing the lesson.' } });
    await user.click(screen.getByRole('button', { name: 'Send reply' }));

    await waitFor(() => expect(addCourseSupportTicketMessage).toHaveBeenCalledWith({
      courseId: 'course-1',
      ticketId: 'ticket-1',
      message: 'Please retry after refreshing the lesson.',
    }));
    expect(await screen.findByRole('status')).toHaveTextContent('Reply sent.');
  });

  it('resolves a ticket with a required resolution summary', async () => {
    const user = userEvent.setup();
    render(<CourseTicketActionPanel courseId="course-1" ticketId="ticket-1" resolved={false} />);

    await user.click(screen.getByRole('button', { name: 'Resolve ticket' }));
    fireEvent.change(screen.getByLabelText('Resolution summary'), { target: { value: 'Access entitlement was refreshed.' } });
    await user.click(screen.getByRole('button', { name: 'Confirm resolution' }));

    await waitFor(() => expect(resolveCourseSupportTicket).toHaveBeenCalledWith({
      courseId: 'course-1',
      ticketId: 'ticket-1',
      summary: 'Access entitlement was refreshed.',
    }));
  });
});
