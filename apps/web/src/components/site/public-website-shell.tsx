import { Link } from '@/i18n/navigation';
import { FlaskConical, Github, GraduationCap, Rocket, Users } from 'lucide-react';
import type { ReactNode } from 'react';
import { PublicDesktopNav, PublicMobileNav } from './public-website-nav';

const primaryNav = [
  { label: 'Courses', href: '/courses' },
  { label: 'Programs', href: '/programs' },
  { label: 'Testing Lab', href: '/testing-lab' },
  { label: 'Launch Pad', href: '/launch-pad' },
  { label: 'Projects', href: '/projects' },
  { label: 'Community', href: '/community' },
  { label: 'Jobs', href: '/jobs' },
  { label: 'About', href: '/about' },
] as const;

const footerSections = [
  {
    title: 'Learn',
    links: [
      { label: 'Courses', href: '/courses' },
      { label: 'Programs', href: '/programs' },
    ],
  },
  {
    title: 'Build & test',
    links: [
      { label: 'Testing Lab', href: '/testing-lab' },
      { label: 'Launch Pad', href: '/launch-pad' },
      { label: 'Project showcase', href: '/projects' },
    ],
  },
  {
    title: 'Community',
    links: [
      { label: 'Community hub', href: '/community' },
      { label: 'Feed', href: '/feed' },
      { label: 'Jobs', href: '/jobs' },
    ],
  },
  {
    title: 'Company',
    links: [
      { label: 'About GameGuild', href: '/about' },
      { label: 'Roadmap', href: '/about/roadmap' },
      { label: 'Contributors', href: '/about/contributors' },
      { label: 'Contact', href: '/contact' },
    ],
  },
] as const;

function BrandMark() {
  return (
    <span className="flex size-9 items-center justify-center rounded-xl border border-white/15 bg-white text-slate-950 shadow-sm">
      <GraduationCap className="size-5" aria-hidden="true" />
    </span>
  );
}

export function PublicWebsiteHeader() {
  return (
    <header className="sticky top-0 z-40 border-b border-white/10 bg-slate-950/90 text-white backdrop-blur-xl">
      <div className="mx-auto flex min-h-16 w-full max-w-7xl items-center justify-between gap-4 px-4 py-3 sm:px-6 lg:px-8">
        <Link href="/" aria-label="GameGuild home" className="flex min-w-0 items-center gap-3">
          <BrandMark />
          <span className="truncate text-base font-semibold tracking-tight text-white">GameGuild</span>
        </Link>

        <PublicDesktopNav items={primaryNav} />

        <div className="flex items-center gap-2">
          <a
            href="https://github.com/gameguild-gg/gameguild"
            className="hidden rounded-full border border-white/10 px-3 py-2 text-sm font-medium text-slate-300 transition hover:border-white/20 hover:bg-white/10 hover:text-white xl:inline-flex xl:items-center xl:gap-2"
          >
            <Github className="size-4" aria-hidden="true" />
            GitHub
          </a>
          <Link
            href="/sign-in"
            className="hidden rounded-full border border-white/10 px-4 py-2 text-sm font-semibold text-slate-200 transition hover:bg-white/10 hover:text-white sm:inline-flex"
          >
            Sign in
          </Link>
          <Link
            href="/sign-up"
            className="hidden items-center rounded-full bg-sky-300 px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-sky-200 sm:inline-flex"
          >
            Join community
          </Link>
          <PublicMobileNav items={primaryNav} />
        </div>
      </div>
    </header>
  );
}

export function PublicWebsiteFooter() {
  return (
    <footer className="border-t border-white/10 bg-slate-950 text-white">
      <div className="mx-auto w-full max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        <div
          data-testid="footer-primary-grid"
          className="grid gap-10 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1.65fr)] lg:items-start"
        >
          <div className="max-w-xl space-y-5">
            <div className="flex items-center gap-3">
              <BrandMark />
              <span className="text-lg font-semibold text-white">GameGuild</span>
            </div>
            <p className="text-2xl font-semibold leading-tight tracking-tight text-white sm:text-3xl">
              Learn. Build. Ship together.
            </p>
            <p className="max-w-md text-sm leading-6 text-slate-400">
              A practical game development community for courses, playable testing, launch support, and the people who
              help each other finish real projects.
            </p>
            <div className="flex flex-wrap items-center gap-3 text-sm font-semibold">
              <Link
                href="/sign-up"
                className="inline-flex items-center rounded-full bg-sky-300 px-4 py-2 text-slate-950 transition hover:bg-sky-200"
              >
                Join community
              </Link>
              <a
                href="https://github.com/gameguild-gg/gameguild"
                className="inline-flex items-center gap-2 rounded-full border border-white/10 px-4 py-2 text-slate-200 transition hover:border-white/20 hover:bg-white/10 hover:text-white"
              >
                <Github className="size-4" aria-hidden="true" />
                GitHub
              </a>
            </div>
          </div>

          <nav aria-label="Footer" className="grid gap-x-8 gap-y-7 sm:grid-cols-2 lg:grid-cols-4">
            {footerSections.map((section) => (
              <div key={section.title} className="space-y-3">
                <h2 className="text-sm font-semibold text-white">{section.title}</h2>
                <ul className="space-y-2">
                  {section.links.map((link) => (
                    <li key={link.href}>
                      <Link href={link.href} className="text-sm text-slate-400 transition hover:text-white">
                        {link.label}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </nav>
        </div>

        <div className="mt-10 flex flex-col gap-3 border-t border-white/10 pt-6 text-sm text-slate-500 sm:flex-row sm:items-center sm:justify-between">
          <p>© 2026 GameGuild. All rights reserved.</p>
          <div className="flex flex-wrap gap-4">
            <Link href="/licenses" className="transition hover:text-slate-300">
              Licenses
            </Link>
            <Link href="/terms-of-service" className="transition hover:text-slate-300">
              Terms
            </Link>
            <Link href="/polices/privacy" className="transition hover:text-slate-300">
              Privacy
            </Link>
          </div>
        </div>
      </div>
    </footer>
  );
}

export function PublicWebsiteShell({ children }: { readonly children: ReactNode }) {
  return (
    <div className="min-h-svh bg-slate-950">
      <PublicWebsiteHeader />
      {children}
      <PublicWebsiteFooter />
    </div>
  );
}

export const publicWebsiteHighlights = [
  {
    title: 'Interactive Courses',
    description: 'Structured game development paths with practical projects, assessments, and production-ready outcomes.',
    icon: GraduationCap,
  },
  {
    title: 'Testing Lab',
    description: 'A focused review space where creators can validate builds, gather feedback, and improve playable work.',
    icon: FlaskConical,
  },
  {
    title: 'Launch Pad',
    description: 'Release planning, store-page critique, and launch-readiness checklists for student projects.',
    icon: Rocket,
  },
  {
    title: 'Community Studio',
    description: 'Connect with peers, instructors, and project teams around critique, collaboration, and shipped work.',
    icon: Users,
  },
] as const;
