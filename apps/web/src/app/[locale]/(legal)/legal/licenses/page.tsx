const licenseGroups = [
  {
    title: 'Runtime packages',
    items: ['Next.js and React runtime packages', 'Radix UI interface primitives', 'Lucide icon set', 'Markdown and syntax rendering packages'],
  },
  {
    title: 'Development tooling',
    items: ['TypeScript, ESLint, Vitest, and Playwright compatible tooling', 'Tsup and package generation utilities', 'OpenAPI and schema generation dependencies'],
  },
  {
    title: 'Content and assets',
    items: ['GameGuild owned copy, product names, course text, and original media', 'Contributor submissions governed by repository contribution terms'],
  },
];

export default async function LicensesPage({}: PageProps<'/[locale]/legal/licenses'>): Promise<React.JSX.Element> {
  return (
    <article className="flex flex-col gap-8">
      <div className="space-y-3">
        <p className="text-sm font-medium uppercase tracking-[0.18em] text-muted-foreground">Legal</p>
        <h1 className="text-4xl font-bold tracking-tight">Licenses</h1>
        <p className="max-w-3xl text-muted-foreground">
          GameGuild uses third-party packages and first-party platform assets. This page summarizes how those materials are handled for product, learning, and community surfaces.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        {licenseGroups.map((group) => (
          <section key={group.title} className="rounded-lg border bg-card p-5">
            <h2 className="text-lg font-semibold">{group.title}</h2>
            <ul className="mt-4 space-y-2 text-sm text-muted-foreground">
              {group.items.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </section>
        ))}
      </div>

      <section className="space-y-3 rounded-lg border bg-card p-5">
        <h2 className="text-xl font-semibold">Third-party packages</h2>
        <p className="text-sm text-muted-foreground">
          Third-party packages remain governed by their upstream licenses. Production deployments should keep generated lockfiles and dependency notices available for audit, security review, and customer procurement checks.
        </p>
      </section>
    </article>
  );
}
