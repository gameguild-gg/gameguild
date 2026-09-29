import type { Metadata } from 'next';
import { notFound, permanentRedirect } from 'next/navigation';

import { BlogPostView } from '@/components/blogs/blog-post-view';
import { BlogComments } from '@/components/blogs/blog-comments';
import { BlogReactions } from '@/components/blogs/blog-reactions';
import { buildBlogPostMetadata, isReservedBlogSegment, type BlogAuthorProfile } from '@/lib/blogs/seo';
import { getBlogPost } from '@/lib/blogs/queries';
import { getViewerBlogAuthor } from '@/lib/blogs/actions';

interface PostPageProps {
  readonly params: Promise<{ username: string; slug: string }>;
}

export async function generateMetadata({ params }: PostPageProps): Promise<Metadata> {
  const { username, slug } = await params;

  if (isReservedBlogSegment(username)) {
    return { title: 'Not Found', robots: { index: false, follow: false } };
  }

  const lookup = await getBlogPost(decodeURIComponent(username), decodeURIComponent(slug));
  if (lookup.status !== 'ok') {
    return { title: 'Not Found', robots: { index: false, follow: false } };
  }

  const authorProfiles: BlogAuthorProfile[] = [
    { handle: lookup.post.primaryAuthorHandle, displayName: lookup.post.primaryAuthorDisplayName },
    ...(lookup.post.coAuthorHandles ?? []).map((handle) => ({ handle, displayName: null as string | null })),
  ];

  return buildBlogPostMetadata(lookup.post, authorProfiles);
}

export default async function BlogPostPage({ params }: PostPageProps) {
  const { username, slug } = await params;

  if (isReservedBlogSegment(username)) {
    notFound();
  }

  const lookup = await getBlogPost(decodeURIComponent(username), decodeURIComponent(slug));

  if (lookup.status === 'redirect') {
    permanentRedirect(`/blogs/${lookup.redirect.handle}/${lookup.redirect.slug}`);
  }

  if (lookup.status === 'not-found') {
    notFound();
  }

  const post = lookup.post;
  const viewer = await getViewerBlogAuthor();

  return (
    <>
      <BlogPostView post={post} />
      {post.id ? (
        <div className="mx-auto max-w-3xl px-4 pb-12">
          <BlogReactions
            postId={post.id}
            currentUserId={viewer.userId}
            initialReactionCount={post.reactionCount ?? 0}
          />
          <BlogComments
            postId={post.id}
            allowComments={post.allowComments ?? true}
            currentUserId={viewer.userId}
            isPostAuthor={
              viewer.handle != null &&
              viewer.handle.length > 0 &&
              (viewer.handle === username ||
                (post.coAuthorHandles ?? []).includes(viewer.handle))
            }
            coAuthorIds={[]}
            currentUserHandle={viewer.handle}
          />
        </div>
      ) : null}
    </>
  );
}
