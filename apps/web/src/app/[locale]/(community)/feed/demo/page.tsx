'use client';

import { useEffect } from 'react';
import { 
  CommunityFeed, 
  PostCard, 
  FeedHeader, 
  PostFilters, 
  PinnedPosts 
} from '@/components/feed';
import type { PostDto, FeedFilters } from '@/lib/feed';

// Mock data for demonstration
const mockPost: PostDto = {
  id: 'demo-post-1',
  title: 'Welcome to Game Guild Community!',
  description: 'This is a demonstration of our new community feed system. You can see different types of posts, interact with them, and filter content based on your preferences.',
  postType: 'announcement',
  authorId: 'user-1',
  authorName: 'gamemaster',
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  isPinned: false,
  likesCount: 42,
  commentsCount: 15,
  sharesCount: 8,
  contentReferences: [
    {
      resourceId: 'ref-1',
      resourceType: 'image',
      resourceTitle: 'Community Feed Preview',
      referenceType: 'attachment',
    }
  ],
};

const mockPinnedPost: PostDto = {
  ...mockPost,
  id: 'demo-pinned-1',
  title: '🎉 Important Announcement: New Features Released!',
  postType: 'announcement',
  isPinned: true,
  likesCount: 156,
  commentsCount: 43,
  sharesCount: 22,
};

const mockFilters: FeedFilters = {
  orderBy: 'createdAt',
  descending: true,
};

const POST_TYPES = [
  'general',
  'announcement',
  'discussion',
  'question',
  'showcase',
  'tutorial',
  'event',
];

