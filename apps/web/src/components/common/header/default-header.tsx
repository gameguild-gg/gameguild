import { cn } from '@/lib/utils';
import Image from 'next/image';
import Link from 'next/link';
import React, { PropsWithChildren } from 'react';

import { UserProfile } from '@/components/common/header/common/ui/user-profile';
import { NavigationMenu, NavigationMenuContent, NavigationMenuItem, NavigationMenuList, NavigationMenuTrigger, NavigationMenuLink } from '@/components/ui/navigation-menu';
import { MobileMenu } from './mobile-menu';
import { ThemeToggle } from '@/components/theme/theme-toggle';
import { GitHubForkButton } from './github-fork-button';
import type { Session } from 'next-auth';
import type { UserResponseDto } from '@/lib/api/generated/types.gen';

type Props = PropsWithChildren<React.HTMLAttributes<HTMLDivElement> & {
  session?: Session | null;
  userProfile?: UserResponseDto | null;
}>;

const Header: React.FunctionComponent<Readonly<Props>> = ({ className, children, session = null, userProfile = null, ...props }) => {

  return (
    <header className={cn('w-full bg-white/10 dark:bg-slate-900/20 backdrop-blur-md border-b border-white/10 dark:border-slate-700/30 text-slate-900 dark:text-white sticky top-0 z-50 shadow-lg', className)} {...props}>
      {/* Top Subtle Border */}
      <div className="h-px bg-gradient-to-r from-transparent via-white/20 dark:via-slate-400/30 to-transparent"></div>

      <div className="container mx-auto px-4 flex justify-between items-center py-4">
        <div className="flex space-x-2 md:space-x-8 items-center">
          <Image src="/assets/images/logo-text-2.png" width={135} height={46} className="my-auto mx-[10px] flex-shrink-0 drop-shadow-[0_0_2px_rgba(255,255,255,0.8)] filter" alt="Logo" />

          {/* Desktop Navigation */}
          <NavigationMenu className="hidden md:flex">
            <NavigationMenuList>

              <NavigationMenuItem>
                <NavigationMenuTrigger className="text-black dark:text-slate-200 hover:text-purple-600 dark:hover:text-purple-300 transition-colors duration-300 bg-white/90 hover:bg-white dark:bg-slate-800/90 dark:hover:bg-slate-800 backdrop-blur-sm">Courses</NavigationMenuTrigger>
                <NavigationMenuContent>
                  <ul className="grid gap-3 p-6 md:w-[400px] lg:w-[500px] lg:grid-cols-[.75fr_1fr] bg-white/5 dark:bg-slate-900/80 backdrop-blur-xl border border-white/20 dark:border-slate-600/50 shadow-2xl">
                    <ListItem href="/courses/catalog" title="Courses">
                      Learn about our learning pathways and start your journey.
                    </ListItem>
                    <a
                      href="#"
                      className="block select-none space-y-1 rounded-md p-3 leading-none no-underline outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground"
                      data-github-issue="true"
                      data-route="/tracks"
                    >
                      <div className="text-sm font-medium leading-none">Learning Tracks</div>
                      <p className="line-clamp-2 text-sm leading-snug text-muted-foreground">
                        Explore structured learning paths from beginner to expert.
                      </p>
                    </a>
                  </ul>
                </NavigationMenuContent>
              </NavigationMenuItem>
              <NavigationMenuItem>
                <a 
                  href="#" 
                  data-github-issue="true" 
                  data-route="/testing-lab"
                  className="group inline-flex h-9 w-max items-center justify-center rounded-md px-4 py-2 text-sm font-medium focus:bg-accent focus:text-accent-foreground disabled:pointer-events-none disabled:opacity-50 data-[state=open]:hover:bg-accent data-[state=open]:text-accent-foreground data-[state=open]:focus:bg-accent data-[state=open]:bg-accent/50 focus-visible:ring-ring/50 outline-none focus-visible:ring-[3px] focus-visible:outline-1 group text-black dark:text-slate-200 hover:text-green-600 dark:hover:text-green-300 transition-colors duration-300 bg-white/90 hover:bg-white dark:bg-slate-800/90 dark:hover:bg-slate-800 backdrop-blur-sm"
                >
                  Testing Lab
                </a>
              </NavigationMenuItem>

              <NavigationMenuItem>
                <NavigationMenuTrigger className="text-black dark:text-slate-200 hover:text-blue-600 dark:hover:text-blue-300 transition-colors duration-300 bg-white/90 hover:bg-white dark:bg-slate-800/90 dark:hover:bg-slate-800 backdrop-blur-sm">Institutional</NavigationMenuTrigger>
                <NavigationMenuContent>
                  <ul className="grid gap-3 p-6 md:w-[400px] lg:w-[500px] lg:grid-cols-[.75fr_1fr] bg-white/5 dark:bg-slate-900/80 backdrop-blur-xl border border-white/20 dark:border-slate-600/50 shadow-2xl">
                    <ListItem href="/contributors" title="Contributors">
                      Meet the people who make this platform possible.
                    </ListItem>
                    <ListItem href="/licenses" title="Licenses">
                      View our open source licenses and legal information.
                    </ListItem>
                    <ListItem href="#" title="Terms of Service">
                      Read our terms and conditions of use.
                    </ListItem>
                    <ListItem href="#" title="Privacy">
                      Learn about our privacy policy and data protection.
                    </ListItem>
                    <ListItem href="#" title="Contact">
                      Get in touch with our team for support or inquiries.
                    </ListItem>
                    <ListItem href="#" title="About Us">
                      Discover our mission, vision, and company story.
                    </ListItem>
                  </ul>
                </NavigationMenuContent>
              </NavigationMenuItem>

            </NavigationMenuList>
          </NavigationMenu>
        </div>
        <div className="flex items-center space-x-2 md:space-x-4 flex-shrink-0">
          {/*<NotificationDropdown />*/}
          <GitHubForkButton />
          <ThemeToggle />
          <MobileMenu>
            <Link href="/courses/catalog" className="block px-3 py-2 text-black dark:text-slate-200 hover:text-black dark:hover:text-white bg-white/90 hover:bg-white dark:bg-slate-800/90 dark:hover:bg-slate-800 rounded-md transition-colors">
              Courses
            </Link>
            <a
              href="#"
              className="block px-3 py-2 text-black dark:text-slate-200 hover:text-black dark:hover:text-white bg-white/90 hover:bg-white dark:bg-slate-800/90 dark:hover:bg-slate-800 rounded-md transition-colors"
              data-github-issue="true"
              data-route="/tracks"
            >
              Learning Tracks
            </a>
            <Link href="/testing-lab" className="block px-3 py-2 text-black dark:text-slate-200 hover:text-black dark:hover:text-white bg-white/90 hover:bg-white dark:bg-slate-800/90 dark:hover:bg-slate-800 rounded-md transition-colors">
              Testing Lab
            </Link>
            <Link href="#" className="block px-3 py-2 text-black dark:text-slate-200 hover:text-black dark:hover:text-white bg-white/90 hover:bg-white dark:bg-slate-800/90 dark:hover:bg-slate-800 rounded-md transition-colors">
              About
            </Link>
            <Link href="/contact" className="block px-3 py-2 text-slate-700 dark:text-slate-200 hover:text-slate-900 dark:hover:text-white hover:bg-black/10 dark:hover:bg-white/10 rounded-md transition-colors">
              Contact
            </Link>
          </MobileMenu>
          <UserProfile session={session} userProfile={userProfile} />
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
            'block select-none space-y-1 rounded-md p-3 leading-none no-underline outline-none transition-all duration-200 hover:bg-black/10 dark:hover:bg-slate-800/50 text-slate-700 dark:text-slate-200 hover:text-slate-900 dark:hover:text-white border border-transparent hover:border-black/20 dark:hover:border-blue-400/30 backdrop-blur-sm',
            className,
          )}
          {...props}
        >
          <div className="text-sm font-medium leading-none text-blue-600 dark:text-blue-300">{title}</div>
          <p className="line-clamp-2 text-sm leading-snug text-slate-600 dark:text-slate-300">{children}</p>
        </a>
      </NavigationMenuLink>
    </li>
  );
});
ListItem.displayName = 'ListItem';

export default Header;
