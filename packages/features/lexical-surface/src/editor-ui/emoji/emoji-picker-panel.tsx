/**
 * EmojiPickerPanel — searchable grid panel for picking emojis.
 *
 * Used by the toolbar Insert menu to show a popover where the user can
 * search by alias/tag and click a glyph to insert. Calls `onSelect`
 * with the chosen emoji string.
 */
"use client";

import { useEffect, useMemo, useState, useRef } from "react";
import { cn } from "@game-guild/ui/lib/utils";
import type { Emoji, EmojiCategory } from "./emoji-list";

interface EmojiPickerPanelProps {
  onSelect: (emoji: string) => void;
  autoFocus?: boolean;
  className?: string;
}

const CATEGORY_ICONS: Record<EmojiCategory, string> = {
  "Smileys & Emotion": "😀",
  "People & Body": "👋",
  "Animals & Nature": "🐶",
  "Food & Drink": "🍔",
  "Travel & Places": "🚗",
  "Activities & Objects": "⚽",
  Symbols: "❤️",
  Flags: "🏁",
};

const CATEGORIES: EmojiCategory[] = [
  "Smileys & Emotion",
  "People & Body",
  "Animals & Nature",
  "Food & Drink",
  "Travel & Places",
  "Activities & Objects",
  "Symbols",
  "Flags",
];

export function EmojiPickerPanel({
  onSelect,
  autoFocus = true,
  className,
}: EmojiPickerPanelProps) {
  const [emojis, setEmojis] = useState<Emoji[]>([]);
  const [query, setQuery] = useState("");
  const [activeCategory, setActiveCategory] =
    useState<EmojiCategory>("Smileys & Emotion");
  const listRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let cancelled = false;
    void import("./emoji-list").then((mod) => {
      if (!cancelled) setEmojis(mod.default);
    });
    return () => {
      cancelled = true;
    };
  }, []);

  // Reset scroll on category or query change
  useEffect(() => {
    if (listRef.current) {
      listRef.current.scrollTop = 0;
    }
  }, [activeCategory, query]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) {
      return emojis.filter((e) => e.category === activeCategory);
    }
    return emojis.filter(
      (e) =>
        e.aliases.some((a) => a.toLowerCase().includes(q)) ||
        e.tags.some((t) => t.toLowerCase().includes(q)),
    );
  }, [emojis, query, activeCategory]);

  return (
    <div className={cn("flex w-[280px] flex-col gap-2", className)}>
      <input
        type="text"
        autoFocus={autoFocus}
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search emoji…"
        aria-label="Search emoji"
        className={cn(
          "h-8 w-full rounded border px-2 text-sm outline-none",
          "border-gray-200 bg-white text-gray-900",
          "dark:border-gray-700 dark:bg-gray-900 dark:text-white",
          "focus:border-blue-500",
        )}
      />

      {!query && (
        <div className="flex items-center justify-between px-1 border-b border-gray-100 dark:border-gray-800 pb-2">
          {CATEGORIES.map((cat) => (
            <button
              key={cat}
              type="button"
              title={cat}
              aria-label={cat}
              onClick={() => setActiveCategory(cat)}
              className={cn(
                "p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors text-[16px] leading-none grayscale opacity-50",
                activeCategory === cat &&
                  "bg-gray-100 dark:bg-gray-800 grayscale-0 opacity-100",
              )}
            >
              {CATEGORY_ICONS[cat]}
            </button>
          ))}
        </div>
      )}

      <div
        ref={listRef}
        role="listbox"
        aria-label="Emoji"
        className="grid max-h-[260px] grid-cols-8 gap-0.5 overflow-y-auto pr-0.5"
      >
        {filtered.length === 0 && (
          <div className="col-span-8 py-6 text-center text-xs text-gray-500 dark:text-gray-400">
            No emoji found
          </div>
        )}
        {filtered.map((e) => {
          const label = e.aliases[0] ?? "emoji";
          return (
            <button
              key={label + e.emoji}
              type="button"
              role="option"
              title={label}
              aria-label={label}
              onClick={() => onSelect(e.emoji)}
              className={cn(
                "flex h-8 w-8 items-center justify-center rounded text-lg leading-none",
                "hover:bg-gray-100 dark:hover:bg-gray-800",
                "focus:bg-gray-100 dark:focus:bg-gray-800 focus:outline-none",
              )}
            >
              {e.emoji}
            </button>
          );
        })}
      </div>
    </div>
  );
}
