'use server';

// STUB: Posts actions are stubbed; endpoints are unavailable in current SDK.

export type GetApiPostsData = any;
export type PostApiPostsData = any;
export type GetApiPostsByPostIdData = any;

export async function getPosts(_data?: GetApiPostsData): Promise<any> {
  throw new Error('Not implemented (STUB): getPosts');
}

export async function createPost(_data?: PostApiPostsData): Promise<any> {
  throw new Error('Not implemented (STUB): createPost');
}

export async function getPostById(_data: GetApiPostsByPostIdData): Promise<any> {
  throw new Error('Not implemented (STUB): getPostById');
}

export async function getRecentPosts(_limit: number = 10): Promise<any> {
  throw new Error('Not implemented (STUB): getRecentPosts');
}
