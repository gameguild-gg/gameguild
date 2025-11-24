import React from 'react';
import { BlogSidebarList } from '@/components/blog/internal/blog-sidebar-list';
import { Post } from '@/lib/post/post';

type CategoriesProps = {
  posts: Array<Post>;
} & React.HtmlHTMLAttributes<HTMLElement>;

const Categories: React.FunctionComponent<CategoriesProps> = ({ posts, ...props }) => {
  return <BlogSidebarList id="categories" title="Categories" items={posts} linkPrefix="/blog/category" {...props} />;
};

export { Categories };
