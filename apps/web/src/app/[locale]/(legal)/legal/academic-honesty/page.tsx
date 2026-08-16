import type React from 'react';

const standards = [
  'Submit original work or clearly identify borrowed code, assets, writing, and research.',
  'Keep assessment attempts fair by following the stated collaboration and tool-use rules.',
  'Do not impersonate another learner, submit work for another learner, or manipulate progress evidence.',
  'Use AI assistance only when the course or challenge allows it and disclose material AI-generated contributions.',
];

export default async function Page({}: PageProps<'/[locale]/legal/academic-honesty'>): Promise<React.JSX.Element> {
  return (
    <main className="min-h-screen bg-background">
      <section className="mx-auto flex max-w-4xl flex-col gap-8 px-4 py-10">
        <div className="space-y-3">
          <p className="text-sm font-medium uppercase tracking-[0.18em] text-muted-foreground">Learner policy</p>
          <h1 className="text-4xl font-bold tracking-tight">Academic Honesty</h1>
          <p className="text-muted-foreground">
            GameGuild learning, testing, and launch programs depend on honest authorship and reliable assessment evidence.
          </p>
        </div>

        <section className="space-y-4 rounded-lg border bg-card p-5">
          <h2 className="text-xl font-semibold">Standards</h2>
          <ul className="space-y-2 text-sm text-muted-foreground">
            {standards.map((standard) => (
              <li key={standard}>{standard}</li>
            ))}
          </ul>
        </section>

        <section className="space-y-3 rounded-lg border bg-card p-5">
          <h2 className="text-xl font-semibold">Plagiarism and misuse</h2>
          <p className="text-sm text-muted-foreground">
            Plagiarism, fabricated testing evidence, credential sharing, copied submissions, and undisclosed paid assistance may result in assessment reset, certificate hold, account restriction, or program removal.
          </p>
        </section>
      </section>
    </main>
  );
}
