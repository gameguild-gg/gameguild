import React from 'react';
import { BlogSidebarList } from '@/components/blog/internal/blog-sidebar-list';
import { Post } from '@/lib/post/post';

type RecentPostsProps = {
  posts: Array<Post>;
} & React.HtmlHTMLAttributes<HTMLElement>;

const RecentPosts: React.FunctionComponent<RecentPostsProps> = ({ posts, ...props }) => {
  return <BlogSidebarList id="recent-posts" title="Recent Posts" items={posts} linkPrefix="/blog" {...props} />;
};

export { RecentPosts };
