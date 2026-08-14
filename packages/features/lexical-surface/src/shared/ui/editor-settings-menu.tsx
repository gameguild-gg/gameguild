"use client";

import { useState } from "react";
import {
  Braces,
  Hash,
  Indent,
  Map,
  Maximize2,
  Menu,
  Palette,
  ScanLine,
  Space,
  Type,
  WrapText,
} from "lucide-react";
import { Button } from "@game-guild/ui/components/button";
import { Label } from "@game-guild/ui/components/label";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@game-guild/ui/components/popover";
import { Switch } from "@game-guild/ui/components/switch";
import { cn } from "@game-guild/ui/lib/utils";
import {
  hasDualModes,
  SHIKI_THEME_CONFIGS,
  SHIKI_THEME_KEYS,
  type ShikiTheme,
} from "../monaco/shiki-themes";
import type {
  EditorLineHighlight,
  EditorModalSize,
  EditorRenderWhitespace,
} from "./editor-preferences";
import type { FeatureEditorSettings } from "./use-feature-editor-settings";

const MODAL_SIZES: Array<{ value: EditorModalSize; label: string }> = [
  { value: "compact", label: "Compact" },
  { value: "widescreen", label: "Wide" },
  { value: "ultrawide", label: "Ultra" },
  { value: "fullscreen", label: "Full" },
];

export function EditorSettingsMenu({
  settings,
}: {
  settings: FeatureEditorSettings;
}) {
  const [tab, setTab] = useState<"editor" | "window">("editor");
  const options = settings.editor;

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          size="icon"
          className="h-8 w-8"
          title="Editor settings"
        >
          <Menu className="h-4 w-4" />
          <span className="sr-only">Editor settings</span>
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="z-[130] w-80 p-0">
        <div className="border-b px-4 pt-4">
          <div className="mb-3 flex items-center gap-2 text-sm font-semibold">
            <Braces className="h-4 w-4" />
            Editor settings
          </div>
          <div className="grid grid-cols-2 gap-1">
            {(["editor", "window"] as const).map((item) => (
              <button
                key={item}
                type="button"
                onClick={() => setTab(item)}
                className={cn(
                  "border-b-2 px-3 py-2 text-xs font-medium capitalize",
                  tab === item
                    ? "border-blue-500 text-foreground"
                    : "border-transparent text-muted-foreground hover:text-foreground",
                )}
              >
                {item}
              </button>
            ))}
          </div>
        </div>

        <div className="max-h-[min(65vh,560px)] space-y-4 overflow-y-auto p-4">
          {tab === "window" ? (
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
                    variant={
                      settings.modalSize === value ? "default" : "outline"
                    }
                    onClick={() => void settings.setModalSize(value)}
                  >
                    {label}
                  </Button>
                ))}
              </div>
            </section>
          ) : (
            <>
              <SelectSetting
                icon={<Palette className="h-4 w-4 text-indigo-500" />}
                label="Syntax theme"
                value={options.shikiTheme}
                options={SHIKI_THEME_KEYS.map((theme) => {
                  const config = SHIKI_THEME_CONFIGS[theme];
                  const traits = [
                    hasDualModes(config) ? "Light + dark" : "Single mode",
                    config.highContrast ? "High contrast" : undefined,
                  ].filter(Boolean);
                  return {
                    value: theme,
                    label: `${config.label} (${traits.join(", ")})`,
                  };
                })}
                onChange={(value) =>
                  void settings.setEditorOption(
                    "shikiTheme",
                    value as ShikiTheme,
                  )
                }
              />
              <div className="border-t" />
              <RangeSetting
                icon={<Type className="h-4 w-4 text-violet-500" />}
                label={`Font size: ${options.fontSize}px`}
                min={10}
                max={24}
                value={options.fontSize}
                onChange={(value) =>
                  void settings.setEditorOption("fontSize", value)
                }
              />
              <ToggleSetting
                icon={<Hash className="h-4 w-4 text-blue-500" />}
                label="Line numbers"
                checked={options.lineNumbers}
                onChange={(value) =>
                  void settings.setEditorOption("lineNumbers", value)
                }
              />
              <ToggleSetting
                icon={<WrapText className="h-4 w-4 text-emerald-500" />}
                label="Word wrap"
                checked={options.wordWrap}
                onChange={(value) =>
                  void settings.setEditorOption("wordWrap", value)
                }
              />
              <ToggleSetting
                icon={<Map className="h-4 w-4 text-amber-500" />}
                label="Minimap"
                checked={options.minimap}
                onChange={(value) =>
                  void settings.setEditorOption("minimap", value)
                }
              />
              <SelectSetting
                icon={<Indent className="h-4 w-4 text-cyan-500" />}
                label="Tab size"
                value={String(options.tabSize)}
                options={[
                  { value: "2", label: "2 spaces" },
                  { value: "4", label: "4 spaces" },
                  { value: "8", label: "8 spaces" },
                ]}
                onChange={(value) =>
                  void settings.setEditorOption("tabSize", Number(value))
                }
              />
              <SelectSetting
                icon={<Space className="h-4 w-4 text-rose-500" />}
                label="Whitespace"
                value={options.renderWhitespace}
                options={[
                  { value: "none", label: "Hidden" },
                  { value: "boundary", label: "Boundaries" },
                  { value: "all", label: "All" },
                ]}
                onChange={(value) =>
                  void settings.setEditorOption(
                    "renderWhitespace",
                    value as EditorRenderWhitespace,
                  )
                }
              />
              <SelectSetting
                icon={<ScanLine className="h-4 w-4 text-pink-500" />}
                label="Active line"
                value={options.renderLineHighlight}
                options={[
                  { value: "none", label: "Hidden" },
                  { value: "gutter", label: "Gutter" },
                  { value: "line", label: "Line" },
                  { value: "all", label: "Gutter and line" },
                ]}
                onChange={(value) =>
                  void settings.setEditorOption(
                    "renderLineHighlight",
                    value as EditorLineHighlight,
                  )
                }
              />
            </>
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}

function ToggleSetting({
  icon,
  label,
  checked,
  onChange,
}: {
  icon: React.ReactNode;
  label: string;
  checked: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <div className="flex items-center justify-between gap-3">
      <div className="flex items-center gap-2 text-sm">
        {icon}
        <span>{label}</span>
      </div>
      <Switch checked={checked} onCheckedChange={onChange} />
    </div>
  );
}

function RangeSetting({
  icon,
  label,
  min,
  max,
  value,
  onChange,
}: {
  icon: React.ReactNode;
  label: string;
  min: number;
  max: number;
  value: number;
  onChange: (value: number) => void;
}) {
  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2 text-sm">
        {icon}
        <span>{label}</span>
      </div>
      <input
        type="range"
        min={min}
        max={max}
        value={value}
        onChange={(event) => onChange(Number(event.target.value))}
        className="w-full accent-blue-500"
      />
    </div>
  );
}

function SelectSetting({
  icon,
  label,
  value,
  options,
  onChange,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  options: Array<{ value: string; label: string }>;
  onChange: (value: string) => void;
}) {
  return (
    <label className="block space-y-2">
      <span className="flex items-center gap-2 text-sm">
        {icon}
        {label}
      </span>
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="h-9 w-full rounded-md border bg-background px-3 text-sm"
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}
