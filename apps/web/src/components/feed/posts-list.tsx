import React from 'react';
import { PostCard } from './post-card';
import { LoadMore } from './load-more';
import type { PostDto } from '@/lib/feed';

interface PostsListProps {
  posts: PostDto[];
  loading?: boolean;
  hasMore?: boolean;
  onLoadMore?: () => void;
  onLike?: (postId: string) => void;
  onComment?: (postId: string) => void;
  onShare?: (postId: string) => void;
  emptyMessage?: string;
}

export function PostsList({
  posts,
  loading = false,
  hasMore = false,
  onLoadMore,
  onLike,
  onComment,
  onShare,
  emptyMessage = 'No posts found. Be the first to share something!',
}: PostsListProps) {
  if (!loading && posts.length === 0) {
    return (
      <div className="text-center py-12">
        <div className="w-24 h-24 mx-auto mb-6 bg-gradient-to-br from-slate-800 to-slate-700 rounded-full flex items-center justify-center">
          <span className="text-4xl">📝</span>
        </div>
        <h3 className="text-xl font-semibold text-white mb-2">No Posts Yet</h3>
        <p className="text-slate-400 max-w-md mx-auto">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Posts Grid */}
      <div className="space-y-6">
        {posts.map((post) => (
          <PostCard key={post.id} post={post} onLike={onLike} onComment={onComment} onShare={onShare} />
        ))}
      </div>

      {/* Loading State */}
      {loading && (
        <div className="flex justify-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
        </div>
      )}

      {/* Load More */}
      {!loading && hasMore && onLoadMore && <LoadMore onLoadMore={onLoadMore} />}
    </div>
  );
}
