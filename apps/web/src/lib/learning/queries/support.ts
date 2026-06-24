import { cache } from 'react';
import { resolveCourseId } from './course';
import { learningApiGet } from './http';

// =============================================================================
// COURSE SUPPORT QUERIES
// =============================================================================
// Support tickets and discussions for enrolled students.
// =============================================================================

/**
 * Support ticket status
 */
export type TicketStatus = 'open' | 'in-progress' | 'waiting-on-student' | 'resolved' | 'closed';
export type TicketPriority = 'low' | 'normal' | 'high' | 'urgent';
export type TicketCategory = 'technical' | 'content' | 'billing' | 'access' | 'feedback' | 'other';

/**
 * Support ticket
 */
export interface SupportTicket {
  id: string;
  courseId: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  subject: string;
  status: TicketStatus;
  priority: TicketPriority;
  category: TicketCategory;
  messageCount: number;
  lastMessageAt: string;
  assignedTo?: {
    id: string;
    name: string;
  };
  createdAt: string;
  updatedAt: string;
}

export interface CourseSupportTickets {
  tickets: SupportTicket[];
  total: number;
  openCount: number;
  inProgressCount: number;
  resolvedCount: number;
}

/**
 * Ticket message
 */
export interface TicketMessage {
  id: string;
  ticketId: string;
  authorId: string;
  authorName: string;
  authorRole: 'student' | 'instructor' | 'support';
  content: string;
  attachments: Array<{
    id: string;
    name: string;
    url: string;
    size: number;
    type: string;
  }>;
  createdAt: string;
}

export interface SupportTicketDetail extends SupportTicket {
  messages: TicketMessage[];
  relatedContent?: {
    id: string;
    type: string;
    title: string;
  };
}

/**
 * Discussion thread (forum)
 */
export interface DiscussionThread {
  id: string;
  courseId: string;
  contentItemId?: string;     // Optional link to specific content
  authorId: string;
  authorName: string;
  authorAvatar?: string;
  title: string;
  content: string;
  pinned: boolean;
  locked: boolean;
  replyCount: number;
  viewCount: number;
  lastReplyAt: string | null;
  tags: string[];
  createdAt: string;
  updatedAt: string;
}

export interface CourseDiscussions {
  threads: DiscussionThread[];
  total: number;
  pinnedCount: number;
}

/**
 * Discussion reply
 */
export interface DiscussionReply {
  id: string;
  threadId: string;
  parentId?: string;          // For nested replies
  authorId: string;
  authorName: string;
  authorAvatar?: string;
  authorRole: 'student' | 'instructor' | 'ta';
  content: string;
  upvotes: number;
  isAnswer: boolean;          // Marked as accepted answer
  createdAt: string;
  updatedAt: string;
}

export interface DiscussionThreadDetail extends DiscussionThread {
  replies: DiscussionReply[];
}

// =============================================================================
// FETCH FUNCTIONS
// =============================================================================

/**
 * Fetch course support tickets.
 * Cache: revalidate 30s (highly volatile)
 */
export const getCourseSupportTickets = cache(async (courseId: string): Promise<CourseSupportTickets> => {
  const discussions = await getCourseDiscussions(courseId);
  const tickets: SupportTicket[] = discussions.threads.map((thread) => ({
    id: thread.id,
    courseId: thread.courseId,
    studentId: thread.authorId,
    studentName: thread.authorName,
    studentEmail: '',
    subject: thread.title,
    status: thread.locked ? 'closed' : thread.replyCount > 0 ? 'in-progress' : 'open',
    priority: thread.pinned ? 'high' : 'normal',
    category: 'content',
    messageCount: thread.replyCount + 1,
    lastMessageAt: thread.lastReplyAt ?? thread.createdAt,
    createdAt: thread.createdAt,
    updatedAt: thread.updatedAt,
  }));

  return {
    tickets,
    total: tickets.length,
    openCount: tickets.filter((ticket) => ticket.status === 'open').length,
    inProgressCount: tickets.filter((ticket) => ticket.status === 'in-progress').length,
    resolvedCount: tickets.filter((ticket) => ticket.status === 'resolved' || ticket.status === 'closed').length,
  };
});

/**
 * Fetch single ticket detail with messages.
 * Cache: revalidate 30s
 */
