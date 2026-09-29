import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn((config: unknown) => config),
  publicGetPosts: vi.fn(),
  publicGetAuthorPosts: vi.fn(),
  publicGetPostDetail: vi.fn(),
  publicResolve: vi.fn(),
  publicGetComments: vi.fn(),
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    SocialBlogPublicModule: class {
      getApiSocialBlogPublicPosts = mocks.publicGetPosts;
      getApiSocialBlogPublicAuthorsForGetApiSocialBlogPublicAuthorsByHandle = mocks.publicGetAuthorPosts;
      getApiSocialBlogPublicAuthorsForGetApiSocialBlogPublicAuthorsByHandleBySlug = mocks.publicGetPostDetail;
      getApiSocialBlogPublicResolve = mocks.publicResolve;
      getApiSocialBlogPublicPostsComments = mocks.publicGetComments;
    },
  },
}));

import { getAuthorPosts, getBlogIndex, getBlogPost, getBlogPostComments } from './queries';

const ok = <T>(data: T) => ({ ok: true as const, data });
const notFound = () => ({ ok: false as const, error: { status: 404, message: 'Not Found' } });
const serverError = () => ({ ok: false as const, error: { status: 500, message: 'boom' } });

beforeEach(() => {
  vi.clearAllMocks();
});

describe('getBlogPost redirect resolution', () => {
  it('returns the post on 200', async () => {
    mocks.publicGetPostDetail.mockResolvedValue(ok({ id: 'p1', title: 'Hello' }));

    const result = await getBlogPost('alice', 'hello');

    expect(result).toEqual({ status: 'ok', post: { id: 'p1', title: 'Hello' } });
    expect(mocks.publicResolve).not.toHaveBeenCalled();
  });

  it('yields a redirect instruction when resolve finds the moved post (not a throw)', async () => {
    mocks.publicGetPostDetail.mockResolvedValue(notFound());
    mocks.publicResolve.mockResolvedValue(ok({ handle: 'alice', slug: 'renamed-post' }));

    const result = await getBlogPost('alice', 'old-slug');

    expect(result).toEqual({ status: 'redirect', redirect: { handle: 'alice', slug: 'renamed-post' } });
  });

  it('yields not-found when both detail and resolve 404 (draft vs never-existing are the same)', async () => {
    mocks.publicGetPostDetail.mockResolvedValue(notFound());
    mocks.publicResolve.mockResolvedValue(notFound());

    const result = await getBlogPost('alice', 'nope');

    expect(result).toEqual({ status: 'not-found' });
  });

  it('yields not-found when resolve returns an empty resolution payload', async () => {
    mocks.publicGetPostDetail.mockResolvedValue(notFound());
    mocks.publicResolve.mockResolvedValue(ok({}));

    const result = await getBlogPost('alice', 'nope');

    expect(result).toEqual({ status: 'not-found' });
  });

  it('throws on non-404 detail errors without calling resolve', async () => {
    mocks.publicGetPostDetail.mockResolvedValue(serverError());

    await expect(getBlogPost('alice', 'hello')).rejects.toThrow('boom');
    expect(mocks.publicResolve).not.toHaveBeenCalled();
  });
});

describe('list reads', () => {
  it('getBlogIndex maps page payload', async () => {
    mocks.publicGetPosts.mockResolvedValue(
      ok({ items: [{ id: 'p1', slug: 'a' }], hasMore: true }),
    );

    const page = await getBlogIndex({ beforeId: 'zz' });

    expect(page).toEqual({ items: [{ id: 'p1', slug: 'a' }], hasMore: true });
    expect(mocks.publicGetPosts).toHaveBeenCalledWith({ beforeId: 'zz' });
  });

  it('getBlogIndex returns null on 404 and throws otherwise', async () => {
    mocks.publicGetPosts.mockResolvedValueOnce(notFound());
    await expect(getBlogIndex()).resolves.toBeNull();

    mocks.publicGetPosts.mockResolvedValueOnce(serverError());
    await expect(getBlogIndex()).rejects.toThrow();
  });

  it('getAuthorPosts maps page payload', async () => {
    mocks.publicGetAuthorPosts.mockResolvedValue(ok({ items: [{ id: 'p2' }], hasMore: false }));

    const page = await getAuthorPosts('alice');

    expect(page).toEqual({ items: [{ id: 'p2' }], hasMore: false });
    expect(mocks.publicGetAuthorPosts).toHaveBeenCalledWith('alice', undefined);
  });

  it('getBlogPostComments maps page payload and null on 404', async () => {
    mocks.publicGetComments.mockResolvedValueOnce(ok({ items: [{ id: 'c1' }], hasMore: false }));
    await expect(getBlogPostComments('p1')).resolves.toEqual({ items: [{ id: 'c1' }], hasMore: false });

    mocks.publicGetComments.mockResolvedValueOnce(notFound());
    await expect(getBlogPostComments('p1')).resolves.toBeNull();
  });
});
