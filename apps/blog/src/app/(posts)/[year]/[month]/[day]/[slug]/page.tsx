import React from 'react';
import { notFound } from 'next/navigation';
import { decodeParams, isValidDate } from '@/app/utils';
import { getPostBySlug, getPrimaryTag, getPublishedPosts, getTagsByPost } from '@/app/actions/posts';

interface Props {
  params: Promise<{ year: string; month: string; day: string; slug: string }>;
}

export async function generateStaticParams(): Promise<Array<{ year: string; month: string; day: string; slug: string }>> {
  const posts = await getPublishedPosts();

  return posts.map((post) => ({
    year: post.publishedAt.getFullYear().toString(),
    month: (post.publishedAt.getMonth() + 1).toString().padStart(2, '0'),
    day: post.publishedAt.getDate().toString().padStart(2, '0'),
    slug: post.slug,
  }));
}

export default async function Page({ params }: Props): Promise<React.JSX.Element> {
  const { year, month, day, slug } = decodeParams(await params);

  if (!year || !month || !day || !slug) notFound();

  if (!isValidDate(year, month, day)) notFound();

  const post = await getPostBySlug(slug);

  if (!post) return notFound();

  const postTags = await getTagsByPost(post);
  const primaryTag = await getPrimaryTag(post);

  return (
    <>
      <article className="flex flex-col flex-1 gap-8">
        <header className="">
          <h1 className="">{post?.title}</h1>
        </header>
        <div className="flex flex-row flex-1 gap-8">
          <aside className="flex flex-col flex-1 gap-8 max-w-60">
            <section>
              {/* Category and Date */}
              <div className="space-y-2">
                <div className="text-sm text-muted-foreground">
                  <time dateTime={post.publishedAt.toISOString()}>
                    {year}/{month}/{day}
                  </time>
                </div>
                <div className="space-y-1">
                  <h3 className="text-sm font-medium">Primary Category</h3>
                  <div className="flex flex-col gap-1">
                    {primaryTag && <span className="text-sm bg-primary/10 text-primary px-2 py-1 rounded-md w-fit">{primaryTag.name}</span>}
                  </div>
                </div>
                <div className="space-y-1">
                  <h3 className="text-sm font-medium">Tags</h3>
                  <div className="flex flex-wrap gap-1">
                    {postTags.map((tag) => (
                      <span key={tag.slug} className="text-xs bg-secondary text-secondary-foreground px-2 py-1 rounded-sm" title={tag.name}>
                        {tag.name}
                      </span>
                    ))}
                  </div>
                </div>
              </div>
            </section>
            <section>
              {/*  TODO: I should separate it in a component*/}
              <summary className="">
                <ol>
                  <li></li>
                  <li></li>
                  <li></li>
                </ol>
              </summary>
            </section>
          </aside>
          <section className="flex flex-col flex-1">
            <div className="space-y-4">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                {primaryTag && <span>{primaryTag.name}</span>}
                <span>•</span>
                <time dateTime={post.publishedAt.toISOString()}>
                  {post.publishedAt.toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                  })}
                </time>
              </div>
              <h1 className="text-3xl font-bold">{post.title}</h1>
              <div className="prose max-w-none">
                {/* Post content would go here */}
                <p>This is where the post content would be rendered.</p>
                <p>Post slug: {slug}</p>
                {primaryTag && (
                  <p>
                    Primary Category: {primaryTag.name} ({primaryTag.slug})
                  </p>
                )}
                <p>Tags: {postTags.map((tag) => tag.name).join(', ')}</p>
              </div>
            </div>
          </section>
        </div>
        <footer className="">{/* TODO: I do need this section? */}</footer>
      </article>
    </>
  );
}
