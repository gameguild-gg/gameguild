import { Gamepad2, TestTube, Users } from "lucide-react";
import { buttonVariants } from "@game-guild/ui/components/button";
import { Link } from "@/i18n/navigation";

const FEATURES = [
  {
    icon: Gamepad2,
    title: "Test Latest Games",
    description:
      "Get early access to upcoming games and provide valuable feedback",
  },
  {
    icon: Users,
    title: "Join Community",
    description:
      "Connect with other testers and developers in collaborative sessions",
  },
  {
    icon: TestTube,
    title: "Improve Projects",
    description:
      "Give creators structured evidence they can apply to the next build",
  },
] as const;

export function TestingLabHero() {
  return (
    <section className="px-4 py-16 text-center">
      <div className="relative z-10 mx-auto max-w-6xl p-8">
        {/* Status Chip */}
        <div className="mb-10 flex justify-center">
          <div className="flex items-center gap-2 rounded-full border border-success/30 bg-success/10 px-4 py-2">
            <span className="size-2 rounded-full bg-success" />
            <span className="text-sm font-semibold text-success">
              Community playtesting is live
            </span>
          </div>
        </div>

        {/* Main Headline */}
        <h1 className="mb-12 text-5xl font-bold text-foreground md:text-6xl">
          Game Testing Lab
        </h1>

        {/* Subheadline */}
        <p className="mb-16 text-xl leading-relaxed text-muted-foreground md:text-2xl">
          Help community creators improve their games through managed testing
          sessions
          <br />
          <span className="text-primary">
            Play, observe, and turn experience into useful feedback
          </span>
        </p>

        {/* Key Features */}
        <div className="mx-auto mb-16 grid max-w-5xl gap-12 md:grid-cols-3">
          {FEATURES.map(({ icon: Icon, title, description }) => (
            <div key={title} className="flex flex-col items-center gap-3">
              <div className="rounded-full border border-border bg-muted p-4">
                <Icon className="size-8 text-primary" />
              </div>
              <h3 className="text-lg font-semibold">{title}</h3>
              <p className="text-center text-muted-foreground">{description}</p>
            </div>
          ))}
        </div>

        {/* Call-to-Action Buttons */}
        <div className="flex flex-col justify-center gap-4 sm:flex-row">
          <Link href="/testing-lab" className={buttonVariants({ size: "lg" })}>
            Browse Events
          </Link>
          <Link
            href="/workspace/projects"
            className={buttonVariants({ variant: "outline", size: "lg" })}
          >
            Submit a project
          </Link>
        </div>
      </div>
    </section>
  );
}
