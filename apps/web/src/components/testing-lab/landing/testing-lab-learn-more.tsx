import { buttonVariants } from "@game-guild/ui/components/button";
import Link from "next/link";

export function TestingLabLearnMore() {
  return (
    <div className="mt-24 mb-12 flex flex-col items-center">
      <div className="mb-8 flex items-center gap-6">
        <div className="h-px w-32 bg-border"></div>
        <span className="text-base font-medium text-muted-foreground">
          curious how it all works?
        </span>
        <div className="h-px w-32 bg-border"></div>
      </div>
      <Link href="#learn-more" className={buttonVariants({ size: "lg" })}>
        Learn More
      </Link>
    </div>
  );
}
