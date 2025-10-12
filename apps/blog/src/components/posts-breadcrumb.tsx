'use client';

import React, { Fragment } from 'react';
import Link from 'next/link';
import { HomeIcon, Slash } from 'lucide-react';
import { Breadcrumb, BreadcrumbItem, BreadcrumbLink, BreadcrumbList, BreadcrumbPage, BreadcrumbSeparator } from '@gameguild/ui/components/breadcrumb';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@gameguild/ui/components/dropdown-menu';
import { Post } from '@/app/actions/posts';
import { useBlogParams } from '@/hooks/use-blog-params';

interface PostsBreadcrumbProps {
  posts: Post[];
}

const MONTH_NAMES = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

export const PostsBreadcrumb = ({ posts }: PostsBreadcrumbProps): React.JSX.Element => {
  const { currentYear: year, currentMonth: month, currentDay: day, currentSlug: slug } = useBlogParams();

  // Extract unique values from posts client-side
  const availableYears = [...new Set(posts.map((post) => post.publishedAt.getFullYear()))].sort((a, b) => b - a);
  const availableMonths = [...new Set(posts.map((post) => post.publishedAt.getMonth() + 1))].sort((a, b) => a - b);
  const availableDays = [...new Set(posts.map((post) => post.publishedAt.getDate()))].sort((a, b) => a - b);
  const availablePosts = posts.map((post) => ({ slug: post.slug, title: post.title }));

  // Find current post
  const currentPost = slug ? posts.find((post) => post.slug === slug) : undefined;

  const breadcrumbItems = [
    {
      label: <HomeIcon className="size-4" />,
      href: '/',
      current: null,
      availableItems: [],
      isExternal: true,
    },
    {
      label: 'Blog',
      href: '/',
      current: null,
      availableItems: [],
      isExternal: false,
    },
    ...(year
      ? [
          {
            label: year.toString(),
            href: `/${year}`,
            current: year,
            availableItems: availableYears.map((yearItem) => ({
              key: yearItem,
              label: yearItem.toString(),
              href: `/${yearItem}`,
            })),
            isExternal: false,
          },
        ]
      : []),
    ...(month
      ? [
          {
            label: MONTH_NAMES[month - 1],
            href: `/${year}/${month.toString().padStart(2, '0')}`,
            current: month,
            availableItems: availableMonths.map((monthItem) => ({
              key: monthItem,
              label: MONTH_NAMES[monthItem - 1],
              href: `/${year}/${monthItem.toString().padStart(2, '0')}`,
            })),
            isExternal: false,
          },
        ]
      : []),
    ...(day
      ? [
          {
            label: day.toString(),
            href: `/${year}/${month?.toString().padStart(2, '0')}/${day.toString().padStart(2, '0')}`,
            current: day,
            availableItems: availableDays.map((dayItem) => ({
              key: dayItem,
              label: dayItem.toString(),
              href: `/${year}/${month?.toString().padStart(2, '0')}/${dayItem.toString().padStart(2, '0')}`,
            })),
            isExternal: false,
          },
        ]
      : []),
    ...(currentPost && slug
      ? [
          {
            label: currentPost.title,
            href: `/${year}/${month?.toString().padStart(2, '0')}/${day?.toString().padStart(2, '0')}/${currentPost.slug}`,
            current: currentPost,
            availableItems: availablePosts.map((post) => ({
              key: post.slug,
              label: post.title,
              href: `/${year}/${month?.toString().padStart(2, '0')}/${day?.toString().padStart(2, '0')}/${post.slug}`,
            })),
            isExternal: false,
            isPost: true,
          },
        ]
      : []),
  ];

  return (
    <Breadcrumb>
      <BreadcrumbList className="sm:gap-2">
        {breadcrumbItems.map((item, index) => (
          <Fragment key={`${item.label}-${index}`}>
            {index > 0 && (
              <BreadcrumbSeparator>
                <Slash />
              </BreadcrumbSeparator>
            )}
            <BreadcrumbItem key={`${item.label}-${index}`}>
              {item.availableItems.length > 0 ? (
                <DropdownMenu>
                  <DropdownMenuTrigger className={`flex items-center gap-1 ${item.isPost ? 'truncate' : ''}`}>
                    <span className={item.isPost ? 'truncate' : ''} title={item.isPost ? item.label : undefined}>
                      {item.label}
                    </span>
                    {/*<ChevronDownIcon className={`size-4 ${item.isPost ? 'shrink-0' : ''}`} />*/}
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="start" className={item.isPost ? 'overflow-y-auto' : ''}>
                    {item.availableItems.map((availableItem) => (
                      <DropdownMenuItem key={availableItem.key} asChild>
                        <Link href={availableItem.href} className={item.isPost ? 'truncate' : ''}>
                          {availableItem.label}
                        </Link>
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuContent>
                </DropdownMenu>
              ) : item.current === null ? (
                <BreadcrumbLink asChild>{item.isExternal ? <a href={item.href}>{item.label}</a> : <Link href={item.href}>{item.label}</Link>}</BreadcrumbLink>
              ) : item.isPost ? (
                <BreadcrumbPage>{item.label}</BreadcrumbPage>
              ) : (
                <BreadcrumbLink asChild>
                  <Link href={item.href}>{item.label}</Link>
                </BreadcrumbLink>
              )}
            </BreadcrumbItem>
          </Fragment>
        ))}
      </BreadcrumbList>
    </Breadcrumb>
  );
};
