import type React from 'react';

const disclosureRules = [
  'Students choose which education records may be shared with instructors, sponsors, teammates, or reviewers.',
  'A disclosure must describe the recipient, purpose, record type, and expiration date before consent is recorded.',
  'Students can revoke consent for future disclosures without changing historical audit records.',
];

export default async function Page({}: PageProps<'/[locale]/ferpa-waiver'>): Promise<React.JSX.Element> {
  return (
    <main className="min-h-screen bg-background">
      <section className="mx-auto flex max-w-4xl flex-col gap-8 px-4 py-10">
        <div className="space-y-3">
          <p className="text-sm font-medium uppercase tracking-[0.18em] text-muted-foreground">Student records</p>
          <h1 className="text-4xl font-bold tracking-tight">FERPA Waiver</h1>
          <p className="text-muted-foreground">
            This waiver explains how GameGuild handles education records in learning, assessment, testing lab, and launch review workflows.
          </p>
        </div>

        <section className="space-y-4 rounded-lg border bg-card p-5">
          <h2 className="text-xl font-semibold">Consent model</h2>
          <p className="text-sm text-muted-foreground">
            Education records include enrollment status, assessment results, submitted work, attendance, certificate progress, feedback, and project review notes connected to a learner account.
          </p>
          <ul className="space-y-2 text-sm text-muted-foreground">
            {disclosureRules.map((rule) => (
              <li key={rule}>{rule}</li>
            ))}
          </ul>
        </section>

        <section className="space-y-3 rounded-lg border bg-card p-5">
          <h2 className="text-xl font-semibold">Revocation</h2>
          <p className="text-sm text-muted-foreground">
            To revoke a waiver, the student should use account support or the records request workflow. Revocation stops future sharing but does not delete disclosures that were valid at the time they were made.
          </p>
        </section>
      </section>
    </main>
  );
}
