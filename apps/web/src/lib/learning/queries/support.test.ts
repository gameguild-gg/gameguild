import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  resolveCourseId: vi.fn(),
  learningApiGet: vi.fn(),
}));

vi.mock('./course', () => ({ resolveCourseId: mocks.resolveCourseId }));
vi.mock('./http', () => ({ learningApiGet: mocks.learningApiGet }));

import { getCourseSupportTickets, getSupportTicket } from './support';

describe('course support ticket queries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.resolveCourseId.mockResolvedValue('course-1');
  });

  it('loads the persisted course ticket queue instead of deriving tickets from discussions', async () => {
    mocks.learningApiGet.mockResolvedValue({
      items: [{
        id: 'ticket-1',
        customerId: 'course-1',
        reporterUserId: 'user-1',
        reporterName: 'Ada Learner',
        reporterEmail: 'ada@example.com',
        subject: 'Cannot open lesson 3',
        category: 'access',
        status: 'InProgress',
        priority: 'High',
        openedAt: '2026-07-01T10:00:00.000Z',
        lastMessageAt: '2026-07-02T10:00:00.000Z',
        messageCount: 2,
        messages: [],
      }],
      total: 1,
      skip: 0,
      take: 100,
    });

    const result = await getCourseSupportTickets('course-slug');

    expect(mocks.learningApiGet).toHaveBeenCalledWith('/v1/courses/course-1/support/tickets?skip=0&take=100', 30);
    expect(result).toMatchObject({ total: 1, openCount: 0, inProgressCount: 1, resolvedCount: 0 });
    expect(result.tickets[0]).toMatchObject({
      id: 'ticket-1',
      courseId: 'course-1',
      studentId: 'user-1',
      studentName: 'Ada Learner',
      subject: 'Cannot open lesson 3',
      status: 'in-progress',
      priority: 'high',
    });
  });

  it('loads persisted ticket messages from the course-scoped endpoint', async () => {
    mocks.learningApiGet.mockResolvedValue({
      id: 'ticket-1',
      customerId: 'course-1',
      reporterUserId: 'user-1',
      reporterName: 'Ada Learner',
      reporterEmail: 'ada@example.com',
      subject: 'Cannot open lesson 3',
      category: 'access',
      status: 'Open',
      priority: 'Normal',
      openedAt: '2026-07-01T10:00:00.000Z',
      lastMessageAt: '2026-07-01T10:00:00.000Z',
      messageCount: 1,
      messages: [{
        id: 'message-1',
        ticketId: 'ticket-1',
        authorUserId: 'user-1',
        authorName: 'Ada Learner',
        authorType: 'Customer',
        body: 'The player stays blank.',
        createdAt: '2026-07-01T10:00:00.000Z',
      }],
    });

    const result = await getSupportTicket('course-slug', 'ticket-1');

    expect(mocks.learningApiGet).toHaveBeenCalledWith('/v1/courses/course-1/support/tickets/ticket-1', 30);
    expect(result?.messages).toEqual([expect.objectContaining({
      id: 'message-1',
      authorRole: 'student',
      content: 'The player stays blank.',
    })]);
  });
});
