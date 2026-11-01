import { Link } from '@/i18n/navigation';

import { requireDashboardCapability } from '@/lib/require-dashboard-capability';
import {
  getBlogAdminComments,
  getBlogAdminPosts,
  getBlogAdminStats,
  type BlogAdminPost,
} from '@/lib/blogs/admin';
import {
  deleteCommentAction,
  deletePostAction,
  importGhostAction,
  unpublishPostAction,
} from '@/lib/blogs/admin-actions';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { buttonVariants } from '@game-guild/ui/components/button-variants';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Checkbox } from '@game-guild/ui/components/checkbox';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { Textarea } from '@game-guild/ui/components/textarea';
import { FileEdit, FileText, MessageSquare, ShieldAlert, Upload } from 'lucide-react';

interface Props {
  searchParams?: Promise<{
    tab?: string;
    page?: string;
    message?: string;
    error?: string;
  }>;
}

function formatDate(value: string | null) {
  if (!value) return '—';
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

function postHref(post: Pick<BlogAdminPost, 'primaryAuthorHandle' | 'slug'>) {
  return post.primaryAuthorHandle && post.slug
    ? `/blogs/${post.primaryAuthorHandle}/${post.slug}`
    : '/blogs';
}

function statusBadgeVariant(status: BlogAdminPost['status']) {
  return status === 'Published' ? 'default' : 'secondary';
}

export default async function BlogAdminConsolePage({ searchParams }: Props) {
  await requireDashboardCapability('Community.ManageBlog');

  const query = await searchParams;
  const tab = query?.tab === 'comments' ? 'comments' : 'posts';
  const page = Math.max(0, Number(query?.page ?? 0) || 0);
  const includePage = page > 0 ? `&page=${page}` : '';

  const [stats, posts, comments] = await Promise.all([
    getBlogAdminStats(),
    getBlogAdminPosts(tab === 'posts' ? page : 0),
    getBlogAdminComments(tab === 'comments' ? page : 0),
  ]);

  if (stats.status === 'forbidden' || posts.status === 'forbidden' || comments.status === 'forbidden') {
    return (
      <main className="px-4 py-16">
        <div className="mx-auto max-w-3xl space-y-4">
          <h1 className="text-2xl font-semibold">Blog administration</h1>
          <Alert variant="destructive">
            <ShieldAlert className="size-4" />
            <AlertTitle>SystemAdmin required</AlertTitle>
            <AlertDescription>
              Blog moderation is a platform-scope capability. Sign in with a SystemAdmin account.
            </AlertDescription>
          </Alert>
        </div>
      </main>
    );
  }

  if (stats.status === 'error' || posts.status === 'error' || comments.status === 'error') {
    return (
      <main className="px-4 py-16">
        <div className="mx-auto max-w-3xl space-y-4">
          <h1 className="text-2xl font-semibold">Blog administration</h1>
          <Alert variant="destructive">
            <ShieldAlert className="size-4" />
            <AlertTitle>Failed to load blog administration data</AlertTitle>
            <AlertDescription>
              {stats.status === 'error'
                ? stats.message
                : posts.status === 'error'
                  ? posts.message
                  : comments.status === 'error'
                    ? comments.message
                    : 'Unknown error.'}
            </AlertDescription>
          </Alert>
        </div>
      </main>
    );
  }

  const statCards = [
    { label: 'Published posts', value: stats.data.publishedPosts, icon: FileText },
    { label: 'Draft posts', value: stats.data.draftPosts, icon: FileEdit },
    { label: 'Comments (30 days)', value: stats.data.commentsLast30Days, icon: MessageSquare },
  ];

  return (
    <main className="px-4 py-8">
      <div className="mx-auto max-w-7xl space-y-8">
        <header className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Blog administration</h1>
            <p className="mt-1 text-muted-foreground">
              Platform-wide post moderation, comment removal, and the Ghost content migration (issue #96).
            </p>
          </div>
          <Link href="/blogs" className={buttonVariants({ variant: 'outline' })}>
            Public blog index
          </Link>
        </header>

        {query?.message ? (
          <Alert>
            <AlertTitle>Done</AlertTitle>
            <AlertDescription>{query.message}</AlertDescription>
          </Alert>
        ) : null}
        {query?.error ? (
          <Alert variant="destructive">
            <AlertTitle>Failed</AlertTitle>
            <AlertDescription>{query.error}</AlertDescription>
          </Alert>
        ) : null}

        <div className="grid gap-4 sm:grid-cols-3">
          {statCards.map((card) => (
            <Card key={card.label}>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">{card.label}</CardTitle>
                <card.icon className="size-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{card.value}</div>
              </CardContent>
            </Card>
          ))}
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Ghost content migration</CardTitle>
            <CardDescription>
              Import a Ghost export JSON. Posts keep their slug, SEO fields, tags, and original publish
              date; authors map by profile handle. Re-runs skip duplicates. Ghost member comments have
              no platform identity and are counted, not imported.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form action={importGhostAction} className="space-y-4">
              <div className="grid gap-2">
                <Label htmlFor="ghost-export-file">Ghost export file</Label>
                <Input id="ghost-export-file" name="exportFile" type="file" accept=".json,application/json" />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="ghost-export-json">…or paste the export JSON</Label>
                <Textarea id="ghost-export-json" name="exportJson" rows={4} placeholder='{"db":[{"data":{…}}]}' />
              </div>
              <div className="flex flex-wrap gap-6">
                <div className="flex items-center gap-2">
                  <Checkbox id="ghost-dry-run" name="dryRun" defaultChecked />
                  <Label htmlFor="ghost-dry-run">Dry run (plan only, persist nothing)</Label>
                </div>
                <div className="flex items-center gap-2">
                  <Checkbox id="ghost-publish" name="publishImported" defaultChecked />
                  <Label htmlFor="ghost-publish">Publish posts that were published in Ghost</Label>
                </div>
              </div>
              <button type="submit" className={buttonVariants()}>
                <Upload className="size-4" />
                Run import
              </button>
            </form>
          </CardContent>
        </Card>

        <div className="flex items-center gap-2">
          <Link
            href={`/console/community/blog?tab=posts${includePage}`}
            className={buttonVariants({ variant: tab === 'posts' ? 'default' : 'ghost' })}
          >
            Posts
          </Link>
          <Link
            href={`/console/community/blog?tab=comments${includePage}`}
            className={buttonVariants({ variant: tab === 'comments' ? 'default' : 'ghost' })}
          >
            Comments
          </Link>
        </div>

        {tab === 'posts' ? (
          <Card>
            <CardHeader>
              <CardTitle>All posts ({posts.data.items.length})</CardTitle>
              <CardDescription>Every live post across all authors, newest edit first.</CardDescription>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Title</TableHead>
                    <TableHead>Author</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Published</TableHead>
                    <TableHead>Comments</TableHead>
                    <TableHead>Moderation</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {posts.data.items.map((post) => (
                    <TableRow key={post.id}>
                      <TableCell className="max-w-64 truncate font-medium">
                        <Link href={postHref(post)} className="hover:underline">
                          {post.title}
                        </Link>
                      </TableCell>
                      <TableCell>{post.primaryAuthorHandle || '—'}</TableCell>
                      <TableCell>
                        <Badge variant={statusBadgeVariant(post.status)}>{post.status}</Badge>
                      </TableCell>
                      <TableCell>{formatDate(post.publishedAt)}</TableCell>
                      <TableCell>{post.commentsCount}</TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-2">
                          {post.status === 'Published' ? (
                            <form action={unpublishPostAction}>
                              <input type="hidden" name="postId" value={post.id} />
                              <input type="hidden" name="page" value={page} />
                              <button type="submit" className={buttonVariants({ variant: 'outline', size: 'sm' })}>
                                Take down
                              </button>
                            </form>
                          ) : null}
                          <form action={deletePostAction}>
                            <input type="hidden" name="postId" value={post.id} />
                            <input type="hidden" name="page" value={page} />
                            <button type="submit" className={buttonVariants({ variant: 'destructive', size: 'sm' })}>
                              Delete
                            </button>
                          </form>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                  {posts.data.items.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} className="py-8 text-center text-muted-foreground">
                        No posts found.
                      </TableCell>
                    </TableRow>
                  ) : null}
                </TableBody>
              </Table>
              <Pager page={page} hasMore={posts.data.hasMore} tab={tab} />
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardHeader>
              <CardTitle>Recent comments ({comments.data.items.length})</CardTitle>
              <CardDescription>Platform-wide comment feed, newest first. Removal is a soft delete.</CardDescription>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Comment</TableHead>
                    <TableHead>Author</TableHead>
                    <TableHead>Post</TableHead>
                    <TableHead>Created</TableHead>
                    <TableHead>Moderation</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {comments.data.items.map((comment) => (
                    <TableRow key={comment.id}>
                      <TableCell className="max-w-96 truncate">{comment.content}</TableCell>
                      <TableCell>{comment.authorHandle || '—'}</TableCell>
                      <TableCell className="max-w-64 truncate">
                        {comment.postHandle && comment.postSlug ? (
                          <Link
                            href={`/blogs/${comment.postHandle}/${comment.postSlug}`}
                            className="hover:underline"
                          >
                            {comment.postTitle}
                          </Link>
                        ) : (
                          comment.postTitle || '—'
                        )}
                      </TableCell>
                      <TableCell>{formatDate(comment.createdAt)}</TableCell>
                      <TableCell>
                        <form action={deleteCommentAction}>
                          <input type="hidden" name="commentId" value={comment.id} />
                          <input type="hidden" name="page" value={page} />
                          <button type="submit" className={buttonVariants({ variant: 'destructive', size: 'sm' })}>
                            Remove
                          </button>
                        </form>
                      </TableCell>
                    </TableRow>
                  ))}
                  {comments.data.items.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} className="py-8 text-center text-muted-foreground">
                        No comments found.
                      </TableCell>
                    </TableRow>
                  ) : null}
                </TableBody>
              </Table>
              <Pager page={page} hasMore={comments.data.hasMore} tab={tab} />
            </CardContent>
          </Card>
        )}
      </div>
    </main>
  );
}

function Pager({ page, hasMore, tab }: { page: number; hasMore: boolean; tab: 'posts' | 'comments' }) {
  return (
    <div className="flex items-center justify-between pt-4">
      {page > 0 ? (
        <Link
          href={`/console/community/blog?tab=${tab}&page=${page - 1}`}
          className={buttonVariants({ variant: 'outline', size: 'sm' })}
        >
          Previous
        </Link>
      ) : (
        <span />
      )}
      {hasMore ? (
        <Link
          href={`/console/community/blog?tab=${tab}&page=${page + 1}`}
          className={buttonVariants({ variant: 'outline', size: 'sm' })}
        >
          Next
        </Link>
      ) : (
        <span />
      )}
    </div>
  );
}
