"use client";

import type { ComponentProps } from "react";
import { SelectContent } from "@game-guild/ui/components/select";
import { cn } from "@game-guild/ui/lib/utils";

export function FeatureEditorSelectContent({
  className,
  ...props
}: ComponentProps<typeof SelectContent>) {
  return <SelectContent className={cn("z-[130]", className)} {...props} />;
}
