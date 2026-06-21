import { render, screen } from '@testing-library/react';
import fs from 'node:fs';
import path from 'node:path';
import type { AnchorHTMLAttributes, ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: AnchorHTMLAttributes<HTMLAnchorElement> & { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
  usePathname: () => '/',
}));

import { PublicWebsiteFooter } from '@/components/site/public-website-shell';

const appRoot = __dirname;

const footerRoutes = [
  { href: '/sign-up', page: '[locale]/(auth)/sign-up/page.tsx' },
  { href: '/community', page: '[locale]/(community)/community/page.tsx' },
  { href: '/feed', page: '[locale]/(community)/(feed)/feed/page.tsx' },
  { href: '/jobs', page: '[locale]/(contents)/(jobs)/jobs/page.tsx' },
  { href: '/programs', page: '[locale]/(contents)/(learning)/programs/page.tsx' },
  { href: '/courses', page: '[locale]/(contents)/(learning)/courses/page.tsx' },
  { href: '/projects', page: '[locale]/(contents)/(projects)/projects/page.tsx' },
  { href: '/testing-lab', page: '[locale]/(contents)/(testing-lab)/testing-lab/page.tsx' },
  { href: '/launch-pad', page: '[locale]/(contents)/(launch-pad)/launch-pad/page.tsx' },
  { href: '/about', page: '[locale]/(institutional)/about/page.tsx' },
  { href: '/about/roadmap', page: '[locale]/(institutional)/about/(project)/roadmap/page.tsx' },
  { href: '/about/contributors', page: '[locale]/(institutional)/about/(project)/contributors/page.tsx' },
  { href: '/contact', page: '[locale]/(institutional)/contact/page.tsx' },
  { href: '/licenses', page: '[locale]/(legal)/licenses/page.tsx' },
  { href: '/terms-of-service', page: '[locale]/(legal)/terms-of-service/page.tsx' },
  { href: '/polices/privacy', page: '[locale]/(legal)/polices/privacy/page.tsx' },
  { href: '/polices/cookies', page: '[locale]/(legal)/polices/cookies/page.tsx' },
] as const;

describe('PublicWebsiteFooter routes', () => {
  it('links only to public pages that exist in the app router', () => {
    render(<PublicWebsiteFooter />);

    for (const route of footerRoutes) {
      const links = screen.getAllByRole('link').filter((link) => link.getAttribute('href') === route.href);
      expect(links.length, `Expected footer link for ${route.href}`).toBeGreaterThan(0);
      expect(fs.existsSync(path.join(appRoot, route.page)), `Expected route file for ${route.href}`).toBe(true);
    }
  });
});
