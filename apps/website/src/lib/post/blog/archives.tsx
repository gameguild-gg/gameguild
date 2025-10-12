import React from 'react';
import { Post } from '@/lib/post/post';
import { BlogSidebarList } from '@/components/blog/internal/blog-sidebar-list';

type ArchivesProps = {
  posts: Array<Post>;
} & React.HtmlHTMLAttributes<HTMLElement>;

const Archives: React.FunctionComponent<ArchivesProps> = ({ posts, ...props }) => {
  return <BlogSidebarList id="archives" title="Archives" items={posts} linkPrefix="/blog/archive" {...props} />;
};

export { Archives };
