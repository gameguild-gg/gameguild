import { Metadata } from 'next';
import { 
  CommunityFeed, 
  PostCard, 
  FeedHeader, 
  PostFilters, 
  PinnedPosts 
} from '@/components/feed';
import type { PostDto, FeedFilters } from '@/lib/feed';

export const metadata: Metadata = {
  title: 'Feed Components Demo | Game Guild',
  description: 'Demonstration of all community feed components and their features.',
};

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
              <h3 className="text-lg font-medium text-blue-400 mb-3">Component Features</h3>
              <ul className="space-y-2 text-slate-300">
                <li>✓ Server-side rendering support</li>
                <li>✓ Responsive design with Tailwind CSS</li>
                <li>✓ Type-safe with TypeScript</li>
                <li>✓ Composition pattern architecture</li>
                <li>✓ Reusable components</li>
                <li>✓ Accessibility best practices</li>
                <li>✓ Loading states and error handling</li>
                <li>✓ Infinite scroll pagination</li>
              </ul>
            </div>
            <div>
              <h3 className="text-lg font-medium text-green-400 mb-3">Post Types</h3>
              <ul className="space-y-2 text-slate-300">
                <li>📢 Announcements</li>
                <li>💬 Discussions</li>
                <li>❓ Questions</li>
                <li>🎮 Showcases</li>
                <li>📚 Tutorials</li>
                <li>📅 Events</li>
                <li>🏆 Achievements</li>
                <li>📰 News</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
