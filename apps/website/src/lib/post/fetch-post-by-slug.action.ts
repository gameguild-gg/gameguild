'use server';

import { Post } from '@/lib/post/post';

export async function fetchPostBySlug(slug: string): Promise<Readonly<Post | null>> {
  return null;
}
