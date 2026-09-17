import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  resolveCourseId: vi.fn(),
  getTickets: vi.fn(),
  getTicket: vi.fn(),
  getDiscussions: vi.fn(),
  getDiscussion: vi.fn(),
  getReplies: vi.fn(),
}));

vi.mock('react', () => ({ cache: (callback: unknown) => callback }));
vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('./course', () => ({ resolveCourseId: mocks.resolveCourseId }));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesSupportTicketsModule: class {
      getCoursesSupportTicketsForGetCoursesByCourseIdSupportTickets = mocks.getTickets;
      getCoursesSupportTicketsForGetCoursesByCourseIdSupportTicketsByTicketId = mocks.getTicket;
    },
    LearningExperienceSocialDiscussionsModule: class {
      getApiSocialCoursesDiscussions = mocks.getDiscussions;
      getApiSocialDiscussions = mocks.getDiscussion;
    },
    LearningExperienceSocialRepliesModule: class {
      getApiSocialDiscussionsReplies = mocks.getReplies;
    },
  },
}));

import {
  getCourseDiscussions,
  getCourseSupportTickets,
  getDiscussionThread,
  getSupportTicket,
} from './support';

describe('learning support query coverage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('API_URL', '');
    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('token');
    mocks.resolveCourseId.mockResolvedValue('course-1');
  });

  it('configures support clients with server, public, and default URLs', async () => {
    mocks.getTickets.mockResolvedValue({ ok: false, error: {} });

    vi.stubEnv('API_URL', 'https://internal.example');
    vi.stubEnv('NEXT_PUBLIC_API_URL', 'https://public.example');
    await getCourseSupportTickets('server');
    let options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe('https://internal.example');
    await expect(options.auth.getAccessToken()).resolves.toBe('token');

    vi.stubEnv('API_URL', '');
    await getCourseSupportTickets('public');
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe('https://public.example');

    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    await getCourseSupportTickets('default');
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe('http://localhost:8080');
  });

  it('maps ticket status, priority, category, counts, and assignments', async () => {
    mocks.getTickets.mockResolvedValue({
      ok: true,
      data: {
        totalCount: 8,
        items: [
          { id: '1', status: 'Open', priority: 'Low', category: 'Technical', assignedToUserId: 'agent-1', assignedToName: 'Agent' },
          { id: '2', status: 'InProgress', priority: 'HIGH', category: 'content', assignedToUserId: 'agent-2' },
          { id: '3', status: 'Resolved', priority: 'urgent', category: 'Billing', assignedToName: 'Unassigned' },
          { id: '4', status: 'Closed', priority: 'Normal', category: 'ACCESS' },
          { id: '5', status: 'Cancelled', priority: 'unknown', category: 'feedback' },
          { id: '6', status: null, priority: null, category: 'unknown' },
        ],
      },
    });

    const result = await getCourseSupportTickets('course-slug');

    expect(result).toMatchObject({ total: 8, openCount: 2, inProgressCount: 1, resolvedCount: 3 });
    expect(result.tickets.map((ticket) => ticket.status)).toEqual(['open', 'in-progress', 'resolved', 'closed', 'closed', 'open']);
    expect(result.tickets.map((ticket) => ticket.priority)).toEqual(['low', 'high', 'urgent', 'normal', 'normal', 'normal']);
    expect(result.tickets.map((ticket) => ticket.category)).toEqual(['technical', 'content', 'billing', 'access', 'feedback', 'other']);
    expect(result.tickets[0]?.assignedTo).toEqual({ id: 'agent-1', name: 'Agent' });
    expect(result.tickets.slice(1).every((ticket) => ticket.assignedTo === undefined)).toBe(true);
  });

  it('maps ticket defaults and uses the item count when no total is returned', async () => {
    mocks.getTickets.mockResolvedValue({ ok: true, data: { items: [{}] } });
    const result = await getCourseSupportTickets('course-slug');
    expect(result.total).toBe(1);
    expect(result.tickets[0]).toMatchObject({
      id: '', courseId: '', studentId: '', studentName: 'Student', studentEmail: '', subject: '',
      messageCount: 0, status: 'open', priority: 'normal', category: 'other',
    });
    expect(result.tickets[0]?.createdAt).toMatch(/^\d{4}-/);
    expect(result.tickets[0]?.lastMessageAt).toBe(result.tickets[0]?.createdAt);
  });

  it('returns empty ticket queues for API failure, missing response items, and null totals', async () => {
    mocks.getTickets
      .mockResolvedValueOnce({ ok: false, error: {} })
      .mockResolvedValueOnce({ ok: true, data: { items: null, totalCount: null } });
    await expect(getCourseSupportTickets('failed')).resolves.toEqual({ tickets: [], total: 0, openCount: 0, inProgressCount: 0, resolvedCount: 0 });
    await expect(getCourseSupportTickets('empty')).resolves.toEqual({ tickets: [], total: 0, openCount: 0, inProgressCount: 0, resolvedCount: 0 });
  });

  it('returns null for a missing ticket and maps every message author role', async () => {
    mocks.getTicket
      .mockResolvedValueOnce({ ok: false, error: {} })
      .mockResolvedValueOnce({
        ok: true,
        data: {
          id: 'ticket-1', openedAt: '2026-01-01T00:00:00.000Z', lastMessageAt: '2026-01-02T00:00:00.000Z',
          messages: [
            { id: 'm1', ticketId: 'ticket-1', authorUserId: 'student', authorName: 'Student', authorType: 'Customer', body: 'Help', createdAt: '2026-01-01T01:00:00.000Z' },
            { id: 'm2', authorType: 'Agent' },
            {},
          ],
        },
      });

    await expect(getSupportTicket('course-slug', 'missing')).resolves.toBeNull();
    const result = await getSupportTicket('course-slug', 'ticket-1');
    expect(result?.messages).toMatchObject([
      { id: 'm1', ticketId: 'ticket-1', authorId: 'student', authorName: 'Student', authorRole: 'student', content: 'Help', createdAt: '2026-01-01T01:00:00.000Z' },
      { id: 'm2', ticketId: 'ticket-1', authorId: '', authorName: 'Support', authorRole: 'instructor', content: '' },
      { id: '', ticketId: 'ticket-1', authorId: '', authorName: 'Support', authorRole: 'support', content: '' },
    ]);
  });

  it('defaults an absent ticket message list', async () => {
    mocks.getTicket.mockResolvedValue({ ok: true, data: {} });
    await expect(getSupportTicket('course-slug', 'ticket-1')).resolves.toMatchObject({ messages: [] });
  });

  it('maps discussions and reports pinned totals', async () => {
    mocks.getDiscussions.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'thread-1', courseId: 'course-1', contentId: 'lesson-1', authorId: 'abcdefgh-user',
          title: 'Question', content: 'How?', isPinned: true, isResolved: true, replyCount: 2,
          viewCount: 7, lastActivityAt: '2026-01-02T00:00:00.000Z', createdAt: '2026-01-01T00:00:00.000Z',
        },
        {},
      ],
    });

    const result = await getCourseDiscussions('course-slug');

    expect(mocks.getDiscussions).toHaveBeenCalledWith('course-1', { skip: 0, take: 100, pinnedFirst: true });
    expect(result).toMatchObject({ total: 2, pinnedCount: 1 });
    expect(result.threads[0]).toMatchObject({
      id: 'thread-1', contentItemId: 'lesson-1', authorName: 'Student abcdefgh', pinned: true,
      locked: true, replyCount: 2, viewCount: 7, lastReplyAt: '2026-01-02T00:00:00.000Z',
      updatedAt: '2026-01-02T00:00:00.000Z',
    });
    expect(result.threads[1]).toMatchObject({
      id: '', courseId: '', contentItemId: undefined, authorId: '', authorName: 'Student', title: '',
      content: '', pinned: false, locked: false, replyCount: 0, viewCount: 0, lastReplyAt: null,
    });
  });

  it('returns an empty discussion list when the API fails', async () => {
    mocks.getDiscussions.mockResolvedValue({ ok: false, error: {} });
    await expect(getCourseDiscussions('course-slug')).resolves.toEqual({ threads: [], total: 0, pinnedCount: 0 });
  });

  it('returns null for a missing discussion while still requesting replies', async () => {
    mocks.getDiscussion.mockResolvedValue({ ok: false, error: {} });
    mocks.getReplies.mockResolvedValue({ ok: true, data: [] });
    await expect(getDiscussionThread('missing')).resolves.toBeNull();
    expect(mocks.getReplies).toHaveBeenCalledWith('missing', { skip: 0, take: 100 });
  });

  it('maps discussion replies and their default values', async () => {
    mocks.getDiscussion.mockResolvedValue({ ok: true, data: { id: 'thread-1' } });
    mocks.getReplies.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'reply-1', discussionId: 'thread-1', parentReplyId: 'parent-1', authorId: 'abcdefgh-member',
          content: 'Answer', upvoteCount: 5, isAcceptedAnswer: true, createdAt: '2026-01-03T00:00:00.000Z',
        },
        {},
      ],
    });

    const result = await getDiscussionThread('thread-1');
    expect(result?.replies).toMatchObject([
      {
        id: 'reply-1', threadId: 'thread-1', parentId: 'parent-1', authorId: 'abcdefgh-member',
        authorName: 'Member abcdefgh', content: 'Answer', upvotes: 5, isAnswer: true,
        createdAt: '2026-01-03T00:00:00.000Z', updatedAt: '2026-01-03T00:00:00.000Z',
      },
      {
        id: '', threadId: 'thread-1', parentId: undefined, authorId: '', authorName: 'Member',
        content: '', upvotes: 0, isAnswer: false,
      },
    ]);
  });

  it('uses no replies when the replies endpoint fails', async () => {
    mocks.getDiscussion.mockResolvedValue({ ok: true, data: { id: 'thread-1' } });
    mocks.getReplies.mockResolvedValue({ ok: false, error: {} });
    await expect(getDiscussionThread('thread-1')).resolves.toMatchObject({ replies: [] });
  });
});
