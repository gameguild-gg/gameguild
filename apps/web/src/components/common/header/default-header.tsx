'use client';

import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { Menu, X } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import React, { PropsWithChildren, useState } from 'react';

import { UserProfile } from '@/components/common/header/common/ui/user-profile';
import { NavigationMenu, NavigationMenuContent, NavigationMenuItem, NavigationMenuList, NavigationMenuTrigger } from '@/components/ui/navigation-menu';
import { NavigationMenuLink } from '@radix-ui/react-navigation-menu';

type Props = PropsWithChildren<React.HTMLAttributes<HTMLDivElement>>;

const Header: React.FunctionComponent<Readonly<Props>> = ({ className, children, ...props }) => {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const toggleMobileMenu = () => {
    setIsMobileMenuOpen(!isMobileMenuOpen);
  };

  return (
    <header className={cn('w-full bg-white/10 dark:bg-slate-900/20 backdrop-blur-md border-b border-white/10 dark:border-slate-700/30 text-white sticky top-0 z-50 shadow-lg', className)} {...props}>
      {/* Top Subtle Border */}
      <div className="h-px bg-gradient-to-r from-transparent via-white/20 dark:via-slate-400/30 to-transparent"></div>

      <div className="container mx-auto px-4 flex justify-between items-center py-4">
        <div className="flex space-x-2 md:space-x-8 items-center">
          <Image src="/assets/images/logo-text-2.png" width={135} height={46} className="my-auto mx-[10px] flex-shrink-0 drop-shadow-[0_0_2px_rgba(255,255,255,0.8)] filter" alt="Logo" />

          {/* Desktop Navigation */}
          <NavigationMenu className="hidden md:flex">
            <NavigationMenuList>

              <NavigationMenuItem>
                <NavigationMenuTrigger className="text-slate-200 hover:text-purple-300 transition-colors duration-300 bg-transparent hover:bg-white/5 backdrop-blur-sm">Courses</NavigationMenuTrigger>
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
                <NavigationMenuTrigger className="text-slate-200 hover:text-green-300 transition-colors duration-300 bg-transparent hover:bg-white/5 backdrop-blur-sm">Testing Lab</NavigationMenuTrigger>
                <NavigationMenuContent>
                  <ul className="grid gap-3 p-6 md:w-[400px] lg:w-[500px] lg:grid-cols-[.75fr_1fr] bg-white/5 dark:bg-slate-900/80 backdrop-blur-xl border border-white/20 dark:border-slate-600/50 shadow-2xl">
                    <ListItem href="/testing-lab" title="Testing Lab">
                      Access the game testing platform and submit feedback.
                    </ListItem>
                    <ListItem href="/testing-lab/sessions" title="Testing Sessions">
                      View and join active testing sessions.
                    </ListItem>
                    <ListItem href="/dashboard/testing-lab/overview" title="Testing Overview">
                      Manage testing lab dashboard and analytics.
                    </ListItem>
                  </ul>
                </NavigationMenuContent>
              </NavigationMenuItem>

              <NavigationMenuItem>
                <NavigationMenuTrigger className="text-slate-200 hover:text-blue-300 transition-colors duration-300 bg-transparent hover:bg-white/5 backdrop-blur-sm">Institutional</NavigationMenuTrigger>
                <NavigationMenuContent>
                  <ul className="grid gap-3 p-6 md:w-[400px] lg:w-[500px] lg:grid-cols-[.75fr_1fr] bg-white/5 dark:bg-slate-900/80 backdrop-blur-xl border border-white/20 dark:border-slate-600/50 shadow-2xl">
                    <ListItem href="/contributors" title="Contributors">
                      Meet the people who make this platform possible.
                    </ListItem>
                    <ListItem href="/licenses" title="Licenses">
                      View our open source licenses and legal information.
                    </ListItem>
                    <ListItem href="/terms-of-service" title="Terms of Service">
                      Read our terms and conditions of use.
                    </ListItem>
                    <ListItem href="/polices/privacy" title="Privacy">
                      Learn about our privacy policy and data protection.
                    </ListItem>
                    <ListItem href="/contact" title="Contact">
                      Get in touch with our team for support or inquiries.
                    </ListItem>
                    <ListItem href="/about" title="About Us">
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
          {/* Mobile Menu Button */}
          <Button
            variant="ghost"
            size="lg"
            className="md:hidden text-slate-200 hover:text-white hover:bg-white/5 flex items-center justify-center p-1"
            onClick={toggleMobileMenu}
          >
            <div className="relative w-12 h-12 flex items-center justify-center">
              <Menu className={`absolute h-20 w-20 transition-all duration-300 ease-in-out ${isMobileMenuOpen ? 'opacity-0 rotate-180 scale-75' : 'opacity-100 rotate-0 scale-100'
                }`} />
              <X className={`absolute h-20 w-20 transition-all duration-300 ease-in-out ${isMobileMenuOpen ? 'opacity-100 rotate-0 scale-100' : 'opacity-0 rotate-180 scale-75'
                }`} />
            </div>
          </Button>
          <UserProfile />
        </div>
        {children}
      </div>

      {/* Mobile Menu */}
      <div className={`md:hidden bg-white/5 dark:bg-slate-900/80 backdrop-blur-xl border-t border-white/20 dark:border-slate-600/50 transition-all duration-300 ease-in-out overflow-hidden ${isMobileMenuOpen
          ? 'max-h-[32rem] opacity-100 transform translate-y-0'
          : 'max-h-0 opacity-0 transform -translate-y-2'
        }`}>
        <div className="container mx-auto px-4 py-4 space-y-2">

          <Link href="/courses" className="block px-3 py-2 text-slate-200 hover:text-white hover:bg-white/10 rounded-md transition-colors">
            Courses
          </Link>
          <Link href="/testing-lab" className="block px-3 py-2 text-slate-200 hover:text-white hover:bg-white/10 rounded-md transition-colors">
            Testing Lab
          </Link>
          
          {/* Institutional Links */}
          <div className="border-t border-white/20 pt-2 mt-4">
            <div className="text-xs text-slate-400 px-3 py-1 uppercase tracking-wider">Institutional</div>
            <Link href="/contributors" className="block px-3 py-2 text-slate-200 hover:text-white hover:bg-white/10 rounded-md transition-colors">
              Contributors
            </Link>
            <Link href="/licenses" className="block px-3 py-2 text-slate-200 hover:text-white hover:bg-white/10 rounded-md transition-colors">
              Licenses
            </Link>
            <Link href="/terms-of-service" className="block px-3 py-2 text-slate-200 hover:text-white hover:bg-white/10 rounded-md transition-colors">
              Terms of Service
            </Link>
            <Link href="/polices/privacy" className="block px-3 py-2 text-slate-200 hover:text-white hover:bg-white/10 rounded-md transition-colors">
              Privacy
            </Link>
            <Link href="/contact" className="block px-3 py-2 text-slate-200 hover:text-white hover:bg-white/10 rounded-md transition-colors">
              Contact
            </Link>
            <Link href="/about" className="block px-3 py-2 text-slate-200 hover:text-white hover:bg-white/10 rounded-md transition-colors">
              About Us
            </Link>
          </div>

        </div>
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
