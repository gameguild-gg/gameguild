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

interface SupportTicketApiDto {
  id: string;
  customerId: string;
  reporterUserId: string;
  reporterName: string;
  reporterEmail?: string | null;
  subject: string;
  category?: string | null;
  status: 'Open' | 'InProgress' | 'Resolved' | 'Closed' | 'Cancelled' | string;
  priority: 'Low' | 'Normal' | 'High' | 'Urgent' | string;
  assignedToUserId?: string | null;
  assignedToName?: string | null;
  openedAt: string;
  lastMessageAt?: string | null;
  messageCount: number;
  messages: SupportTicketMessageApiDto[];
}

interface SupportTicketMessageApiDto {
  id: string;
  ticketId: string;
  authorUserId: string;
  authorName: string;
  authorType: 'Customer' | 'Agent' | 'System' | string;
  body: string;
  createdAt: string;
}

interface SupportTicketPageApiDto {
  items: SupportTicketApiDto[];
  total?: number;
  totalCount?: number;
}

function mapTicketStatus(status: SupportTicketApiDto['status']): TicketStatus {
  if (status === 'InProgress') return 'in-progress';
  if (status === 'Resolved') return 'resolved';
  if (status === 'Closed' || status === 'Cancelled') return 'closed';
  return 'open';
}

function mapTicketPriority(priority: SupportTicketApiDto['priority']): TicketPriority {
  const normalized = priority.toLowerCase();
  return normalized === 'low' || normalized === 'high' || normalized === 'urgent' ? normalized : 'normal';
}

function mapTicketCategory(category: string | null | undefined): TicketCategory {
  const normalized = category?.toLowerCase();
  return normalized === 'technical' || normalized === 'content' || normalized === 'billing' || normalized === 'access' || normalized === 'feedback'
    ? normalized
    : 'other';
}

function mapSupportTicket(dto: SupportTicketApiDto): SupportTicket {
  const lastMessageAt = dto.lastMessageAt ?? dto.openedAt;
  return {
    id: dto.id,
    courseId: dto.customerId,
    studentId: dto.reporterUserId,
    studentName: dto.reporterName,
    studentEmail: dto.reporterEmail ?? '',
    subject: dto.subject,
    status: mapTicketStatus(dto.status),
    priority: mapTicketPriority(dto.priority),
    category: mapTicketCategory(dto.category),
    messageCount: dto.messageCount,
    lastMessageAt,
    assignedTo: dto.assignedToUserId && dto.assignedToName
      ? { id: dto.assignedToUserId, name: dto.assignedToName }
      : undefined,
    createdAt: dto.openedAt,
    updatedAt: lastMessageAt,
  };
}

// =============================================================================
// FETCH FUNCTIONS
// =============================================================================

/**
 * Fetch course support tickets.
 * Cache: revalidate 30s (highly volatile)
 */
export const getCourseSupportTickets = cache(async (courseId: string): Promise<CourseSupportTickets> => {
  const resolvedCourseId = await resolveCourseId(courseId);
  const response = await learningApiGet<SupportTicketPageApiDto>(
    `/v1/courses/${resolvedCourseId}/support/tickets?skip=0&take=100`,
    30,
  );
  const tickets = (response?.items ?? []).map(mapSupportTicket);

  return {
    tickets,
    total: response?.totalCount ?? response?.total ?? tickets.length,
    openCount: tickets.filter((ticket) => ticket.status === 'open').length,
    inProgressCount: tickets.filter((ticket) => ticket.status === 'in-progress').length,
    resolvedCount: tickets.filter((ticket) => ticket.status === 'resolved' || ticket.status === 'closed').length,
  };
});

/**
 * Fetch single ticket detail with messages.
 * Cache: revalidate 30s
 */
export const getSupportTicket = cache(async (courseId: string, ticketId: string): Promise<SupportTicketDetail | null> => {
  const resolvedCourseId = await resolveCourseId(courseId);
  const dto = await learningApiGet<SupportTicketApiDto>(
    `/v1/courses/${resolvedCourseId}/support/tickets/${ticketId}`,
    30,
  );
  if (!dto) return null;

  return {
    ...mapSupportTicket(dto),
    messages: dto.messages.map((message) => ({
      id: message.id,
      ticketId: message.ticketId,
      authorId: message.authorUserId,
      authorName: message.authorName,
      authorRole: message.authorType === 'Customer' ? 'student' : message.authorType === 'Agent' ? 'instructor' : 'support',
      content: message.body,
      attachments: [],
      createdAt: message.createdAt,
    })),
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
