import React, { PropsWithChildren } from 'react';
import { cn } from '@/lib/utils';
import { CommunityInfo } from './common/ui/community-info';
import { NavigationLinks } from './common/ui/navigation-links';
import { SocialMediaLinks } from './common/ui/social-media-links';
import { LegalLinks } from '@/components/common/footer/common/ui/legal-links';
import { Github, Twitter, Youtube, LucideIcon } from 'lucide-react';
import { FaDiscord } from 'react-icons/fa';
import { IconType } from 'react-icons';

type Props = PropsWithChildren<React.HTMLAttributes<HTMLDivElement>>;

const socialLinks = [
  {
    href: 'https://discord.gg/9CdJeQ2XKB',
    icon: FaDiscord,
    label: 'Discord',
    hoverColor: 'hover:border-indigo-400/50 hover:shadow-indigo-500/10',
    iconHover: 'group-hover:text-indigo-400',
  },
  {
    href: 'https://twitter.com/gameguild_gg',
    icon: Twitter,
    label: 'Twitter',
    hoverColor: 'hover:border-blue-400/50 hover:shadow-blue-500/10',
    iconHover: 'group-hover:text-blue-400',
  },
  {
    href: 'https://github.com/gameguild-gg',
    icon: Github,
    label: 'GitHub',
    hoverColor: 'hover:border-purple-400/50 hover:shadow-purple-500/10',
    iconHover: 'group-hover:text-purple-400',
  },
  {
    href: 'https://youtube.com/@gameguild',
    icon: Youtube,
    label: 'YouTube',
    hoverColor: 'hover:border-red-400/50 hover:shadow-red-500/10',
    iconHover: 'group-hover:text-red-400',
  },
];

const Footer: React.FunctionComponent<Readonly<Props>> = ({ className, children, ...props }) => {
  return (
    <footer className={cn('w-full bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900 text-white', className)} {...props}>
      {/* Top Subtle Border */}
      <div className="h-0.5 bg-gradient-to-r from-transparent via-slate-600/50 to-transparent"></div>

      {/* Main Footer Content */}
      <div className="container mx-auto max-w-7xl px-4 py-8 lg:py-12">
        <div className="grid grid-cols-1 lg:grid-cols-6 gap-8 lg:gap-12">
          {/* Community Info */}
          <div className="lg:col-span-2">
            <CommunityInfo />
          </div>

          {/* Navigation Links */}
          <div className="lg:col-span-4">
            <NavigationLinks />
          </div>
        </div>

        {/* Social Media & Bottom Links */}
        <div className="mt-8 lg:mt-12 pt-6 lg:pt-8 border-t border-slate-700/50">
          <div className="flex flex-col sm:flex-row justify-between items-center gap-6">
            <SocialMediaLinks links={socialLinks} />
            <LegalLinks
              links={[
                { label: 'Licenses', href: '/licenses' },
                { label: 'Terms of Service', href: '/terms-of-service' },
                { label: 'Privacy', href: '/polices/privacy' },
                { label: 'Cookies', href: '/polices/cookies' },
              ]}
            />
          </div>

          <div className="mt-6 lg:mt-8 pt-4 lg:pt-6 border-t border-slate-700/50 text-center text-sm text-slate-500">© {new Date().getFullYear()} Game Guild Inc. All rights reserved.</div>
        </div>
      </div>

      {/* Bottom Beautiful Border */}
      <div className="h-1 bg-gradient-to-r from-green-500 via-blue-500 to-purple-500"></div>

      {children}
    </footer>
  );
};

export default Footer;
