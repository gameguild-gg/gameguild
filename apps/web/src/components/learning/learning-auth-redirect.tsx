"use client";

import { Button } from "@game-guild/ui/components/button";
import {
  getPathname,
  Link,
  usePathname,
  useRouter,
} from "@/i18n/navigation";
import { getLearnerSignInHref } from "@/lib/learner/paths";
import { LogIn } from "lucide-react";
import { useLocale } from "next-intl";
import { useSearchParams } from "next/navigation";
import { useEffect } from "react";

export function LearningAuthRedirect() {
  const pathname = usePathname();
  const locale = useLocale();
  const searchParams = useSearchParams();
  const router = useRouter();
  const href = getLearnerSignInHref({
    pathname: getPathname({ href: pathname, locale }),
    search: searchParams.toString(),
  });

  useEffect(() => {
    router.replace(href);
  }, [href, router]);

  return (
    <main className="grid min-h-dvh place-items-center bg-background p-6">
      <div className="max-w-sm text-center">
        <LogIn className="mx-auto size-8 text-primary" aria-hidden="true" />
        <h1 className="mt-4 text-xl font-semibold text-foreground">
          Sign in to continue
        </h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Your learning destination will be restored after authentication.
        </p>
        <Button className="mt-6" render={<Link href={href} />}>
          Continue to sign in
        </Button>
      </div>
    </main>
  );
}
