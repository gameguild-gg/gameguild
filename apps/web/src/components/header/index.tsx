import React, { PropsWithChildren } from 'react';
import Image from 'next/image';

import { cn } from '@game-guild/ui/lib/utils';

import {
  NavigationMenu,
  NavigationMenuContent,
  NavigationMenuItem,
  NavigationMenuList,
  NavigationMenuTrigger,
} from '@game-guild/ui/components/navigation-menu';
import { NavigationMenuLink } from '@radix-ui/react-navigation-menu';
import { NotificationDropdown } from '@/components/legacy/notifications';
import { UserProfileDropdown } from '@/components/legacy/profile';

type Props = PropsWithChildren<React.HTMLAttributes<HTMLDivElement>>;

const Header: React.FunctionComponent<Readonly<Props>> = ({ className, children, ...props }) => {
  return (
    <header
      className={cn(
        'w-full bg-white/10 dark:bg-slate-900/20 backdrop-blur-md border-b border-white/10 dark:border-slate-700/30 text-white sticky top-0 z-50 shadow-lg',
        className,
      )}
      {...props}
    >
      {/* Top Subtle Border */}
      <div className="h-px bg-gradient-to-r from-transparent via-white/20 dark:via-slate-400/30 to-transparent"></div>
      
      <div className="container mx-auto px-4 flex justify-between items-center py-4">
        <div className="flex space-x-8 items-center">
          <Image src="/assets/images/logo-text-2.png" width={135} height={46} className="my-auto mx-[10px]" alt="Logo" />
          <NavigationMenu>
            <NavigationMenuList>
              <NavigationMenuItem>
                <NavigationMenuTrigger className="text-slate-200 hover:text-blue-300 transition-colors duration-300 bg-transparent hover:bg-white/5 backdrop-blur-sm">
                  Getting started
                </NavigationMenuTrigger>
                <NavigationMenuContent>
                  <ul className="grid gap-3 p-6 md:w-[400px] lg:w-[500px] lg:grid-cols-[.75fr_1fr] bg-white/5 dark:bg-slate-900/80 backdrop-blur-xl border border-white/20 dark:border-slate-600/50 shadow-2xl">
                    <ListItem href="/docs" title="Introduction">
                      Re-usable components built using Radix UI and Tailwind CSS.
                    </ListItem>
                    <ListItem href="/docs/installation" title="Installation">
                      How to install dependencies and structure your app.
                    </ListItem>
                    <ListItem href="/docs/primitives/typography" title="Typography">
                      Styles for headings, paragraphs, lists...etc
                    </ListItem>
                  </ul>
                </NavigationMenuContent>
              </NavigationMenuItem>
              <NavigationMenuItem>
                <NavigationMenuTrigger className="text-slate-200 hover:text-purple-300 transition-colors duration-300 bg-transparent hover:bg-white/5 backdrop-blur-sm">
                  Courses
                </NavigationMenuTrigger>
                <NavigationMenuContent>
                  <ul className="grid gap-3 p-6 md:w-[400px] lg:w-[500px] lg:grid-cols-[.75fr_1fr] bg-white/5 dark:bg-slate-900/80 backdrop-blur-xl border border-white/20 dark:border-slate-600/50 shadow-2xl">
                    <ListItem href="/courses" title="Courses">
                      Learn about our learning pathways and start your journey.
                    </ListItem>
                    <ListItem href="/courses/catalog" title="Course Catalog">
                      Browse and filter through all available courses.
                    </ListItem>
                    <ListItem href="/tracks" title="Learning Tracks">
                      Explore structured learning paths from beginner to expert.
                    </ListItem>
                  </ul>
                </NavigationMenuContent>
              </NavigationMenuItem>
              <NavigationMenuItem>
                <NavigationMenuTrigger className="text-slate-200 hover:text-green-300 transition-colors duration-300 bg-transparent hover:bg-white/5 backdrop-blur-sm">
                  Jobs
                </NavigationMenuTrigger>
                <NavigationMenuContent>
                  <ul className="grid gap-3 p-6 md:w-[400px] lg:w-[500px] lg:grid-cols-[.75fr_1fr] bg-white/5 dark:bg-slate-900/80 backdrop-blur-xl border border-white/20 dark:border-slate-600/50 shadow-2xl">
                    <ListItem href="/jobs" title="Job Board">
                      Find and apply for jobs.
                    </ListItem>
                  </ul>
                </NavigationMenuContent>
              </NavigationMenuItem>
              <NavigationMenuItem>
                <NavigationMenuTrigger className="text-slate-200 hover:text-blue-300 transition-colors duration-300 bg-transparent hover:bg-white/5 backdrop-blur-sm">
                  Blogs
                </NavigationMenuTrigger>
                <NavigationMenuContent>
                  <ul className="grid gap-3 p-6 md:w-[400px] lg:w-[500px] lg:grid-cols-[.75fr_1fr] bg-white/5 dark:bg-slate-900/80 backdrop-blur-xl border border-white/20 dark:border-slate-600/50 shadow-2xl">
                    <ListItem href="/blog" title="Blog">
                      Read the latest articles and news.
                    </ListItem>
                  </ul>
                </NavigationMenuContent>
              </NavigationMenuItem>
            </NavigationMenuList>
          </NavigationMenu>
        </div>
        <div className="flex items-center space-x-4">
          <NotificationDropdown />
          <UserProfileDropdown />
        </div>
        {children}
      </div>
      
      {/* Bottom Beautiful Border */}
      <div className="h-px bg-gradient-to-r from-transparent via-white/20 dark:via-slate-400/30 to-transparent"></div>
    </header>
  );
};

const ListItem = React.forwardRef<React.ElementRef<'a'>, React.ComponentPropsWithoutRef<'a'>>(({ className, title, children, ...props }, ref) => {
  return (
    <li>
      <NavigationMenuLink asChild>
        <a
          ref={ref}
          className={cn(
            'block select-none space-y-1 rounded-md p-3 leading-none no-underline outline-none transition-all duration-200 hover:bg-white/10 dark:hover:bg-slate-800/50 text-slate-200 hover:text-white border border-transparent hover:border-white/20 dark:hover:border-blue-400/30 backdrop-blur-sm',
            className,
          )}
          {...props}
        >
          <div className="text-sm font-medium leading-none text-blue-300">{title}</div>
          <p className="line-clamp-2 text-sm leading-snug text-slate-300">{children}</p>
        </a>
      </NavigationMenuLink>
    </li>
  );
});
ListItem.displayName = 'ListItem';

export default Header;
