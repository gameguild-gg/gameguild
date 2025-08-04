'use client';

import { cn } from '@/lib/utils';
import { formatDistanceToNow } from 'date-fns';
import { Bookmark, Heart, MessageCircle, MoreHorizontal, Share2, User, Verified } from 'lucide-react';
import Image from 'next/image';
import { useCallback, useState } from 'react';
import './feed.css';

interface SocialPost {
  id: string;
  user: {
    id: string;
    username: string;
    displayName: string;
    avatar: string;
    isVerified: boolean;
    followerCount: number;
  };
  content: {
    text?: string;
    media?: {
      type: 'image' | 'video' | 'gif';
      url: string;
      thumbnail?: string;
      aspectRatio?: number;
    }[];
    hashtags?: string[];
    mentions?: string[];
  };
  interactions: {
    likes: number;
    comments: number;
    shares: number;
    saves: number;
    isLiked: boolean;
    isSaved: boolean;
  };
  createdAt: string;
  location?: string;
  type: 'post' | 'story' | 'reel' | 'video';
}

interface SocialMediaFeedProps {
  className?: string;
}

// Mock data for demo - in real app this would come from API
const mockPosts: SocialPost[] = [
  {
    id: '1',
    user: {
      id: '1',
      username: 'gamedev_pro',
      displayName: 'Alex GameDev',
      avatar: 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150&h=150&fit=crop&crop=face',
      isVerified: true,
      followerCount: 12500,
    },
    content: {
      text: 'Just finished implementing a new particle system for our indie game! 🎮✨ The explosions look incredible now. What do you think? #gamedev #indiegame #particles',
      media: [
        {
          type: 'video',
          url: 'https://sample-videos.com/zip/10/mp4/SampleVideo_1280x720_1mb.mp4',
          thumbnail: 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=800&h=600&fit=crop',
          aspectRatio: 16 / 9,
        },
      ],
      hashtags: ['gamedev', 'indiegame', 'particles'],
      mentions: ['@unity', '@unrealengine'],
    },
    interactions: {
      likes: 245,
      comments: 18,
      shares: 12,
      saves: 34,
      isLiked: false,
      isSaved: false,
    },
    createdAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
    type: 'video',
  },
  {
    id: '2',
    user: {
      id: '2',
      username: 'pixel_artist',
      displayName: 'Maya Pixels',
      avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612d5c8?w=150&h=150&fit=crop&crop=face',
      isVerified: false,
      followerCount: 8900,
    },
    content: {
      text: "Working on character designs for our upcoming RPG! Here's the main protagonist. Feedback welcome! 🎨",
      media: [
        {
          type: 'image',
          url: 'https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=800&h=800&fit=crop',
          aspectRatio: 1,
        },
      ],
      hashtags: ['pixelart', 'gamedev', 'rpg', 'characterdesign'],
    },
    interactions: {
      likes: 189,
      comments: 25,
      shares: 8,
      saves: 56,
      isLiked: true,
      isSaved: false,
    },
    createdAt: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
    type: 'post',
  },
  {
    id: '3',
    user: {
      id: '3',
      username: 'sound_designer',
      displayName: 'Chris Audio',
      avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&h=150&fit=crop&crop=face',
      isVerified: true,
      followerCount: 15200,
    },
    content: {
      text: 'Just dropped a new sound pack for horror games! 👻🔊 Includes ambient sounds, jump scares, and atmospheric music. Link in bio!',
      hashtags: ['gameaudio', 'horror', 'sounddesign', 'indie'],
    },
    interactions: {
      likes: 156,
      comments: 12,
      shares: 23,
      saves: 78,
      isLiked: false,
      isSaved: true,
    },
    createdAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
    type: 'post',
  },
  {
    id: '4',
    user: {
      id: '4',
      username: 'unity_tips',
      displayName: 'Unity Tips & Tricks',
      avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=150&fit=crop&crop=face',
      isVerified: true,
      followerCount: 45600,
    },
    content: {
      text: "🚀 Performance Tip: Use object pooling for frequently spawned objects like bullets or enemies. Here's a quick implementation:",
      media: [
        {
          type: 'image',
          url: 'https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=800&h=600&fit=crop',
          aspectRatio: 4 / 3,
        },
      ],
      hashtags: ['unity', 'gamedev', 'performance', 'tips'],
    },
    interactions: {
      likes: 892,
      comments: 67,
      shares: 234,
      saves: 456,
      isLiked: true,
      isSaved: true,
    },
    createdAt: new Date(Date.now() - 12 * 60 * 60 * 1000).toISOString(),
    type: 'post',
  },
];

