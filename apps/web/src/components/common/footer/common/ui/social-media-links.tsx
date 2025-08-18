import React from 'react';
import { LucideIcon } from 'lucide-react';
import { IconType } from 'react-icons';

interface SocialLinkData {
  href: string;
  icon: LucideIcon | IconType;
  label: string;
  hoverColor: string;
  iconHover: string;
}

interface SocialMediaLinksProps {
  links: SocialLinkData[];
}

// Internal component to encapsulate social link styling
const SocialLink = ({ href, icon: Icon, label, hoverColor, iconHover }: SocialLinkData): React.JSX.Element => {
  // Add GitHub issue modal data attributes for Twitter and YouTube
  const shouldShowGitHubModal = label === 'Twitter' || label === 'YouTube';
  
  return (
    <a 
      href={shouldShowGitHubModal ? '#' : href}
      target={shouldShowGitHubModal ? undefined : '_blank'}
      rel={shouldShowGitHubModal ? undefined : 'noopener noreferrer'}
      aria-label={label} 
      className={`group p-3 bg-gradient-to-br from-slate-800/50 to-slate-700/50 rounded-lg border border-slate-600/50 transition-all duration-300 hover:shadow-lg ${hoverColor}`}
      {...(shouldShowGitHubModal && {
        'data-github-issue': 'true',
        'data-route': '/footer'
      })}
    >
      <Icon className={`w-5 h-5 text-slate-400 transition-colors ${iconHover}`} />
    </a>
  );
};

export function SocialMediaLinks({ links }: SocialMediaLinksProps) {
  return (
    <div className="flex gap-3">
      {links.map((link) => (
        <SocialLink key={link.label} href={link.href} icon={link.icon} label={link.label} hoverColor={link.hoverColor} iconHover={link.iconHover} />
      ))}
    </div>
  );
}
