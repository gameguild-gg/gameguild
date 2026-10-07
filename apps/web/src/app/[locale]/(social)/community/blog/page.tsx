import { Link } from '@/i18n/navigation';
import { getSession } from '@/auth';
import { listMyBlogPosts } from '@/lib/blogs/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { buttonVariants } from '@game-guild/ui/components/button-variants';
import { FileText, Plus } from 'lucide-react';

function postEditedAt(post: { publishedAt: string | null; updatedAt: string }) {
  const at = post.publishedAt ?? post.updatedAt;
  return new Date(at).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

export default async function Page() {
  const session = await getSession();
  const blogPosts = session ? await listMyBlogPosts() : [];

  if (!session) {
    return (
      <main className="px-4 py-16">
        <div className="mx-auto max-w-3xl rounded-3xl border border-border bg-card p-8">
          <h1 className="text-3xl font-semibold">Blog</h1>
          <p className="mt-3 text-muted-foreground">Sign in to manage your blog posts.</p>
          <Link href="/sign-in" className={buttonVariants({ className: 'mt-6' })}>Sign in</Link>
        </div>
      </main>
    );
  }

  return (
    <main className="px-4 py-12">
      <div className="mx-auto max-w-7xl space-y-8">
        <header className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <h1 className="flex items-center gap-2 text-3xl font-bold tracking-tight"><FileText className="size-6" />Blog</h1>
            <p className="mt-1 text-muted-foreground">Drafts and published articles under your handle.</p>
          </div>
          <Link href="/blog/new" className={buttonVariants()}><Plus className="size-4" />New post</Link>
        </header>

        <div className="max-h-[60vh] space-y-2 overflow-y-auto">
          {blogPosts.map((post) => {
            const href = post.primaryAuthorHandle && post.slug ? `/blogs/${post.primaryAuthorHandle}/${post.slug}/edit` : '/blogs';
            return (
              <Link key={post.id} href={href} className="flex items-center justify-between gap-4 rounded-lg border p-3 transition hover:bg-muted/50">
                <span className="min-w-0 truncate font-medium">{post.title}</span>
                <span className="flex shrink-0 items-center gap-3 text-sm text-muted-foreground">
                  {postEditedAt(post)}
                  <Badge variant="secondary">{post.status}</Badge>
                </span>
              </Link>
            );
          })}
        </div>

        {blogPosts.length === 0 && (
          <div className="rounded-lg border p-8 text-center">
            <p className="text-muted-foreground">No posts yet.</p>
            <Link href="/blog/new" className={buttonVariants({ variant: 'outline', className: 'mt-4' })}><Plus className="size-4" />New post</Link>
          </div>
        )}
      </div>
    </main>
  );
}
