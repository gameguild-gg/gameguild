import { cache } from 'react';

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
  void courseId;
  return { tickets: [], total: 0, openCount: 0, inProgressCount: 0, resolvedCount: 0 };
});

/**
 * Fetch single ticket detail with messages.
 * Cache: revalidate 30s
 */
export const getSupportTicket = cache(async (ticketId: string): Promise<SupportTicketDetail | null> => {
  void ticketId;
  return null;
});

/**
 * Fetch course discussions (conditional: hasDiscussions).
 * Cache: revalidate 60s
 */
export const getCourseDiscussions = cache(async (courseId: string): Promise<CourseDiscussions> => {
  void courseId;
  return { threads: [], total: 0, pinnedCount: 0 };
});

/**
 * Fetch single discussion thread with replies.
 * Cache: revalidate 60s
 */
export const getDiscussionThread = cache(async (threadId: string): Promise<DiscussionThreadDetail | null> => {
  void threadId;
  return null;
});
