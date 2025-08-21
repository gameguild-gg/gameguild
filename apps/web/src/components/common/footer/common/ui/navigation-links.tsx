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
      { href: '/testing-lab', label: 'Game Testing Lab' },
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
    ],
  },
  {
    title: 'Institutional',
    color: 'blue',
    links: [
      { href: '/contributors', label: 'Contributors' },
      { href: '/licenses', label: 'Licenses' },
      { href: '/terms-of-service', label: 'Terms of Service' },
      { href: '/polices/privacy', label: 'Privacy' },
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
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 lg:gap-8">
      {navSections.map((section) => (
        <div key={section.title} className="min-w-0">
          <h3 className={`font-semibold mb-4 ${colorClasses[section.color].header} text-base lg:text-lg`}>{section.title}</h3>
          <ul className="space-y-2 text-sm text-slate-700 dark:text-slate-400">
            {section.links.map((link) => {
              // Add GitHub modal for specific links
              const linksWithGitHubModal = [
                'Game Jams', 
                'Community', 
                'Blog', 
                'Careers', 
                'News', 
                'Contact', 
                'Documentation', 
                'Tutorials', 
                'Support', 
                'API Reference',
                'Privacy',
                'Terms of Service'
              ];
              const shouldShowGitHubModal = linksWithGitHubModal.includes(link.label);
              
              return (
                <li key={link.href} className="flex items-start">
                  <span className="text-slate-600 dark:text-slate-500 mr-2 mt-1.5 flex-shrink-0">•</span>
                  {shouldShowGitHubModal ? (
                    <a
                      href={link.href}
                      className={`${colorClasses[section.color].hover} transition-colors duration-300 block py-1 leading-relaxed break-words cursor-pointer`}
                      data-github-issue="true"
                      data-route={link.href}
                    >
                      {link.label}
                    </a>
                  ) : (
                    <Link
                      href={link.href}
                      className={`${colorClasses[section.color].hover} transition-colors duration-300 block py-1 leading-relaxed break-words`}
                    >
                      {link.label}
                    </Link>
                  )}
                </li>
              );
            })}
          </ul>
        </div>
      ))}
    </div>
  );
}
