import React from 'react';
import { Pin } from 'lucide-react';
import { PostCard } from './post-card';
import type { PostDto } from '@/lib/feed';

interface PinnedPostsProps {
  posts: PostDto[];
  onLike?: (postId: string) => void;
  onComment?: (postId: string) => void;
  onShare?: (postId: string) => void;
}

export function PinnedPosts({ posts, onLike, onComment, onShare }: PinnedPostsProps) {
  if (posts.length === 0) return null;

  return (
    <div className="mb-8">
      <div className="flex items-center gap-2 mb-4">
        <Pin className="w-5 h-5 text-yellow-400" />
        <h2 className="text-xl font-bold text-white">Pinned Posts</h2>
      </div>
      <div className="space-y-4">
        {posts.map((post) => (
          <PostCard key={post.id} post={post} onLike={onLike} onComment={onComment} onShare={onShare} className="border-yellow-500/20 bg-gradient-to-br from-yellow-500/5 to-amber-500/5" />
        ))}
      </div>
    </div>
  );
}
