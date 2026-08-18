'use server';

import { loadPosts, type PostsStream } from './queries';

export async function loadPostsAction(stream: PostsStream, skip: number): Promise<{ items: Awaited<ReturnType<typeof loadPosts>>; nextSkip: number | null }> {
  const items = await loadPosts(stream, skip);
  const nextSkip = items.length > 0 ? skip + items.length : null;
  return { items, nextSkip };
}