export const getSupportTicket = cache(async (ticketId: string): Promise<SupportTicketDetail | null> => {
  const thread = await getDiscussionThread(ticketId);
  if (!thread) return null;

  return {
    id: thread.id,
    courseId: thread.courseId,
    studentId: thread.authorId,
    studentName: thread.authorName,
    studentEmail: '',
    subject: thread.title,
    status: thread.locked ? 'closed' : thread.replyCount > 0 ? 'in-progress' : 'open',
    priority: thread.pinned ? 'high' : 'normal',
    category: 'content',
    messageCount: thread.replies.length + 1,
    lastMessageAt: thread.lastReplyAt ?? thread.createdAt,
    createdAt: thread.createdAt,
    updatedAt: thread.updatedAt,
    messages: [
      {
        id: `${thread.id}-root`,
        ticketId: thread.id,
        authorId: thread.authorId,
        authorName: thread.authorName,
        authorRole: 'student',
        content: thread.content,
        attachments: [],
        createdAt: thread.createdAt,
      },
      ...thread.replies.map((reply) => ({
        id: reply.id,
        ticketId: thread.id,
        authorId: reply.authorId,
        authorName: reply.authorName,
        authorRole: reply.authorRole === 'ta' ? 'support' as const : reply.authorRole,
        content: reply.content,
        attachments: [],
        createdAt: reply.createdAt,
      })),
    ],
  };
});

interface DiscussionApiDto {
  id: string;
  courseId: string;
  contentId?: string | null;
  authorId: string;
  title: string;
  content: string;
  isPinned?: boolean;
  isResolved?: boolean;
  replyCount?: number;
  viewCount?: number;
  lastActivityAt?: string | null;
  createdAt?: string;
}

interface DiscussionReplyApiDto {
  id: string;
  discussionId: string;
  authorId: string;
  parentReplyId?: string | null;
  content: string;
  isAcceptedAnswer?: boolean;
  upvoteCount?: number;
  createdAt?: string;
}

function mapDiscussion(dto: DiscussionApiDto): DiscussionThread {
  const createdAt = dto.createdAt ?? new Date().toISOString();

  return {
    id: dto.id,
    courseId: dto.courseId,
    contentItemId: dto.contentId ?? undefined,
    authorId: dto.authorId,
    authorName: `Student ${dto.authorId.slice(0, 8)}`,
    title: dto.title,
    content: dto.content,
    pinned: dto.isPinned ?? false,
    locked: dto.isResolved ?? false,
    replyCount: dto.replyCount ?? 0,
    viewCount: dto.viewCount ?? 0,
    lastReplyAt: dto.lastActivityAt ?? null,
    tags: [],
    createdAt,
    updatedAt: dto.lastActivityAt ?? createdAt,
  };
}

/**
 * Fetch course discussions (conditional: hasDiscussions).
 * Cache: revalidate 60s
 */
export const getCourseDiscussions = cache(async (courseId: string): Promise<CourseDiscussions> => {
  const resolvedCourseId = await resolveCourseId(courseId);
  const discussions = await learningApiGet<DiscussionApiDto[]>(`/api/social/courses/${resolvedCourseId}/discussions?skip=0&take=100&pinnedFirst=true`, 60);
  const threads = (discussions ?? []).map(mapDiscussion);

  return { threads, total: threads.length, pinnedCount: threads.filter((thread) => thread.pinned).length };
});

/**
 * Fetch single discussion thread with replies.
 * Cache: revalidate 60s
 */
export const getDiscussionThread = cache(async (threadId: string): Promise<DiscussionThreadDetail | null> => {
  const [discussion, replies] = await Promise.all([
    learningApiGet<DiscussionApiDto>(`/api/social/discussions/${threadId}`, 60),
    learningApiGet<DiscussionReplyApiDto[]>(`/api/social/discussions/${threadId}/replies?skip=0&take=100`, 60),
  ]);

  if (!discussion) return null;

  const thread = mapDiscussion(discussion);
  return {
    ...thread,
    replies: (replies ?? []).map((reply) => {
      const createdAt = reply.createdAt ?? new Date().toISOString();

      return {
        id: reply.id,
        threadId: reply.discussionId,
        parentId: reply.parentReplyId ?? undefined,
        authorId: reply.authorId,
        authorName: `Member ${reply.authorId.slice(0, 8)}`,
        authorRole: 'student',
        content: reply.content,
        upvotes: reply.upvoteCount ?? 0,
        isAnswer: reply.isAcceptedAnswer ?? false,
        createdAt,
        updatedAt: createdAt,
      };
    }),
  };
});
