export interface NotificationUser {
  id: string;
  name: string;
  avatar?: string;
}

export type NotificationType = 'comment' | 'follow' | 'invite' | 'reminder' | 'task' | 'mention' | 'system' | 'course' | 'achievement' | 'social' | 'promotion';

export type NotificationPriority = 'low' | 'medium' | 'high';

export interface NotificationActionButtons {
  accept?: boolean;
  decline?: boolean;
  respond?: boolean;
  view?: boolean;
}

export interface NotificationMetadata {
  courseId?: string;
  userId?: string;
  achievementId?: string;
  groupId?: string;
  taskId?: string;
  projectName?: string;
  statusFrom?: string;
  statusTo?: string;
  amount?: string;
  icon?: string;
  [key: string]: unknown;
}

export interface Notification {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  user?: NotificationUser;
  timestamp: Date;
  isRead: boolean;
  isArchived: boolean;
  isStarred?: boolean;
  priority?: NotificationPriority;
  actionButtons?: NotificationActionButtons;
  actionUrl?: string;
  actionText?: string;
  metadata?: NotificationMetadata;
}

export interface NotificationResponse {
  notifications: Notification[];
  unreadCount: number;
  totalCount: number;
}

export interface NotificationFilters {
  type?: 'all' | 'unread' | 'read' | 'archived';
  category?: 'all' | 'following' | 'archive';
  notificationType?: NotificationType | 'all';
  priority?: NotificationPriority | 'all';
  limit?: number;
  offset?: number;
}

export interface NotificationActionState {
  success?: boolean;
  error?: string;
  message?: string;
}

export interface NotificationPreferences {
  emailNotifications: boolean;
  pushNotifications: boolean;
  inAppNotifications: boolean;
  soundEnabled: boolean;
  types: {
    [K in NotificationType]: boolean;
  };
}

// Reducer action types
export type NotificationAction =
  | { type: 'SET_NOTIFICATIONS'; payload: Notification[] }
  | { type: 'ADD_NOTIFICATION'; payload: Notification }
  | { type: 'MARK_AS_READ'; payload: string }
  | { type: 'MARK_ALL_AS_READ' }
  | { type: 'ARCHIVE_NOTIFICATION'; payload: string }
  | { type: 'TOGGLE_STAR'; payload: string }
  | { type: 'REMOVE_NOTIFICATION'; payload: string }
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SET_FILTERS'; payload: NotificationFilters }
  | { type: 'RESET_STATE' };

export interface NotificationState {
  notifications: Notification[];
  unreadCount: number;
  isLoading: boolean;
  error: string | null;
  filters: NotificationFilters;
  lastFetch: Date | null;
}
