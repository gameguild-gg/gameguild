# Community Feed System

A comprehensive, reusable community feed system built with Next.js, TypeScript, and Tailwind CSS, following composition
patterns and best practices for server-side rendering.

## Architecture

The feed system is built using a composition pattern with reusable components that can be combined to create different
feed experiences:

### Core Components

- **`CommunityFeed`** - Interactive client-side feed with full functionality
- **`CommunityFeedServer`** - Server-side rendered version for better initial load
- **`PostCard`** - Individual post display with interactions
- **`FeedHeader`** - Statistics and metrics display
- **`PostFilters`** - Search and filtering controls
- **`PostsList`** - Grid layout for posts with pagination
- **`LoadMore`** - Pagination component for infinite scroll
- **`PinnedPosts`** - Special display for highlighted posts

### API Layer

- **`lib/feed/api.ts`** - API client functions for fetching posts
- **`lib/feed/types.ts`** - Type definitions re-exported from generated API types

## Features

### ✅ Implemented Features

- **Server-Side Rendering** - Full SSR support with loading states
- **Type Safety** - Complete TypeScript coverage with generated API types
- **Responsive Design** - Mobile-first design with Tailwind CSS
- **Composition Pattern** - Reusable components that can be combined
- **Post Types** - Support for multiple post types with custom styling
- **Interactions** - Like, comment, and share functionality
- **Filtering** - Search, type filtering, and sorting
- **Pagination** - Infinite scroll with load more functionality
- **Pinned Posts** - Special highlighting for important posts
- **Loading States** - Skeleton loading and loading indicators
- **Error Handling** - Graceful error handling throughout

### 🎨 Design System

Following the contributors page design patterns:

- Gradient backgrounds (slate-900 to slate-800)
- Glassmorphism effects with backdrop blur
- Responsive grid layouts
- Hover effects and transitions
- Consistent spacing and typography
- Accessible color contrasts

## Usage

### Basic Feed Page

```tsx
import { CommunityFeed } from '@/components/feed';

export default function FeedPage() {
  return <CommunityFeed />;
}
```

### Server-Side Rendered Feed

```tsx
import { CommunityFeedServer } from '@/components/feed';

export default function SSRFeedPage({ searchParams }) {
  return <CommunityFeedServer searchParams={searchParams} />;
}
```

### Individual Components

```tsx
import { PostCard, FeedHeader, PostFilters } from '@/components/feed';

// Use individual components for custom layouts
function CustomFeed() {
  return (
    <div>
      <FeedHeader totalPosts={100} activeUsers={50} engagementRate={75} />
      <PostFilters filters={filters} onFiltersChange={handleChange} />
      <PostCard post={post} onLike={handleLike} />
    </div>
  );
}
```

## Post Types

The system supports various post types with custom styling:

- **`general`** - Default posts (blue accent)
- **`announcement`** - Important announcements (yellow accent)
- **`discussion`** - Community discussions (green accent)
- **`question`** - User questions (purple accent)
- **`showcase`** - Project showcases (pink accent)
- **`tutorial`** - Educational content (indigo accent)
- **`event`** - Community events (orange accent)
- **`user_registration`** - User signups (gray accent)
- **`project_completion`** - Completed projects (emerald accent)
- **`achievement_unlocked`** - User achievements (yellow accent)
- **`milestone`** - Important milestones (blue accent)
- **`news`** - News and updates (red accent)

## API Integration

The system integrates with the Game Guild Posts API:

```typescript
// Fetch posts with filtering
const posts = await fetchPosts({
  searchTerm: 'game development',
  postType: 'discussion',
  orderBy: 'createdAt',
  descending: true,
  pageNumber: 1,
  pageSize: 20,
});

// Fetch pinned posts
const pinnedPosts = await fetchPinnedPosts();

// Fetch specific post
const post = await fetchPostById('post-id');
```

## Component Props

### CommunityFeed

Client-side interactive feed component.

```typescript
interface CommunityFeedProps {
  // No props - fully self-contained
}
```

### CommunityFeedServer

Server-side rendered feed component.

```typescript
interface CommunityFeedServerProps {
  searchParams?: Record<string, string | string[] | undefined>;
}
```

### PostCard

Individual post display component.

```typescript
interface PostCardProps {
  post: PostDto;
  onLike?: (postId: string) => void;
  onComment?: (postId: string) => void;
  onShare?: (postId: string) => void;
}
```

### FeedHeader

Statistics header component.

```typescript
interface FeedHeaderProps {
  totalPosts: number;
  activeUsers: number;
  engagementRate: number;
}
```

### PostFilters

Filtering and search component.

```typescript
interface PostFiltersProps {
  filters: FeedFilters;
  onFiltersChange: (filters: FeedFilters) => void;
  postTypes: string[];
}
```

## File Structure

```
src/
├── components/feed/
│   ├── community-feed.tsx       # Interactive feed component
│   ├── community-feed-server.tsx # SSR feed component
│   ├── post-card.tsx           # Individual post component
│   ├── feed-header.tsx         # Statistics header
│   ├── post-filters.tsx        # Search and filters
│   ├── posts-list.tsx          # Posts grid layout
│   ├── load-more.tsx           # Pagination component
│   ├── pinned-posts.tsx        # Pinned posts display
│   └── index.ts                # Component exports
├── lib/feed/
│   ├── api.ts                  # API client functions
│   ├── types.ts                # Type definitions
│   └── index.ts                # Library exports
└── app/[locale]/(community)/feed/
    ├── page.tsx                # Main feed page
    └── demo/
        └── page.tsx            # Component demo page
```

## Demo

Visit `/feed/demo` to see a comprehensive demonstration of all components and their features.

## Development

### Adding New Post Types

1. Add the type to the `PostType` union in `types.ts`
2. Add styling for the type in `post-card.tsx`
3. Update the `POST_TYPES` array in feed components

### Customizing Styles

The system uses Tailwind CSS with a consistent design system. Key design tokens:

- **Background**: `bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900`
- **Cards**: `bg-slate-800/50 backdrop-blur-sm`
- **Text**: `text-white`, `text-slate-300`, `text-slate-400`
- **Accents**: Type-specific colors for post types

### Adding Interactions

Post interactions are handled through callback props:

```typescript
const handleLike = useCallback((postId: string) => {
  // Implement like functionality
  console.log('Like post:', postId);
}, []);

const handleComment = useCallback((postId: string) => {
  // Implement comment functionality
  console.log('Comment on post:', postId);
}, []);
```

## Best Practices

1. **Server-Side Rendering** - Use `CommunityFeedServer` for initial page loads
2. **Type Safety** - Always use the generated API types
3. **Error Handling** - Implement proper error boundaries
4. **Loading States** - Provide feedback during data fetching
5. **Accessibility** - Ensure keyboard navigation and screen reader support
6. **Performance** - Implement pagination and lazy loading
7. **Caching** - Consider implementing client-side caching for better UX

## Future Enhancements

- Real-time updates with WebSockets
- Image/media upload support
- Rich text editor for post creation
- Advanced filtering options
- User mentions and tagging
- Push notifications
- Offline support with service workers
