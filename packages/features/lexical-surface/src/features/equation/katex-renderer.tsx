/**
 * KaTeX renderer. Ported from
 * `packages/lexical-playground/src/ui/KatexRenderer.tsx`. Uses
 * `katex.render` with `throwOnError: false` so equation errors display
 * inline (no error boundary needed).
 */
"use client";

import { useEffect, useRef } from "react";
import type * as React from "react";
import katex from "katex";

export function KatexRenderer({
  equation,
  inline,
  fontSize,
  align,
  selected,
  onClick,
  onDoubleClick,
}: Readonly<{
  equation: string;
  inline: boolean;
  fontSize?: number;
  align?: "left" | "center" | "right";
  selected?: boolean;
  onClick?: (e: React.MouseEvent) => void;
  onDoubleClick?: () => void;
}>) {
  const ref = useRef<HTMLSpanElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (el) {
      katex.render(equation, el, {
        displayMode: !inline,
        errorColor: "#cc0000",
        output: "html",
        strict: "warn",
        throwOnError: false,
        trust: false,
      });
    }
  }, [equation, inline]);

  const style: React.CSSProperties = {};
  if (fontSize) style.fontSize = `${fontSize}em`;
  if (!inline && align) style.textAlign = align;
  const className = [
    "inline-block rounded transition-shadow",
    !inline ? "w-full" : "",
    selected ? "ring-2 ring-blue-500 ring-offset-1" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <>
      <img
        src="data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"
        width="0"
        height="0"
        alt=""
      />
      <span
        role="button"
        tabIndex={-1}
        onClick={onClick}
        onDoubleClick={onDoubleClick}
        ref={ref}
        className={className}
        style={style}
      />
      <img
        src="data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"
        width="0"
        height="0"
        alt=""
      />
    </>
  );
}