export default function FeedDemoPage() {
  const mockStats = {
    totalPosts: 1247,
    activeUsers: 856,
    engagementRate: 73.2,
  };

  useEffect(() => {
    document.title = 'Feed Components Demo | Game Guild';
  }, []);

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      <div className="max-w-6xl mx-auto px-6 py-8">
        {/* Page Header */}
        <div className="mb-8 text-center">
          <h1 className="text-4xl font-bold text-white mb-4">
            Community Feed Components
          </h1>
          <p className="text-slate-300 text-lg max-w-2xl mx-auto">
            A comprehensive demonstration of all feed components, showcasing the 
            composition patterns and design system used throughout Game Guild.
          </p>
        </div>

        {/* Individual Component Demos */}
        <div className="space-y-12">
          {/* Feed Header Demo */}
          <section>
            <h2 className="text-2xl font-semibold text-white mb-4">Feed Header</h2>
            <p className="text-slate-400 mb-6">
              Displays key community statistics and metrics.
            </p>
            <FeedHeader
              totalPosts={mockStats.totalPosts}
              activeUsers={mockStats.activeUsers}
              engagementRate={mockStats.engagementRate}
            />
          </section>

          {/* Post Filters Demo */}
          <section>
            <h2 className="text-2xl font-semibold text-white mb-4">Post Filters</h2>
            <p className="text-slate-400 mb-6">
              Interactive filters for searching and sorting posts. (Demo version - non-functional)
            </p>
            <PostFilters
              filters={mockFilters}
              postTypes={POST_TYPES}
              onFiltersChange={() => {}} // Demo only
            />
          </section>

          {/* Pinned Posts Demo */}
          <section>
            <h2 className="text-2xl font-semibold text-white mb-4">Pinned Posts</h2>
            <p className="text-slate-400 mb-6">
              Special display for important or highlighted posts.
            </p>
            <PinnedPosts
              posts={[mockPinnedPost]}
              onLike={() => {}} // Demo only
              onComment={() => {}} // Demo only
              onShare={() => {}} // Demo only
            />
          </section>

          {/* Post Card Demo */}
          <section>
            <h2 className="text-2xl font-semibold text-white mb-4">Post Card</h2>
            <p className="text-slate-400 mb-6">
              Individual post display with full interaction capabilities.
            </p>
            <div className="grid gap-6">
              <PostCard
                post={mockPost}
                onLike={() => {}} // Demo only
                onComment={() => {}} // Demo only
                onShare={() => {}} // Demo only
              />
              
              {/* Showcase different post types */}
              <PostCard
                post={{
                  ...mockPost,
                  id: 'demo-discussion',
                  title: 'What\'s your favorite game mechanic?',
                  description: 'I\'ve been thinking about different game mechanics and how they create engaging experiences. What are some of your favorites and why?',
                  postType: 'discussion',
                  likesCount: 23,
                  commentsCount: 31,
                  sharesCount: 4,
                }}
                onLike={() => {}}
                onComment={() => {}}
                onShare={() => {}}
              />

              <PostCard
                post={{
                  ...mockPost,
                  id: 'demo-showcase',
                  title: 'My latest game project: Pixel Adventure',
                  description: 'Just finished working on this retro-style platformer! It features hand-crafted pixel art, chiptune music, and classic gameplay mechanics.',
                  postType: 'showcase',
                  likesCount: 89,
                  commentsCount: 12,
                  sharesCount: 18,
                }}
                onLike={() => {}}
                onComment={() => {}}
                onShare={() => {}}
              />
            </div>
          </section>

          {/* Full Community Feed Demo */}
          <section>
            <h2 className="text-2xl font-semibold text-white mb-4">Complete Community Feed</h2>
            <p className="text-slate-400 mb-6">
              The full interactive community feed with all components working together.
            </p>
            <div className="border-2 border-slate-700 rounded-xl overflow-hidden">
              <CommunityFeed />
            </div>
          </section>
        </div>

        {/* Usage Information */}
        <div className="mt-16 bg-slate-800/50 rounded-xl p-8">
          <h2 className="text-2xl font-semibold text-white mb-4">Usage & Features</h2>
          <div className="grid md:grid-cols-2 gap-8">
            <div>
              <h3 className="text-lg font-medium text-blue-400 mb-4">Component Features</h3>
              <div className="space-y-3">
                <div className="flex items-center space-x-3 p-2 rounded-lg bg-slate-700/30 hover:bg-slate-700/50 transition-all duration-200">
                  <span className="text-green-400 text-lg">✓</span>
                  <span className="text-slate-300">Server-side rendering support</span>
                </div>
                <div className="flex items-center space-x-3 p-2 rounded-lg bg-slate-700/30 hover:bg-slate-700/50 transition-all duration-200">
                  <span className="text-green-400 text-lg">✓</span>
                  <span className="text-slate-300">Responsive design with Tailwind CSS</span>
                </div>
                <div className="flex items-center space-x-3 p-2 rounded-lg bg-slate-700/30 hover:bg-slate-700/50 transition-all duration-200">
                  <span className="text-green-400 text-lg">✓</span>
                  <span className="text-slate-300">Type-safe with TypeScript</span>
                </div>
                <div className="flex items-center space-x-3 p-2 rounded-lg bg-slate-700/30 hover:bg-slate-700/50 transition-all duration-200">
                  <span className="text-green-400 text-lg">✓</span>
                  <span className="text-slate-300">Composition pattern architecture</span>
                </div>
                <div className="flex items-center space-x-3 p-2 rounded-lg bg-slate-700/30 hover:bg-slate-700/50 transition-all duration-200">
                  <span className="text-green-400 text-lg">✓</span>
                  <span className="text-slate-300">Reusable components</span>
                </div>
                <div className="flex items-center space-x-3 p-2 rounded-lg bg-slate-700/30 hover:bg-slate-700/50 transition-all duration-200">
                  <span className="text-green-400 text-lg">✓</span>
                  <span className="text-slate-300">Accessibility best practices</span>
                </div>
                <div className="flex items-center space-x-3 p-2 rounded-lg bg-slate-700/30 hover:bg-slate-700/50 transition-all duration-200">
                  <span className="text-green-400 text-lg">✓</span>
                  <span className="text-slate-300">Loading states and error handling</span>
                </div>
                <div className="flex items-center space-x-3 p-2 rounded-lg bg-slate-700/30 hover:bg-slate-700/50 transition-all duration-200">
                  <span className="text-green-400 text-lg">✓</span>
                  <span className="text-slate-300">Infinite scroll pagination</span>
                </div>
              </div>
            </div>
            <div>
              <h3 className="text-lg font-medium text-green-400 mb-4">Post Types</h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {/* Announcements - Premium Card Style */}
                <div className="relative group overflow-hidden rounded-xl bg-gradient-to-br from-orange-500/20 via-red-500/15 to-pink-500/10 border-2 border-orange-500/30 p-4 hover:border-orange-400/50 transition-all duration-300 hover:scale-105 hover:shadow-lg hover:shadow-orange-500/20">
                  <div className="absolute inset-0 bg-gradient-to-br from-orange-500/5 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
                  <div className="relative flex items-center space-x-4">
                    <div className="p-2 rounded-lg bg-orange-500/20 group-hover:bg-orange-500/30 transition-colors duration-200">
                      <span className="text-3xl">📢</span>
                    </div>
                    <div>
                      <h4 className="text-orange-200 font-bold text-lg">Announcements</h4>
                      <p className="text-orange-300/70 text-sm">Official news & updates</p>
                    </div>
                  </div>
                </div>

                {/* Discussions - Chat Bubble Style */}
                <div className="relative group rounded-xl bg-gradient-to-r from-blue-500/15 to-cyan-500/10 border border-blue-500/30 p-4 hover:border-blue-400/50 transition-all duration-300 hover:translate-y-[-2px] hover:shadow-lg hover:shadow-blue-500/20">
                  <div className="flex items-center space-x-4">
                    <div className="relative">
                      <div className="p-2 rounded-full bg-blue-500/20 group-hover:bg-blue-500/30 transition-colors duration-200">
                        <span className="text-2xl">💬</span>
                      </div>
                      <div className="absolute -top-1 -right-1 w-3 h-3 bg-cyan-400 rounded-full animate-pulse"></div>
                    </div>
                    <div>
                      <h4 className="text-blue-200 font-semibold">Discussions</h4>
                      <p className="text-blue-300/70 text-sm">Community conversations</p>
                    </div>
                  </div>
                </div>

                {/* Questions - Badge Style */}
                <div className="relative group rounded-xl bg-gradient-to-r from-yellow-500/15 to-amber-500/10 border-l-4 border-yellow-500 p-4 hover:border-yellow-400 transition-all duration-300 hover:bg-yellow-500/20">
                  <div className="flex items-center space-x-4">
                    <div className="p-2 rounded-lg bg-yellow-500/25 group-hover:bg-yellow-500/35 transition-colors duration-200 group-hover:rotate-12 transform">
                      <span className="text-2xl">❓</span>
                    </div>
                    <div>
                      <h4 className="text-yellow-200 font-semibold">Questions</h4>
                      <p className="text-yellow-300/70 text-sm">Get help & answers</p>
                    </div>
                  </div>
                </div>

                {/* Showcases - Gallery Style */}
                <div className="relative group rounded-xl bg-gradient-to-br from-purple-500/20 via-pink-500/15 to-purple-600/10 border border-purple-500/30 p-4 hover:border-purple-400/50 transition-all duration-300 overflow-hidden">
                  <div className="absolute inset-0 bg-gradient-to-r from-purple-500/5 via-pink-500/5 to-purple-500/5 opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
                  <div className="relative flex items-center space-x-4">
                    <div className="relative">
                      <div className="p-2 rounded-lg bg-gradient-to-br from-purple-500/25 to-pink-500/20 group-hover:from-purple-500/35 group-hover:to-pink-500/30 transition-all duration-200">
                        <span className="text-2xl">🎮</span>
                      </div>
                      <div className="absolute -top-1 -right-1 w-2 h-2 bg-pink-400 rounded-full"></div>
                      <div className="absolute top-1 -right-2 w-1 h-1 bg-purple-400 rounded-full animate-ping"></div>
                    </div>
                    <div>
                      <h4 className="text-purple-200 font-bold">Showcases</h4>
                      <p className="text-purple-300/70 text-sm">Show off your creations</p>
                    </div>
                  </div>
                </div>

                {/* Tutorials - Book Style */}
                <div className="relative group rounded-xl bg-gradient-to-r from-emerald-500/15 to-teal-500/10 border border-emerald-500/30 p-4 hover:border-emerald-400/50 transition-all duration-300 hover:shadow-inner">
                  <div className="flex items-center space-x-4">
                    <div className="relative">
                      <div className="p-2 rounded-lg bg-emerald-500/20 group-hover:bg-emerald-500/30 transition-colors duration-200 shadow-lg">
                        <span className="text-2xl">📚</span>
                      </div>
                      <div className="absolute bottom-0 left-0 w-full h-1 bg-gradient-to-r from-emerald-400 to-teal-400 rounded-b-lg"></div>
                    </div>
                    <div>
                      <h4 className="text-emerald-200 font-semibold">Tutorials</h4>
                      <p className="text-emerald-300/70 text-sm">Learn new skills</p>
                    </div>
                  </div>
                </div>

                {/* Events - Calendar Style */}
                <div className="relative group rounded-xl bg-gradient-to-r from-indigo-500/15 to-violet-500/10 border border-indigo-500/30 p-4 hover:border-indigo-400/50 transition-all duration-300">
                  <div className="absolute top-2 right-2 w-2 h-2 bg-red-400 rounded-full animate-pulse"></div>
                  <div className="flex items-center space-x-4">
                    <div className="relative">
                      <div className="p-2 rounded-lg bg-indigo-500/20 group-hover:bg-indigo-500/30 transition-colors duration-200">
                        <span className="text-2xl">📅</span>
                      </div>
                      <div className="absolute -bottom-1 left-1/2 transform -translate-x-1/2 w-3 h-1 bg-indigo-400 rounded-full"></div>
                    </div>
                    <div>
                      <h4 className="text-indigo-200 font-semibold">Events</h4>
                      <p className="text-indigo-300/70 text-sm">Join community events</p>
                    </div>
                  </div>
                </div>

                {/* Achievements - Trophy Style */}
                <div className="relative group rounded-xl bg-gradient-to-br from-yellow-600/20 via-orange-500/15 to-red-500/10 border border-yellow-500/30 p-4 hover:border-yellow-400/50 transition-all duration-300 hover:scale-105">
                  <div className="absolute inset-0 bg-gradient-to-br from-yellow-500/5 to-orange-500/5 opacity-0 group-hover:opacity-100 transition-opacity duration-300 rounded-xl"></div>
                  <div className="relative flex items-center space-x-4">
                    <div className="relative">
                      <div className="p-2 rounded-lg bg-gradient-to-br from-yellow-500/25 to-orange-500/20 group-hover:from-yellow-500/35 group-hover:to-orange-500/30 transition-all duration-200 shadow-lg">
                        <span className="text-2xl">🏆</span>
                      </div>
                      <div className="absolute -top-1 -left-1 w-3 h-3 bg-yellow-400 rounded-full animate-bounce"></div>
                    </div>
                    <div>
                      <h4 className="text-yellow-200 font-bold">Achievements</h4>
                      <p className="text-yellow-300/70 text-sm">Celebrate milestones</p>
                    </div>
                  </div>
                </div>

                {/* News - Newspaper Style */}
                <div className="relative group rounded-xl bg-gradient-to-r from-slate-500/15 to-gray-500/10 border border-slate-500/30 p-4 hover:border-slate-400/50 transition-all duration-300">
                  <div className="flex items-center space-x-4">
                    <div className="relative">
                      <div className="p-2 rounded-lg bg-slate-500/20 group-hover:bg-slate-500/30 transition-colors duration-200">
                        <span className="text-2xl">📰</span>
                      </div>
                      <div className="absolute top-0 right-0 w-full h-full border border-slate-400/20 rounded-lg transform rotate-1 -z-10"></div>
                    </div>
                    <div>
                      <h4 className="text-slate-200 font-semibold">News</h4>
                      <p className="text-slate-300/70 text-sm">Latest gaming news</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
