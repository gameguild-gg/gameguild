import React from 'react';
import Link from 'next/link';
import { BlogSidebarSection } from '@/components/blog/internal/blog-sidebar-section';
import { BlogSidebarSectionHeader } from '@/components/blog/internal/blog-sidebar-section-header';
import { BlogSidebarSectionList } from '@/components/blog/internal/blog-sidebar-section-list';
import { BlogSidebarSectionListItem } from '@/components/blog/internal/blog-sidebar-section-list-item';

type ListItemBase = {
  id: string;
  title: string;
  slug: string;
};

type BlogSidebarListProps<T extends ListItemBase> = {
  title: string;
  items: Array<T>;
  linkPrefix: string;
} & React.HTMLAttributes<HTMLElement>;

const BlogSidebarList = <T extends ListItemBase>({ id, className, title, items, linkPrefix, ...props }: BlogSidebarListProps<T>) => {
  if (items.length === 0) return null;
  return (
    <BlogSidebarSection id={id} className={className} {...props}>
      <BlogSidebarSectionHeader>{title}</BlogSidebarSectionHeader>
      <BlogSidebarSectionList>
        {items.map((item) => (
          <BlogSidebarSectionListItem key={item.id}>
            <Link href={`${linkPrefix}/${item.slug}`}>
              <a>{item.title}</a>
            </Link>
          </BlogSidebarSectionListItem>
        ))}
      </BlogSidebarSectionList>
    </BlogSidebarSection>
  );
};

export { BlogSidebarList };
