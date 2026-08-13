"use client";

import { AlertCircle, ChevronDown, ChevronUp } from "lucide-react";
import { Button } from "@game-guild/ui/components/button";

export function ValidationPanel({
  error,
  collapsed,
  onCollapsedChange,
}: {
  error: string;
  collapsed: boolean;
  onCollapsedChange: (collapsed: boolean) => void;
}) {
  return (
    <div className="shrink-0 border-t border-red-300 bg-red-50 dark:border-red-900 dark:bg-red-950/40">
      <div className="flex h-10 items-center justify-between gap-3 px-3">
        <div className="flex min-w-0 items-center gap-2 text-sm font-medium text-red-800 dark:text-red-300">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span className="truncate">Validation error</span>
        </div>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="h-7 w-7 text-red-700 dark:text-red-300"
          onClick={() => onCollapsedChange(!collapsed)}
          title={
            collapsed ? "Show validation details" : "Hide validation details"
          }
        >
          {collapsed ? (
            <ChevronUp className="h-4 w-4" />
          ) : (
            <ChevronDown className="h-4 w-4" />
          )}
        </Button>
      </div>
      {!collapsed && (
        <div className="max-h-40 overflow-auto border-t border-red-200 p-3 font-mono text-xs leading-5 text-red-800 dark:border-red-900 dark:text-red-200">
          {error}
        </div>
      )}
    </div>
  );
}
