import type { Metadata } from 'next';
import { notFound, permanentRedirect } from 'next/navigation';

import { BlogPostView } from '@/components/blogs/blog-post-view';
import { buildBlogPostMetadata, isReservedBlogSegment, type BlogAuthorProfile } from '@/lib/blogs/seo';
import { getBlogPost } from '@/lib/blogs/queries';

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

  return <BlogPostView post={lookup.post} />;
}
