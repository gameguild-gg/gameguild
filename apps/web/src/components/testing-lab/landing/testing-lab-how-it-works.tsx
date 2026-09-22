const STEPS = [
  {
    number: 1,
    title: "Find & Join Events",
    description:
      "Browse available testing sessions for games in development. Choose sessions that match your gaming interests and schedule.",
    bullets: [
      "Filter by game genre and type",
      "View session requirements",
      "Check available time slots",
    ],
  },
  {
    number: 2,
    title: "Test & Provide Feedback",
    description:
      "Participate in guided testing sessions with clear objectives. Follow structured protocols to ensure valuable feedback.",
    bullets: [
      "Follow testing guidelines",
      "Report bugs and issues",
      "Share gameplay experiences",
    ],
  },
  {
    number: 3,
    title: "Submit Your Project",
    description:
      "Prepare an eligible version, add test instructions and a feedback questionnaire, then apply while applications are open.",
    bullets: [
      "Create a ReadyForTesting version",
      "Add a test brief and questionnaire",
      "Apply to an open event",
    ],
  },
] as const;

export function TestingLabHowItWorks() {
  return (
    <section className="flex min-h-dvh items-center px-4 py-24">
      <div className="mx-auto w-full max-w-6xl space-y-16 text-center">
        <div>
          <h2 className="mb-6 text-4xl font-bold text-foreground md:text-6xl">
            How to Get Involved
          </h2>
          <p className="mx-auto max-w-3xl text-xl leading-relaxed text-muted-foreground md:text-2xl">
            Join our community of testers and developers to shape the future of
            gaming
          </p>
        </div>

        <div className="mx-auto grid max-w-5xl gap-12 text-center md:grid-cols-3">
          {STEPS.map((step) => (
            <div
              key={step.number}
              className="rounded-2xl border border-border bg-card p-8"
            >
              <div className="mx-auto mb-6 flex size-16 items-center justify-center rounded-full bg-primary text-primary-foreground">
                <span className="text-2xl font-bold">{step.number}</span>
              </div>
              <h3 className="mb-4 text-2xl font-bold">{step.title}</h3>
              <p className="mb-6 text-lg leading-relaxed text-muted-foreground">
                {step.description}
              </p>
              <ul className="space-y-3 text-left">
                {step.bullets.map((bullet) => (
                  <li
                    key={bullet}
                    className="flex items-center gap-3 text-muted-foreground"
                  >
                    <span
                      aria-hidden="true"
                      className="size-2 shrink-0 rounded-full bg-primary"
                    />
                    <span>{bullet}</span>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