export function SocialMediaFeed({ className }: SocialMediaFeedProps) {
  const [posts, setPosts] = useState<SocialPost[]>(mockPosts);

  const handleLike = useCallback((postId: string) => {
    setPosts((prev) =>
      prev.map((post) => {
        if (post.id === postId) {
          return {
            ...post,
            interactions: {
              ...post.interactions,
              isLiked: !post.interactions.isLiked,
              likes: post.interactions.isLiked ? post.interactions.likes - 1 : post.interactions.likes + 1,
            },
          };
        }
        return post;
      }),
    );
  }, []);

  const handleSave = useCallback((postId: string) => {
    setPosts((prev) =>
      prev.map((post) => {
        if (post.id === postId) {
          return {
            ...post,
            interactions: {
              ...post.interactions,
              isSaved: !post.interactions.isSaved,
              saves: post.interactions.isSaved ? post.interactions.saves - 1 : post.interactions.saves + 1,
            },
          };
        }
        return post;
      }),
    );
  }, []);

  const formatNumber = (num: number) => {
    if (num >= 1000000) return `${(num / 1000000).toFixed(1)}M`;
    if (num >= 1000) return `${(num / 1000).toFixed(1)}K`;
    return num.toString();
  };

  return (
    <div className={cn('min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900', className)}>
      <div className="max-w-2xl mx-auto px-4 py-6">
        {/* Stories Section */}
        <div className="mb-8">
          <div className="flex gap-4 overflow-x-auto pb-2 scrollbar-hide">
            {/* Your Story */}
            <div className="flex-shrink-0 flex flex-col items-center gap-2">
              <div className="relative">
                <div className="w-16 h-16 bg-gradient-to-br from-purple-500 to-pink-500 rounded-full p-0.5">
                  <div className="w-full h-full bg-slate-800 rounded-full flex items-center justify-center">
                    <User className="w-6 h-6 text-white" />
                  </div>
                </div>
                <div className="absolute -bottom-1 -right-1 w-6 h-6 bg-blue-500 rounded-full flex items-center justify-center text-white text-xs font-bold">+</div>
              </div>
              <span className="text-xs text-slate-400 text-center">Your Story</span>
            </div>

            {/* Other Stories */}
            {mockPosts.slice(0, 6).map((post) => (
              <div key={`story-${post.id}`} className="flex-shrink-0 flex flex-col items-center gap-2">
                <div className="story-ring w-16 h-16 rounded-full p-0.5">
                  <Image src={post.user.avatar} alt={post.user.displayName} width={64} height={64} className="w-full h-full rounded-full object-cover" />
                </div>
                <span className="text-xs text-slate-400 text-center max-w-[60px] truncate">{post.user.username}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Feed Posts */}
        <div className="space-y-6">
          {posts.map((post) => (
            <article key={post.id} className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-xl shadow-lg border border-slate-700/50 overflow-hidden">
              {/* Post Header */}
              <div className="p-4 pb-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Image src={post.user.avatar} alt={post.user.displayName} width={40} height={40} className="w-10 h-10 rounded-full object-cover" />
                    <div>
                      <div className="flex items-center gap-1">
                        <span className="font-semibold text-white text-sm">{post.user.displayName}</span>
                        {post.user.isVerified && <Verified className="w-4 h-4 text-blue-400 fill-current" />}
                      </div>
                      <div className="flex items-center gap-1 text-xs text-slate-400">
                        <span>@{post.user.username}</span>
                        <span>•</span>
                        <time>{formatDistanceToNow(new Date(post.createdAt), { addSuffix: true })}</time>
                      </div>
                    </div>
                  </div>
                  <button className="p-2 hover:bg-slate-700/50 rounded-full transition-colors">
                    <MoreHorizontal className="w-5 h-5 text-slate-400" />
                  </button>
                </div>
              </div>

              {/* Post Content */}
              {post.content.text && (
                <div className="px-4 pb-3">
                  <p className="text-white leading-relaxed">
                    {post.content.text.split(/(\s+)/).map((word, index) => {
                      if (word.startsWith('#')) {
                        return (
                          <span key={index} className="text-blue-400 hover:underline cursor-pointer">
                            {word}
                          </span>
                        );
                      }
                      if (word.startsWith('@')) {
                        return (
                          <span key={index} className="text-purple-400 hover:underline cursor-pointer">
                            {word}
                          </span>
                        );
                      }
                      return <span key={index}>{word}</span>;
                    })}
                  </p>
                </div>
              )}

              {/* Media */}
              {post.content.media && post.content.media.length > 0 && (
                <div className="relative">
                  {post.content.media[0].type === 'video' ? (
                    <div className="relative group">
                      <video className="w-full h-auto max-h-[600px] object-cover" poster={post.content.media[0].thumbnail} controls preload="metadata">
                        <source src={post.content.media[0].url} type="video/mp4" />
                      </video>
                      <div className="absolute inset-0 bg-black/20 group-hover:bg-black/10 transition-colors pointer-events-none" />
                    </div>
                  ) : (
                    <Image src={post.content.media[0].url} alt="Post content" width={800} height={600} className="w-full h-auto max-h-[600px] object-cover" />
                  )}
                </div>
              )}

              {/* Post Actions */}
              <div className="p-4">
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-4">
                    <button onClick={() => handleLike(post.id)} className={cn('flex items-center gap-2 transition-colors', post.interactions.isLiked ? 'text-red-500' : 'text-slate-400 hover:text-red-400')}>
                      <Heart className={cn('w-6 h-6 transition-transform', post.interactions.isLiked && 'fill-current like-animation')} />
                      <span className="text-sm font-medium">{formatNumber(post.interactions.likes)}</span>
                    </button>
                    <button className="flex items-center gap-2 text-slate-400 hover:text-blue-400 transition-colors">
                      <MessageCircle className="w-6 h-6" />
                      <span className="text-sm font-medium">{formatNumber(post.interactions.comments)}</span>
                    </button>
                    <button className="flex items-center gap-2 text-slate-400 hover:text-green-400 transition-colors">
                      <Share2 className="w-6 h-6" />
                      <span className="text-sm font-medium">{formatNumber(post.interactions.shares)}</span>
                    </button>
                  </div>
                  <button onClick={() => handleSave(post.id)} className={cn('transition-colors', post.interactions.isSaved ? 'text-yellow-500' : 'text-slate-400 hover:text-yellow-400')}>
                    <Bookmark className={cn('w-6 h-6', post.interactions.isSaved && 'fill-current')} />
                  </button>
                </div>

                {/* Engagement Summary */}
                <div className="text-sm text-slate-400">
                  <span className="font-medium text-white">{formatNumber(post.interactions.likes)}</span> likes
                  {post.interactions.comments > 0 && (
                    <>
                      <span className="mx-2">•</span>
                      <span className="font-medium text-white">{formatNumber(post.interactions.comments)}</span> comments
                    </>
                  )}
                </div>
              </div>
            </article>
          ))}
        </div>

        {/* Load More */}
        <div className="flex justify-center py-8">
          <button
            className="bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 text-white px-8 py-3 rounded-full font-medium transition-all transform hover:scale-105"
            onClick={() => {
              // In real app, load more posts from API
              console.log('Load more posts');
            }}
          >
            Load More Posts
          </button>
        </div>
      </div>
    </div>
  );
}
