import React from 'react';
import Link from 'next/link';

interface NavSection {
  title: string;
  links: Array<{
    href: string;
    label: string;
  }>;
  color: 'blue' | 'purple' | 'green';
}

const navSections: NavSection[] = [
  {
    title: 'Platform',
    color: 'blue',
    links: [
      { href: '/courses', label: 'Courses' },
      { href: '/jobs', label: 'Job Board' },
      { href: '/community', label: 'Community' },
      { href: '/games', label: 'Games' },
      { href: '/jams', label: 'Game Jams' },
    ],
  },
  {
    title: 'Company',
    color: 'purple',
    links: [
      { href: '/blog', label: 'Blog' },
      { href: '/about', label: 'About Us' },
      { href: '/careers', label: 'Careers' },
      { href: '/news', label: 'News' },
      { href: '/contact', label: 'Contact' },
    ],
  },
  {
    title: 'Resources',
    color: 'green',
    links: [
      { href: '/docs', label: 'Documentation' },
      { href: '/tutorials', label: 'Tutorials' },
      { href: '/support', label: 'Support' },
      { href: '/api', label: 'API Reference' },
      { href: '/old/contributors', label: 'Contributors' },
    ],
  },
];

const colorClasses = {
  blue: {
    header: 'text-blue-400',
    hover: 'hover:text-blue-400',
  },
  purple: {
    header: 'text-purple-400',
    hover: 'hover:text-purple-400',
  },
  green: {
    header: 'text-green-400',
    hover: 'hover:text-green-400',
  },
};

export function NavigationLinks() {
  return (
    <>
      {navSections.map((section) => (
        <div key={section.title}>
          <h3 className={`font-semibold mb-4 ${colorClasses[section.color].header}`}>{section.title}</h3>
          <ul className="space-y-2 text-sm text-slate-400">
            {section.links.map((link) => (
              <li key={link.href}>
                <Link href={link.href} className={`${colorClasses[section.color].hover} transition-colors duration-300 block py-1`}>
                  {link.label}
                </Link>
              </li>
            ))}
          </ul>
        </div>
      ))}
    </>
  );
}
