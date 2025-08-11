export type { PostDto, PostsPageDto, GetApiPostsData, PostApiPostsData } from '@/lib/api/generated/types.gen';

export type FeedFilters = {
  postType?: string;
  userId?: string;
  isPinned?: boolean;
  searchTerm?: string;
  orderBy?: string;
  descending?: boolean;
};
