import React, { Suspense } from 'react';
import { notFound } from 'next/navigation';
import { FeedHeader } from './feed-header';
import { PostFilters } from './post-filters';
import { PostsList } from './posts-list';
import { CreatePostForm } from './create-post-form';
import { fetchPostsAction } from '@/lib/feed/server-actions';
import type { FeedFilters } from '@/lib/feed';

interface CommunityFeedServerProps {
  searchParams?: {
    postType?: string;
    userId?: string;
    isPinned?: string;
    searchTerm?: string;
    orderBy?: string;
    descending?: string;
    page?: string;
  };
}

const POST_TYPES = ['general', 'announcement', 'user_registration', 'user_signup', 'project_completion', 'achievement_unlocked', 'milestone', 'news', 'discussion', 'question', 'showcase', 'tutorial', 'event'];

export async function CommunityFeedServer({ searchParams }: CommunityFeedServerProps) {
  // Parse search parameters
  const filters: FeedFilters = {
    postType: searchParams?.postType,
    userId: searchParams?.userId,
    isPinned: searchParams?.isPinned === 'true',
    searchTerm: searchParams?.searchTerm,
    orderBy: searchParams?.orderBy || 'createdAt',
    descending: searchParams?.descending !== 'false',
  };

  const page = parseInt(searchParams?.page || '1', 10);
  const pageSize = 20;

  // Fetch posts server-side
  const result = await fetchPostsAction(filters, page, pageSize);

  if (!result.success || !result.data) {
    notFound();
  }

  const { data: postsPage } = result;

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 text-white">
      <div className="max-w-6xl mx-auto px-6 py-8">
        {/* Feed Header with Stats */}
        <FeedHeader totalPosts={postsPage.totalCount || 0} activeUsers={1200} engagementRate={75.5} />

        {/* Create Post Form */}
        <div className="mb-8">
          <Suspense fallback={<div className="h-24 bg-slate-800/50 rounded-xl animate-pulse" />}>
            <CreatePostForm />
          </Suspense>
        </div>

        {/* Filters */}
        <div className="mb-8">
          <PostFilters filters={filters} onFiltersChange={() => {}} postTypes={POST_TYPES} />
        </div>

        {/* Posts List */}
        <div className="space-y-6">
          <PostsList
            posts={postsPage.posts || []}
            loading={false}
            hasMore={postsPage.hasNextPage || false}
            onLoadMore={() => {}}
            onLike={() => {}}
            onComment={() => {}}
            onShare={() => {}}
            emptyMessage="No posts found. Try adjusting your filters."
          />
        </div>

        {/* Pagination info */}
        <div className="mt-8 text-center text-slate-400 text-sm">
          <p>
            Showing {postsPage.posts?.length || 0} of {postsPage.totalCount || 0} posts (Page {page})
          </p>
        </div>
      </div>
    </div>
  );
}
