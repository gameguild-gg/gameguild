import { Gamepad2 } from "lucide-react";

const COVER_THEMES = [
  {
    base: "from-violet-600 via-indigo-600 to-sky-500",
    glow: ["bg-fuchsia-400", "bg-cyan-300"],
  },
  {
    base: "from-fuchsia-600 via-purple-600 to-indigo-500",
    glow: ["bg-pink-300", "bg-sky-400"],
  },
  {
    base: "from-sky-500 via-blue-600 to-indigo-600",
    glow: ["bg-cyan-300", "bg-violet-400"],
  },
  {
    base: "from-emerald-500 via-teal-600 to-cyan-600",
    glow: ["bg-lime-300", "bg-sky-300"],
  },
  {
    base: "from-amber-500 via-orange-600 to-rose-500",
    glow: ["bg-yellow-300", "bg-pink-400"],
  },
  {
    base: "from-rose-500 via-pink-600 to-purple-600",
    glow: ["bg-orange-300", "bg-indigo-400"],
  },
] as const;

function themeIndex(seed: string) {
  let hash = 0;
  for (let position = 0; position < seed.length; position += 1) {
    hash = (hash * 31 + seed.charCodeAt(position)) % 1_000_003;
  }
  return hash % COVER_THEMES.length;
}

/**
 * Decorative, deterministic cover artwork for testing sessions. Sessions carry
 * no imagery, so the cover is seeded from the event id: the same session always
 * gets the same gradient family wherever it appears (directory cards, feed).
 */
export function EventCoverArt({
  seed,
  className = "",
}: {
  seed: string;
  className?: string;
}) {
  const theme = COVER_THEMES[themeIndex(seed)]!;
  return (
    <div
      aria-hidden="true"
      className={`absolute inset-0 overflow-hidden bg-gradient-to-br ${theme.base} ${className}`}
    >
      <div
        className={`absolute -left-10 -top-12 size-48 rounded-full opacity-25 blur-3xl ${theme.glow[0]}`}
      />
      <div
        className={`absolute -bottom-14 -right-12 size-56 rounded-full opacity-20 blur-3xl ${theme.glow[1]}`}
      />
      <div className="absolute inset-0 opacity-10 [background-image:radial-gradient(circle,white_1.5px,transparent_1.5px)] [background-size:22px_22px]" />
      <Gamepad2
        strokeWidth={1}
        className="absolute -bottom-6 -right-5 size-36 -rotate-12 text-white/15"
      />
      <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-black/5 to-black/20" />
    </div>
  );
}
