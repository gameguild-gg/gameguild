import { Link } from '@/i18n/navigation';
import { FlaskConical, Github, GraduationCap, Users } from 'lucide-react';
import type { ReactNode } from 'react';

const primaryNav = [
  { label: 'Courses', href: '/courses' },
  { label: 'Programs', href: '/programs' },
  { label: 'Projects', href: '/projects' },
  { label: 'Testing Lab', href: '/testing-lab' },
  { label: 'Community', href: '/community' },
  { label: 'Jobs', href: '/jobs' },
  { label: 'About', href: '/about' },
] as const;

const footerSections = [
  {
    title: 'Platform',
    links: [
      { label: 'Courses', href: '/courses' },
      { label: 'Programs', href: '/programs' },
      { label: 'Projects', href: '/projects' },
      { label: 'Testing Lab', href: '/testing-lab' },
    ],
  },
  {
    title: 'Company',
    links: [
      { label: 'About', href: '/about' },
      { label: 'Roadmap', href: '/about/roadmap' },
      { label: 'Contributors', href: '/about/contributors' },
      { label: 'Contact', href: '/contact' },
    ],
  },
  {
    title: 'Community',
    links: [
      { label: 'Community hub', href: '/community' },
      { label: 'Feed', href: '/feed' },
      { label: 'Projects', href: '/projects' },
      { label: 'Jobs', href: '/jobs' },
    ],
  },
  {
    title: 'Institutional',
    links: [
      { label: 'Licenses', href: '/licenses' },
      { label: 'Terms', href: '/terms-of-service' },
      { label: 'Privacy', href: '/polices/privacy' },
      { label: 'Cookies', href: '/polices/cookies' },
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
    <header className="sticky top-0 z-40 border-b border-white/10 bg-slate-950/85 text-white backdrop-blur-xl">
      <div className="mx-auto flex min-h-16 w-full max-w-7xl items-center justify-between gap-4 px-4 py-3 sm:px-6 lg:px-8">
        <Link href="/" aria-label="GameGuild home" className="flex min-w-0 items-center gap-3">
          <BrandMark />
          <span className="truncate text-base font-semibold tracking-tight text-white">GameGuild</span>
        </Link>

        <nav aria-label="Main navigation" className="hidden items-center gap-1 md:flex">
          {primaryNav.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className="rounded-full px-3 py-2 text-sm font-medium text-slate-300 transition hover:bg-white/10 hover:text-white"
            >
              {item.label}
            </Link>
          ))}
        </nav>

        <div className="flex items-center gap-2">
          <a
            href="https://github.com/gameguild-gg/gameguild"
            className="hidden rounded-full border border-white/10 px-3 py-2 text-sm font-medium text-slate-300 transition hover:border-white/20 hover:bg-white/10 hover:text-white sm:inline-flex sm:items-center sm:gap-2"
          >
            <Github className="size-4" aria-hidden="true" />
            GitHub
          </a>
          <Link
            href="/sign-in"
            className="inline-flex items-center rounded-full bg-white px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-sky-100"
          >
            Sign in
          </Link>
        </div>
      </div>

      <nav aria-label="Mobile navigation" className="border-t border-white/10 px-4 pb-3 md:hidden">
        <div className="mx-auto flex w-full max-w-7xl gap-1 overflow-x-auto">
          {primaryNav.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className="shrink-0 rounded-full px-3 py-2 text-sm font-medium text-slate-300 transition hover:bg-white/10 hover:text-white"
            >
              {item.label}
            </Link>
          ))}
        </div>
      </nav>
    </header>
  );
}

export function PublicWebsiteFooter() {
  return (
    <footer className="border-t border-white/10 bg-slate-950 text-white">
      <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-12 sm:px-6 lg:grid-cols-[1.4fr_2fr] lg:px-8">
        <div className="max-w-md space-y-5">
          <div className="flex items-center gap-3">
            <BrandMark />
            <span className="text-lg font-semibold text-white">GameGuild</span>
          </div>
          <p className="text-sm leading-6 text-slate-400">
            A focused game development community for learning, feedback, collaboration, and launch-ready project work.
          </p>
          <div className="space-y-2 text-sm text-slate-300">
            <p>Community-driven learning and development</p>
            <p>Open source and collaborative</p>
          </div>
        </div>

        <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
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
        </div>
      </div>

      <div className="border-t border-white/10">
        <div className="mx-auto flex w-full max-w-7xl flex-col gap-3 px-4 py-6 text-sm text-slate-500 sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8">
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
    title: 'Community Studio',
    description: 'Connect with peers, instructors, and project teams around critique, collaboration, and shipped work.',
    icon: Users,
  },
] as const;
