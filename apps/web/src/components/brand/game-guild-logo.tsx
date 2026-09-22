import { Link } from "@/i18n/navigation";
import { GraduationCap } from "lucide-react";

/**
 * The GameGuild brand lockup: white tile, graduation-cap mark, and wordmark,
 * linking home. Reusable across the auth screens, public site, and shells.
 */
export function GameGuildLogo({
  locale = "en-US",
  href,
  className = "",
}: {
  locale?: string;
  href?: string;
  className?: string;
}) {
  return (
    <Link
      href={href ?? `/${locale}`}
      className={`flex items-center gap-3 font-semibold ${className}`}
    >
      <span className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-white text-slate-950">
        <GraduationCap className="size-5" aria-hidden="true" />
      </span>
      GameGuild
    </Link>
  );
}
