import React from 'react';
import { FeedHeader } from './feed-header';
import { PostFiltersStatic } from './post-filters-static';
import { PinnedPosts } from './pinned-posts';
import { PostsList } from './posts-list';
import { fetchPinnedPosts, fetchPosts } from '@/lib/feed/api';
import type { FeedFilters, PostDto } from '@/lib/feed';

interface CommunityFeedServerProps {
  initialPosts?: PostDto[];
  initialPinnedPosts?: PostDto[];
  totalPosts?: number;
  searchParams?: Record<string, string | string[] | undefined>;
}

const POST_TYPES = [
  'general',
  'announcement',
  'user_registration',
  'user_signup',
  'project_completion',
  'achievement_unlocked',
  'milestone',
  'news',
  'discussion',
  'question',
  'showcase',
  'tutorial',
  'event',
];

export async function CommunityFeedServer({ searchParams = {} }: CommunityFeedServerProps) {
  // Parse search params to filters
  const filters: FeedFilters = {
    searchTerm: typeof searchParams.search === 'string' ? searchParams.search : undefined,
    postType: typeof searchParams.type === 'string' ? searchParams.type : undefined,
    orderBy: typeof searchParams.orderBy === 'string' ? searchParams.orderBy : 'createdAt',
    descending: searchParams.desc !== 'false',
  };

  // Fetch initial data server-side
  let posts: PostDto[] = [];
  let pinnedPosts: PostDto[] = [];
  let totalPosts = 0;
  const activeUsers = 1200;
  const engagementRate = 75.5;

  try {
    const [postsData, pinnedData] = await Promise.all([
      fetchPosts({
        ...filters,
        pageNumber: 1,
        pageSize: 20,
      }),
      fetchPinnedPosts(),
    ]);

    posts = postsData.posts || [];
    pinnedPosts = pinnedData.posts || [];
    totalPosts = postsData.totalCount || 0;
  } catch (error) {
    console.error('Error fetching feed data:', error);
    // Component will render with empty data and error boundary can handle it
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      <div className="max-w-4xl mx-auto px-6 py-8">
        {/* Header */}
        <FeedHeader totalPosts={totalPosts} activeUsers={activeUsers} engagementRate={engagementRate} />

        {/* Filters */}
        <PostFiltersStatic filters={filters} postTypes={POST_TYPES} />

        {/* Pinned Posts */}
        {pinnedPosts.length > 0 && (
          <PinnedPosts
            posts={pinnedPosts}
            onLike={() => {}} // No-op for server-side rendering
            onComment={() => {}} // No-op for server-side rendering
            onShare={() => {}} // No-op for server-side rendering
          />
        )}

        {/* Posts List */}
        <PostsList
          posts={posts}
          loading={false}
          hasMore={false}
          onLoadMore={() => {}} // No-op for server-side rendering
          onLike={() => {}} // No-op for server-side rendering
          onComment={() => {}} // No-op for server-side rendering
          onShare={() => {}} // No-op for server-side rendering
          emptyMessage="No posts match your current filters. Try adjusting your search criteria."
        />

        {/* Server-side note */}
        <div className="mt-8 text-center text-slate-400 text-sm">
          <p>
            Showing {posts.length} of {totalPosts} posts
          </p>
          {posts.length < totalPosts && <p className="mt-2">Load more posts by adjusting your filters or use the interactive version.</p>}
        </div>
      </div>
    </div>
  );
}
