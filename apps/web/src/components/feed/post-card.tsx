'use client';

import React from 'react';
import { formatDistanceToNow } from 'date-fns';
import { Heart, MessageCircle, Share2, Pin, User, Calendar } from 'lucide-react';
import { cn } from '@game-guild/ui/lib/utils';
import type { PostDto } from '@/lib/feed';

interface PostCardProps {
  post: PostDto;
  showActions?: boolean;
  className?: string;
  onLike?: (postId: string) => void;
  onComment?: (postId: string) => void;
  onShare?: (postId: string) => void;
}

export function PostCard({
  post,
  showActions = true,
  className,
  onLike,
  onComment,
  onShare,
}: PostCardProps) {
  const getPostTypeColor = (postType: string) => {
    switch (postType) {
      case 'announcement':
        return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      case 'achievement_unlocked':
        return 'bg-yellow-500/10 text-yellow-400 border-yellow-500/20';
      case 'milestone':
        return 'bg-purple-500/10 text-purple-400 border-purple-500/20';
      case 'user_registration':
        return 'bg-green-500/10 text-green-400 border-green-500/20';
      case 'project_completion':
        return 'bg-orange-500/10 text-orange-400 border-orange-500/20';
      case 'news':
        return 'bg-red-500/10 text-red-400 border-red-500/20';
      case 'discussion':
        return 'bg-indigo-500/10 text-indigo-400 border-indigo-500/20';
      case 'showcase':
        return 'bg-pink-500/10 text-pink-400 border-pink-500/20';
      default:
        return 'bg-slate-500/10 text-slate-400 border-slate-500/20';
    }
  };

  const formatPostType = (postType: string) => {
    return postType.replace('_', ' ').replace(/\b\w/g, (l) => l.toUpperCase());
  };

  // Safe guards for undefined values
  const safePost = {
    ...post,
    id: post.id || '',
    title: post.title || 'Untitled Post',
    authorName: post.authorName || 'Unknown Author',
    postType: post.postType || 'general',
    createdAt: post.createdAt || new Date().toISOString(),
    likesCount: post.likesCount || 0,
    commentsCount: post.commentsCount || 0,
    sharesCount: post.sharesCount || 0,
    contentReferences: post.contentReferences || [],
  };

  return (
    <article
      className={cn(
        'bg-gradient-to-br from-slate-900/50 to-slate-800/50',
        'backdrop-blur-sm rounded-lg shadow-lg hover:shadow-xl',
        'transition-all duration-300 hover:transform hover:scale-[1.02]',
        'border border-slate-700/50 hover:border-slate-600/50',
        'overflow-hidden',
        className,
      )}
    >
      {/* Header */}
      <div className="p-6 pb-4">
        <div className="flex items-start justify-between mb-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center">
              <User className="w-5 h-5 text-white" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="font-medium text-white">{safePost.authorName}</span>
                {post.isPinned && <Pin className="w-4 h-4 text-yellow-400" />}
              </div>
              <div className="flex items-center gap-2 text-sm text-slate-400">
                <Calendar className="w-3 h-3" />
                <time dateTime={safePost.createdAt}>
                  {formatDistanceToNow(new Date(safePost.createdAt), { addSuffix: true })}
                </time>
              </div>
            </div>
          </div>
          <div className={cn('px-3 py-1 rounded-full text-xs font-medium border', getPostTypeColor(safePost.postType))}>
            {formatPostType(safePost.postType)}
          </div>
        </div>

        {/* Title */}
        <h2 className="text-xl font-bold text-white mb-3 leading-tight">{safePost.title}</h2>

        {/* Description */}
        {post.description && (
          <p className="text-slate-300 leading-relaxed mb-4 line-clamp-3">{post.description}</p>
        )}

        {/* Rich Content Preview */}
        {post.richContent && (
          <div className="bg-slate-800/50 rounded-lg p-4 mb-4 border border-slate-700/30">
            <p className="text-sm text-slate-400 italic">Rich content available...</p>
          </div>
        )}

        {/* Content References */}
        {safePost.contentReferences.length > 0 && (
          <div className="mb-4">
            <div className="flex flex-wrap gap-2">
              {safePost.contentReferences.slice(0, 3).map((ref, index) => (
                <div
                  key={index}
                  className="bg-slate-800/30 border border-slate-600/30 rounded-lg px-3 py-2 text-sm"
                >
                  <span className="text-slate-400">{ref.referenceType}:</span>
                  <span className="text-white ml-1">{ref.resourceTitle}</span>
                </div>
              ))}
              {safePost.contentReferences.length > 3 && (
                <div className="bg-slate-800/30 border border-slate-600/30 rounded-lg px-3 py-2 text-sm text-slate-400">
                  +{safePost.contentReferences.length - 3} more
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Actions */}
      {showActions && (
        <div className="border-t border-slate-700/30 px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-6">
              <button
                onClick={() => onLike?.(safePost.id)}
                className="flex items-center gap-2 text-slate-400 hover:text-red-400 transition-colors group"
              >
                <Heart className="w-4 h-4 group-hover:scale-110 transition-transform" />
                <span className="text-sm">{safePost.likesCount}</span>
              </button>
              <button
                onClick={() => onComment?.(safePost.id)}
                className="flex items-center gap-2 text-slate-400 hover:text-blue-400 transition-colors group"
              >
                <MessageCircle className="w-4 h-4 group-hover:scale-110 transition-transform" />
                <span className="text-sm">{safePost.commentsCount}</span>
              </button>
              <button
                onClick={() => onShare?.(safePost.id)}
                className="flex items-center gap-2 text-slate-400 hover:text-green-400 transition-colors group"
              >
                <Share2 className="w-4 h-4 group-hover:scale-110 transition-transform" />
                <span className="text-sm">{safePost.sharesCount}</span>
              </button>
            </div>
            <div className="text-xs text-slate-500">
              {typeof post.status === 'string' && post.status === 'Published'
                ? '📅 Published'
                : `📝 ${post.status || 'Draft'}`}
            </div>
          </div>
        </div>
      )}
    </article>
  );
}
