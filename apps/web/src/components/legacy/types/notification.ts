export interface NotificationUser {
  id: string;
  name: string;
  avatar?: string;
}

export interface Notification {
  id: string;
  type: 'comment' | 'follow' | 'invite' | 'reminder' | 'task' | 'mention' | 'system';
  title: string;
  message: string;
  user?: NotificationUser;
  projectName?: string;
  taskId?: string;
  timestamp: Date;
  isRead: boolean;
  isArchived: boolean;
  actionButtons?: {
    accept?: boolean;
    decline?: boolean;
    respond?: boolean;
    view?: boolean;
  };
  metadata?: Record<string, unknown>;
}

export interface NotificationResponse {
  notifications: Notification[];
  unreadCount: number;
  totalCount: number;
}

export interface NotificationFilters {
  type?: 'all' | 'unread' | 'read' | 'archived';
  category?: 'all' | 'following' | 'archive';
  limit?: number;
  offset?: number;
}

export interface NotificationActionState {
  success?: boolean;
  error?: string;
  message?: string;
}
