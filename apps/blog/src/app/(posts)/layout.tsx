import React, { PropsWithChildren } from 'react';
import { getPublishedPosts } from '@/app/actions/posts';
import { PostsBreadcrumb } from '@/components/posts-breadcrumb';
import { PostsNavigation } from '@/components/posts-navigation';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  const posts = await getPublishedPosts();

  return (
    <>
      <div className="flex flex-col flex-1 items-center">
        <div className="flex flex-row flex-1 container gap-8 p-8">
          <div className="flex flex-col flex-1 gap-4">
            <div className="flex flex-col flex-0">
              <PostsBreadcrumb posts={posts} />
            </div>
            <div className="flex flex-col flex-1">
              {/* TODO */}
              {children}
            </div>
            <div className="flex flex-col flex-0">
              <PostsNavigation posts={posts} />
            </div>
          </div>
          {/*TODO: Move to a collapsable sidebar*/}
          <aside className="flex flex-col flex-1 gap-4 max-w-60">
            {/*  TODO: I should separate it in a component*/}
            <section id="search">
              <search>
                <form>
                  <input type="search" />
                  <button type="submit">Search</button>
                </form>
              </search>
            </section>
            {/*  TODO: I should separate it in a component*/}
            <section id="recent-posts">
              <h2>Recent Posts</h2>
              <ul>
                <li></li>
                <li></li>
                <li></li>
              </ul>
            </section>
            {/*  TODO: I should separate it in a component*/}
            <section id="archives">
              <h2>Archives</h2>
              <ul>
                <li></li>
                <li></li>
                <li></li>
              </ul>
            </section>
            {/*  TODO: I should separate it in a component*/}
            <section id="categories">
              <h2>Categories</h2>
              <ul>
                <li></li>
                <li></li>
                <li></li>
              </ul>
            </section>
          </aside>
        </div>
      </div>
    </>
  );
}
