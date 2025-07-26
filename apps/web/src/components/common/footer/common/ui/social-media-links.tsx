import React from 'react';
import Link from 'next/link';
import { LucideIcon } from 'lucide-react';

interface SocialLinkData {
  href: string;
  icon: LucideIcon;
  label: string;
  hoverColor: string;
  iconHover: string;
}

interface SocialMediaLinksProps {
  links: SocialLinkData[];
}

// Internal component to encapsulate social link styling
const SocialLink = ({ href, icon: Icon, label, hoverColor, iconHover }: SocialLinkData): React.JSX.Element => (
  <Link
    href={href}
    aria-label={label}
    className={`group p-3 bg-gradient-to-br from-slate-800/50 to-slate-700/50 rounded-lg border border-slate-600/50 transition-all duration-300 hover:shadow-lg ${hoverColor}`}
  >
    <Icon className={`w-5 h-5 text-slate-400 transition-colors ${iconHover}`} />
  </Link>
);

export function SocialMediaLinks({ links }: SocialMediaLinksProps) {
  return (
    <div className="flex gap-3">
      {links.map((link) => (
        <SocialLink key={link.label} href={link.href} icon={link.icon} label={link.label} hoverColor={link.hoverColor} iconHover={link.iconHover} />
      ))}
    </div>
  );
}
