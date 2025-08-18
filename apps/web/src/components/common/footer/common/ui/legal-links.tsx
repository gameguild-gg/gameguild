import Link from 'next/link';
import React from 'react';

interface LegalLinkData {
  label: string;
  href: string;
}

interface LegalLinksProps {
  links?: LegalLinkData[];
}

const LegalLink = ({ label, href }: LegalLinkData): React.JSX.Element => {
  // Add GitHub modal for Terms of Service, Privacy, and Cookies links
  const linksWithGitHubModal = ['Terms of Service', 'Privacy', 'Cookies'];
  const shouldShowGitHubModal = linksWithGitHubModal.includes(label);
  
  if (shouldShowGitHubModal) {
    return (
      <a
        href={href}
        className="hover:text-blue-400 transition-colors duration-300 whitespace-nowrap cursor-pointer"
        data-github-issue="true"
        data-route={href}
      >
        {label}
      </a>
    );
  }
  
  return (
    <Link href={href} className="hover:text-blue-400 transition-colors duration-300 whitespace-nowrap">
      {label}
    </Link>
  );
};

export const LegalLinks = ({ links = [] }: LegalLinksProps): React.JSX.Element => (
  <div className="flex flex-wrap gap-4 sm:gap-6 lg:gap-8 text-sm text-slate-400 justify-center sm:justify-end">
    {links.map((link) => (
      <LegalLink key={link.href} label={link.label} href={link.href} />
    ))}
  </div>
);
