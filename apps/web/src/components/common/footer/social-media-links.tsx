import React from 'react';
import Link from 'next/link';
import { Github, MessageSquare, Twitter, Youtube } from 'lucide-react';

const socialLinks = [
  {
    href: '#',
    icon: MessageSquare,
    label: 'Discord',
    hoverColor: 'hover:border-indigo-400/50 hover:shadow-indigo-500/10',
    iconHover: 'group-hover:text-indigo-400',
  },
  {
    href: '#',
    icon: Twitter,
    label: 'Twitter',
    hoverColor: 'hover:border-blue-400/50 hover:shadow-blue-500/10',
    iconHover: 'group-hover:text-blue-400',
  },
  {
    href: '#',
    icon: Github,
    label: 'GitHub',
    hoverColor: 'hover:border-purple-400/50 hover:shadow-purple-500/10',
    iconHover: 'group-hover:text-purple-400',
  },
  {
    href: '#',
    icon: Youtube,
    label: 'YouTube',
    hoverColor: 'hover:border-red-400/50 hover:shadow-red-500/10',
    iconHover: 'group-hover:text-red-400',
  },
];

export function SocialMediaLinks() {
  return (
    <div className="flex gap-3">
      {socialLinks.map(({ href, icon: Icon, label, hoverColor, iconHover }) => (
        <Link
          key={label}
          href={href}
          aria-label={label}
          className={`group p-3 bg-gradient-to-br from-slate-800/50 to-slate-700/50 rounded-lg border border-slate-600/50 transition-all duration-300 hover:shadow-lg ${hoverColor}`}
        >
          <Icon className={`w-5 h-5 text-slate-400 transition-colors ${iconHover}`} />
        </Link>
      ))}
    </div>
  );
}
