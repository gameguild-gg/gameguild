'use server';

/**
 * Blog admin console actions (issue #96): form-bound server actions wrapping the
 * fetch-based admin lib, then redirecting back with a status message (mirrors
 * `lib/community/actions/roles.ts`).
 */

import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';

import {
  adminDeleteComment,
  adminDeletePost,
  adminImportGhost,
  adminUnpublishPost,
} from './admin';

const CONSOLE_BLOG_PATH = '/console/community/blog';

function buildHref(tab: 'posts' | 'comments', params: { page?: number; message?: string; error?: string }) {
  const search = new URLSearchParams({ tab });
  if (params.page && params.page > 0) search.set('page', String(params.page));
  if (params.message) search.set('message', params.message);
  if (params.error) search.set('error', params.error);
  return `${CONSOLE_BLOG_PATH}?${search.toString()}`;
}

function revalidateBlogAdmin() {
  revalidatePath(CONSOLE_BLOG_PATH);
  revalidatePath('/blogs');
}

function readText(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function readPage(formData: FormData) {
  return Number(readText(formData, 'page')) || 0;
}

export async function unpublishPostAction(formData: FormData) {
  const postId = readText(formData, 'postId');
  const page = readPage(formData);
  if (!postId) redirect(buildHref('posts', { page, error: 'Missing post id.' }));

  const result = await adminUnpublishPost(postId);
  revalidateBlogAdmin();
  redirect(
    result.success
      ? buildHref('posts', { page, message: 'Post unpublished (takedown).' })
      : buildHref('posts', { page, error: result.error }),
  );
}

export async function deletePostAction(formData: FormData) {
  const postId = readText(formData, 'postId');
  const page = readPage(formData);
  if (!postId) redirect(buildHref('posts', { page, error: 'Missing post id.' }));

  const result = await adminDeletePost(postId);
  revalidateBlogAdmin();
  redirect(
    result.success
      ? buildHref('posts', { page, message: 'Post deleted.' })
      : buildHref('posts', { page, error: result.error }),
  );
}

export async function deleteCommentAction(formData: FormData) {
  const commentId = readText(formData, 'commentId');
  const page = readPage(formData);
  if (!commentId) redirect(buildHref('comments', { page, error: 'Missing comment id.' }));

  const result = await adminDeleteComment(commentId);
  revalidateBlogAdmin();
  redirect(
    result.success
      ? buildHref('comments', { page, message: 'Comment removed.' })
      : buildHref('comments', { page, error: result.error }),
  );
}

export async function importGhostAction(formData: FormData) {
  const file = formData.get('exportFile');
  const pasted = readText(formData, 'exportJson');
  const dryRun = formData.get('dryRun') === 'on';
  const publishImported = formData.get('publishImported') !== null;

  const exportJson = file instanceof File && file.size > 0 ? await file.text() : pasted;
  if (!exportJson) {
    redirect(buildHref('posts', { error: 'Provide a Ghost export file or paste the export JSON.' }));
  }

  const result = await adminImportGhost(exportJson, { dryRun, publishImported });
  revalidateBlogAdmin();

  if (!result.success) {
    redirect(buildHref('posts', { error: result.error }));
  }

  const report = result.report;
  const summary =
    `${report.dryRun ? 'Dry run planned' : 'Imported'} ${report.imported}/${report.postsInExport} posts` +
    ` (${report.skippedDuplicate} duplicates, ${report.skippedNoAuthor} unmapped authors,` +
    ` ${report.skippedNonPost} non-posts, ${report.commentsInExport} member comments counted, not imported).`;
  redirect(buildHref('posts', { message: summary }));
}
