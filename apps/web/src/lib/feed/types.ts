// Re-export the generated API types for better organization
export type {
  PostDto,
  PostsPageDto,
  CreatePostDto,
  ContentReferenceDto,
} from '@/lib/api/generated/types.gen';

export interface FeedFilters {
  postType?: string;
  userId?: string;
  isPinned?: boolean;
  searchTerm?: string;
  orderBy?: string;
  descending?: boolean;
}

export interface PostInteraction {
  postId: string;
  userId: string;
  action: 'like' | 'unlike' | 'share' | 'pin' | 'unpin';
}

export type PostType =
  | 'general'
  | 'announcement'
  | 'user_registration'
  | 'user_signup'
  | 'project_completion'
  | 'achievement_unlocked'
  | 'milestone'
  | 'news'
  | 'discussion'
  | 'question'
  | 'showcase'
  | 'tutorial'
  | 'event';
