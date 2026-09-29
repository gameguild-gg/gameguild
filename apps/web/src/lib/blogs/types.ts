/**
 * App-level blog type surface.
 *
 * Thin re-exports of the generated client's Social.Blog DTOs so feature code
 * never imports `@game-guild/client` types directly.
 */
import type {
  SocialBlogBlogContentFormat,
  SocialBlogBlogPostStatus,
  SocialBlogControllersAddBlogCommentInput,
  SocialBlogControllersBlogCoauthorInput,
  SocialBlogControllersChangeBlogPostSlugInput,
  SocialBlogControllersCreateBlogPostInput,
  SocialBlogControllersTransferBlogPrimaryInput,
  SocialBlogControllersUpdateBlogPostDraftInput,
  SocialBlogQueriesBlogCommentDto,
  SocialBlogQueriesBlogPostDetailDto,
  SocialBlogQueriesBlogPostSummaryDto,
} from '@game-guild/client';

export type BlogContentFormat = SocialBlogBlogContentFormat;
export type BlogPostStatus = SocialBlogBlogPostStatus;

export type BlogPostSummary = SocialBlogQueriesBlogPostSummaryDto;
export type BlogPostDetail = SocialBlogQueriesBlogPostDetailDto;
export type BlogComment = SocialBlogQueriesBlogCommentDto;

export type CreateBlogPostInput = SocialBlogControllersCreateBlogPostInput;
export type UpdateBlogPostDraftInput = SocialBlogControllersUpdateBlogPostDraftInput;
export type ChangeBlogPostSlugInput = SocialBlogControllersChangeBlogPostSlugInput;
export type BlogCoauthorInput = SocialBlogControllersBlogCoauthorInput;
export type TransferBlogPrimaryInput = SocialBlogControllersTransferBlogPrimaryInput;
export type AddBlogCommentInput = SocialBlogControllersAddBlogCommentInput;

export interface BlogPostSummaryPage {
  items: BlogPostSummary[];
  hasMore: boolean;
}

export interface BlogCommentPage {
  items: BlogComment[];
  hasMore: boolean;
}

export interface BlogRouteResolution {
  handle: string;
  slug: string;
}
