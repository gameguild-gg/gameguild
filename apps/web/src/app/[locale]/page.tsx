import { Link } from '@/i18n';
import { ArrowRight, BookOpen, FlaskConical, Github, GraduationCap, Sparkles, Users } from 'lucide-react';
import React from 'react';

const primaryNav = [
  { label: 'Courses', href: '/courses' },
  { label: 'Programs', href: '/programs' },
  { label: 'Testing Lab', href: '/dashboard/testing-lab' },
  { label: 'Institutional', href: '/about' },
] as const;

const featureCards = [
  {
    title: 'Interactive Courses',
    description: 'Structured game development paths with practical projects, assessments, and production-ready outcomes.',
    icon: BookOpen,
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

const footerSections = [
  {
    title: 'Platform',
    links: [
      { label: 'Courses', href: '/courses' },
      { label: 'Programs', href: '/programs' },
      { label: 'Testing Lab', href: '/dashboard/testing-lab' },
      { label: 'Launch Pad', href: '/dashboard/launch-pad' },
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
    title: 'Resources',
    links: [
      { label: 'Feed', href: '/feed' },
      { label: 'Projects', href: '/projects' },
      { label: 'Jobs', href: '/jobs' },
      { label: 'Support', href: '/dashboard/community/members/support' },
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

function SiteHeader() {
  return (
    <header className="sticky top-0 z-40 border-b border-white/10 bg-slate-950/85 backdrop-blur-xl">
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

function SiteFooter() {
  return (
    <footer className="border-t border-white/10 bg-slate-950">
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

export default async function Page({ params }: PageProps<'/[locale]'>): Promise<React.JSX.Element> {
  await params;

  return (
    <div className="min-h-svh bg-slate-950 text-white">
      <SiteHeader />

      <main>
        <section className="relative overflow-hidden">
          <div className="absolute inset-x-0 top-[-20%] h-96 bg-[radial-gradient(circle_at_center,rgba(56,189,248,0.18),transparent_58%)]" />
          <div className="mx-auto grid w-full max-w-7xl items-center gap-12 px-4 py-20 sm:px-6 sm:py-24 lg:grid-cols-[1.05fr_0.95fr] lg:px-8 lg:py-28">
            <div className="relative z-10 max-w-3xl space-y-8">
              <div className="space-y-5">
                <h1 className="max-w-4xl text-balance text-5xl font-semibold tracking-tight text-white sm:text-6xl lg:text-7xl">
                  Learn, Build & Connect
                </h1>
                <p className="max-w-2xl text-lg leading-8 text-slate-300 sm:text-xl">
                  Master game development through practical courses, community critique, testing workflows, and launch
                  support designed for builders who want to ship.
                </p>
              </div>

              <div className="flex flex-col gap-3 sm:flex-row">
                <Link
                  href="/courses"
                  className="inline-flex items-center justify-center rounded-full bg-sky-300 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
                >
                  Start Learning
                  <ArrowRight className="ml-2 size-4" aria-hidden="true" />
                </Link>
                <Link
                  href="/programs"
                  className="inline-flex items-center justify-center rounded-full border border-white/15 px-5 py-3 text-sm font-semibold text-white transition hover:border-white/30 hover:bg-white/10"
                >
                  Explore Programs
                </Link>
              </div>
            </div>

            <div className="relative z-10">
              <div className="rounded-[2rem] border border-white/10 bg-white/[0.04] p-4 shadow-2xl shadow-sky-950/40 backdrop-blur">
                <div className="rounded-[1.5rem] border border-white/10 bg-slate-900/90 p-5">
                  <div className="mb-6 flex items-center justify-between">
                    <div>
                      <p className="text-sm font-medium text-slate-400">Learning path</p>
                      <h2 className="mt-1 text-2xl font-semibold text-white">From course to shipped project</h2>
                    </div>
                    <Sparkles className="size-6 text-sky-300" aria-hidden="true" />
                  </div>

                  <div className="space-y-3">
                    {['Study core systems', 'Build a playable prototype', 'Test with peers', 'Prepare launch assets'].map(
                      (step, index) => (
                        <div
                          key={step}
                          className="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/[0.03] px-4 py-3"
                        >
                          <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-sky-300/15 text-sm font-semibold text-sky-200">
                            {index + 1}
                          </span>
                          <span className="text-sm font-medium text-slate-200">{step}</span>
                        </div>
                      ),
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section className="border-y border-white/10 bg-white/[0.03]">
          <div className="mx-auto w-full max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
            <div className="max-w-2xl space-y-3">
              <h2 className="text-3xl font-semibold tracking-tight text-white sm:text-4xl">
                Everything You Need to Succeed
              </h2>
              <p className="text-base leading-7 text-slate-400">
                A compact ecosystem for building game skills, validating work, and moving from learning into public
                launch with less friction.
              </p>
            </div>

            <div className="mt-10 grid gap-4 md:grid-cols-3">
              {featureCards.map((feature) => {
                const Icon = feature.icon;

                return (
                  <article key={feature.title} className="rounded-3xl border border-white/10 bg-slate-900/70 p-6">
                    <div className="mb-6 flex size-11 items-center justify-center rounded-2xl bg-sky-300/10 text-sky-200">
                      <Icon className="size-5" aria-hidden="true" />
                    </div>
                    <h3 className="text-lg font-semibold text-white">{feature.title}</h3>
                    <p className="mt-3 text-sm leading-6 text-slate-400">{feature.description}</p>
                  </article>
                );
              })}
            </div>
          </div>
        </section>
      </main>

      <SiteFooter />
    </div>
  );
}
