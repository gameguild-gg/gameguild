import React, { PropsWithChildren } from 'react';
import Link from 'next/link';
import { cn } from '@/lib/utils';
import { NewsletterSection } from './newsletter-section';
import { CommunityInfo } from './community-info';
import { NavigationLinks } from './navigation-links';
import { SocialMediaLinks } from './social-media-links';

type Props = PropsWithChildren<React.HTMLAttributes<HTMLDivElement>>;

const Footer: React.FunctionComponent<Readonly<Props>> = ({ className, children, ...props }) => {
  return (
    <footer className={cn('w-full bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900 text-white', className)} {...props}>
      {/* Top Subtle Border */}
      <div className="h-0.5 bg-gradient-to-r from-transparent via-slate-600/50 to-transparent"></div>

      {/* Newsletter Section */}
      <NewsletterSection />

      {/* Main Footer Content */}
      <div className="container mx-auto max-w-6xl px-4 py-12">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-8">
          {/* Community Info */}
          <CommunityInfo />

          {/* Navigation Links */}
          <NavigationLinks />
        </div>

        {/* Social Media & Bottom Links */}
        <div className="mt-12 pt-8 border-t border-slate-700/50">
          <div className="flex flex-col md:flex-row justify-between items-center gap-6">
            <SocialMediaLinks />

            <div className="flex flex-wrap gap-6 text-sm text-slate-400">
              <Link href="/terms" className="hover:text-blue-400 transition-colors duration-300">
                Terms of Service
              </Link>
              <Link href="/privacy" className="hover:text-blue-400 transition-colors duration-300">
                Privacy Policy
              </Link>
              <Link href="/cookies" className="hover:text-blue-400 transition-colors duration-300">
                Cookies
              </Link>
            </div>
          </div>

          <div className="mt-8 pt-6 border-t border-slate-700/50 text-center text-sm text-slate-500">
            © {new Date().getFullYear()} Game Guild Inc. All rights reserved.
          </div>
        </div>
      </div>

      {/* Bottom Beautiful Border */}
      <div className="h-1 bg-gradient-to-r from-green-500 via-blue-500 to-purple-500"></div>

      {children}
    </footer>
  );
};

export default Footer;
