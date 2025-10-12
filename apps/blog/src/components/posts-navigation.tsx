'use client';

import React from 'react';
import Link from 'next/link';
import { ChevronLeftIcon, ChevronRightIcon } from 'lucide-react';
import { Button } from '@gameguild/ui/components/button';
import { Post } from '@/app/actions/posts';
import { useBlogParams } from '@/hooks/use-blog-params';

interface PostsNavigationProps {
  posts: Post[];
}

const MONTH_NAMES = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

export const PostsNavigation = ({ posts }: PostsNavigationProps): React.JSX.Element | null => {
  const { currentYear: year, currentMonth: month, currentDay: day, currentSlug: slug } = useBlogParams();

  // Determine navigation context and items
  let prevItem: { label: string; href: string } | null = null;
  let nextItem: { label: string; href: string } | null = null;

  // Sort all posts chronologically
  const sortedPosts = [...posts].sort((a, b) => a.publishedAt.getTime() - b.publishedAt.getTime());

  if (slug && year && month && day) {
    // Post level navigation - find chronologically previous/next posts
    const currentPostIndex = sortedPosts.findIndex((post) => post.slug === slug);

    if (currentPostIndex > 0) {
      const prevPost = sortedPosts[currentPostIndex - 1];
      const prevDate = prevPost.publishedAt;
      prevItem = {
        label: `Previous: ${prevPost.title}`,
        href: `/${prevDate.getFullYear()}/${(prevDate.getMonth() + 1).toString().padStart(2, '0')}/${prevDate.getDate().toString().padStart(2, '0')}/${prevPost.slug}`,
      };
    }

    if (currentPostIndex < sortedPosts.length - 1) {
      const nextPost = sortedPosts[currentPostIndex + 1];
      const nextDate = nextPost.publishedAt;
      nextItem = {
        label: `Next: ${nextPost.title}`,
        href: `/${nextDate.getFullYear()}/${(nextDate.getMonth() + 1).toString().padStart(2, '0')}/${nextDate.getDate().toString().padStart(2, '0')}/${nextPost.slug}`,
      };
    }
  } else if (year && month && day) {
    // Day level navigation - find previous/next days with posts
    const allDays = [
      ...new Set(
        posts.map((post) => ({
          year: post.publishedAt.getFullYear(),
          month: post.publishedAt.getMonth() + 1,
          day: post.publishedAt.getDate(),
          date: new Date(post.publishedAt.getFullYear(), post.publishedAt.getMonth(), post.publishedAt.getDate()),
        })),
      ),
    ].sort((a, b) => a.date.getTime() - b.date.getTime());

    const currentDayIndex = allDays.findIndex((dayItem) => dayItem.year === year && dayItem.month === month && dayItem.day === day);

    if (currentDayIndex > 0) {
      const prevDay = allDays[currentDayIndex - 1];
      prevItem = {
        label: `Previous: ${prevDay.day} ${MONTH_NAMES[prevDay.month - 1]} ${prevDay.year}`,
        href: `/${prevDay.year}/${prevDay.month.toString().padStart(2, '0')}/${prevDay.day.toString().padStart(2, '0')}`,
      };
    }

    if (currentDayIndex < allDays.length - 1) {
      const nextDay = allDays[currentDayIndex + 1];
      nextItem = {
        label: `Next: ${nextDay.day} ${MONTH_NAMES[nextDay.month - 1]} ${nextDay.year}`,
        href: `/${nextDay.year}/${nextDay.month.toString().padStart(2, '0')}/${nextDay.day.toString().padStart(2, '0')}`,
      };
    }
  } else if (year && month) {
    // Month level navigation
    const allMonths = [
      ...new Set(
        posts.map((post) => ({
          year: post.publishedAt.getFullYear(),
          month: post.publishedAt.getMonth() + 1,
          date: new Date(post.publishedAt.getFullYear(), post.publishedAt.getMonth()),
        })),
      ),
    ].sort((a, b) => a.date.getTime() - b.date.getTime());

    const currentMonthIndex = allMonths.findIndex((monthItem) => monthItem.year === year && monthItem.month === month);

    if (currentMonthIndex > 0) {
      const prevMonth = allMonths[currentMonthIndex - 1];
      prevItem = {
        label: `Previous: ${MONTH_NAMES[prevMonth.month - 1]} ${prevMonth.year}`,
        href: `/${prevMonth.year}/${prevMonth.month.toString().padStart(2, '0')}`,
      };
    }

    if (currentMonthIndex < allMonths.length - 1) {
      const nextMonth = allMonths[currentMonthIndex + 1];
      nextItem = {
        label: `Next: ${MONTH_NAMES[nextMonth.month - 1]} ${nextMonth.year}`,
        href: `/${nextMonth.year}/${nextMonth.month.toString().padStart(2, '0')}`,
      };
    }
  } else if (year) {
    // Year level navigation
    const allYears = [...new Set(posts.map((post) => post.publishedAt.getFullYear()))].sort((a, b) => a - b);

    const currentYearIndex = allYears.findIndex((yearItem) => yearItem === year);

    if (currentYearIndex > 0) {
      const prevYear = allYears[currentYearIndex - 1];
      prevItem = {
        label: `Previous: ${prevYear}`,
        href: `/${prevYear}`,
      };
    }

    if (currentYearIndex < allYears.length - 1) {
      const nextYear = allYears[currentYearIndex + 1];
      nextItem = {
        label: `Next: ${nextYear}`,
        href: `/${nextYear}`,
      };
    }
  }

  // Don't render if no navigation items
  if (!prevItem && !nextItem) return null;

  return (
    <nav className="flex justify-between items-center mt-8 pt-6 border-t border-border">
      <div className="flex-1">
        {prevItem ? (
          <Button asChild variant="outline" className="group">
            <Link href={prevItem.href} className="flex items-center gap-2">
              <ChevronLeftIcon className="size-4 group-hover:-translate-x-0.5 transition-transform" />
              <span className="truncate max-w-[200px] sm:max-w-[300px]">{prevItem.label}</span>
            </Link>
          </Button>
        ) : (
          <div /> // Empty div to maintain spacing
        )}
      </div>

      <div className="flex-1 flex justify-end">
        {nextItem ? (
          <Button asChild variant="outline" className="group">
            <Link href={nextItem.href} className="flex items-center gap-2">
              <span className="truncate max-w-[200px] sm:max-w-[300px]">{nextItem.label}</span>
              <ChevronRightIcon className="size-4 group-hover:translate-x-0.5 transition-transform" />
            </Link>
          </Button>
        ) : (
          <div /> // Empty div to maintain spacing
        )}
      </div>
    </nav>
  );
};
