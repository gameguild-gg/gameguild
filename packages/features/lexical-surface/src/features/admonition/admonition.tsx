import * as React from "react";
import { cn } from "@game-guild/ui/lib/utils";

export type AdmonitionType =
  | "note"
  | "abstract"
  | "info"
  | "tip"
  | "success"
  | "question"
  | "warning"
  | "failure"
  | "danger"
  | "bug"
  | "example"
  | "quote"
  | "important"
  | "caution"
  | "attention"
  | "hint"
  | "check"
  | "summary";

export type AdmonitionDesign =
  "default" | "compact" | "bordered" | "vertical-bar";

const ACCENT_BY_TYPE: Record<AdmonitionType, string> = {
  note: "blue",
  abstract: "sky",
  info: "cyan",
  tip: "lime",
  success: "green",
  question: "amber",
  warning: "yellow",
  failure: "red",
  danger: "orange",
  bug: "stone",
  example: "teal",
  quote: "pink",
  important: "purple",
  caution: "rose",
  attention: "fuchsia",
  hint: "emerald",
  check: "indigo",
  summary: "violet",
};

export function Admonition({
  type,
  design,
  title,
  content,
  customBorderColor,
  customTextColor,
}: {
  type: AdmonitionType;
  design: AdmonitionDesign;
  title?: React.ReactNode;
  content?: React.ReactNode;
  customBorderColor?: string;
  customTextColor?: string;
}) {
  const accent = ACCENT_BY_TYPE[type];
  const className = cn(
    "rounded-md border p-4",
    design === "compact" && "border-l-4 py-3",
    design === "bordered" && "bg-transparent",
    design === "vertical-bar" &&
      "border-y-0 border-r-0 border-l-4 rounded-none",
    !customBorderColor &&
      `border-${accent}-300 bg-${accent}-50 dark:border-${accent}-700 dark:bg-${accent}-950/30`,
  );

  return (
    <section
      className={className}
      style={customBorderColor ? { borderColor: customBorderColor } : undefined}
    >
      {title && (
        <div className="mb-1 font-semibold" style={{ color: customTextColor }}>
          {title}
        </div>
      )}
      {content && (
        <div className="text-sm" style={{ color: customTextColor }}>
          {content}
        </div>
      )}
    </section>
  );
}
