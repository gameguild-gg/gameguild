import Link from 'next/link';
import React from 'react';

interface LegalLinkData {
  label: string;
  href: string;
}

interface LegalLinksProps {
  links?: LegalLinkData[];
}

const LegalLink = ({ label, href }: LegalLinkData): React.JSX.Element => (
  <Link href={href} className="hover:text-blue-400 transition-colors duration-300">
    {label}
  </Link>
);

export const LegalLinks = ({ links = [] }: LegalLinksProps): React.JSX.Element => (
  <div className="flex flex-wrap gap-8 text-sm text-slate-400">
    {links.map((link) => (
      <LegalLink key={link.href} label={link.label} href={link.href} />
    ))}
  </div>
);
