"use client";

import { Maximize2, Menu } from "lucide-react";
import { Button } from "@game-guild/ui/components/button";
import { Label } from "@game-guild/ui/components/label";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@game-guild/ui/components/popover";
import type { QuizEditorModalSize } from "./quiz-editor-preferences";
import type { QuizEditorSettings } from "./use-quiz-editor-settings";

const MODAL_SIZES: Array<{
  value: QuizEditorModalSize;
  label: string;
}> = [
  { value: "compact", label: "Compact" },
  { value: "widescreen", label: "Wide" },
  { value: "ultrawide", label: "Ultra" },
  { value: "fullscreen", label: "Full" },
];

export function QuizEditorSettingsMenu({
  settings,
}: {
  settings: QuizEditorSettings;
}) {
  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="h-8 w-8"
          aria-label="Quiz editor settings"
          title="Editor settings"
        >
          <Menu className="h-4 w-4" />
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="z-[130] w-72 p-4">
        <section className="space-y-3">
          <div className="flex items-center gap-2">
            <Maximize2 className="h-4 w-4 text-blue-500" />
            <Label>Workspace size</Label>
          </div>
          <div className="grid grid-cols-2 gap-2">
            {MODAL_SIZES.map(({ value, label }) => (
              <Button
                key={value}
                type="button"
                size="sm"
                variant={settings.modalSize === value ? "default" : "outline"}
                onClick={() => void settings.setModalSize(value)}
              >
                {label}
              </Button>
            ))}
          </div>
        </section>
      </PopoverContent>
    </Popover>
  );
}
