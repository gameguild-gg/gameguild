"use client";

import { useRef, useState } from "react";
import {
  AlertCircle,
  CheckCircle,
  FileText,
  GitBranch,
  LayoutTemplate,
  Save,
  Users,
} from "lucide-react";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import {
  Select,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import { Switch } from "@game-guild/ui/components/switch";
import { FeatureEditorShell } from "../../../shared/ui/feature-editor-shell";
import { FeatureEditorSelectContent } from "../../../shared/ui/feature-editor-select";
import { useFeatureEditorSettings } from "../../../shared/ui/use-feature-editor-settings";
import { ValidationPanel } from "../../../shared/ui/validation-panel";
import { useDarkMode } from "../../../shared/ui/use-dark-mode";
import type { MermaidData } from "../mermaid-data";
import { MonacoMermaidEditor } from "./monaco-mermaid-editor";
import { MermaidTemplateSelector } from "./mermaid-template-selector";
import {
  AVAILABLE_MERMAID_THEMES,
  MERMAID_THEME_DESCRIPTIONS,
  MERMAID_THEME_MODE_DESCRIPTIONS,
  type MermaidTheme,
  type MermaidThemeMode,
} from "../theme/mermaid-theme-helper";
import {
  MermaidValidator,
  type MermaidValidationResult,
} from "./mermaid-validator";
import { MermaidViewer } from "../rendering/mermaid-viewer";

const EMPTY_MERMAID_DATA: MermaidData = {
  code: "",
  type: "flowchart",
  title: "",
  caption: "",
  size: 100,
  theme: "default",
  themeMode: "system",
};

export function MermaidEditor({
  initialData,
  onSave,
  onCancel,
}: {
  initialData?: MermaidData;
  onSave: (data: MermaidData) => void;
  onCancel: () => void;
}) {
  const isDarkMode = useDarkMode();
  const settings = useFeatureEditorSettings("mermaid");
  const initial = { ...EMPTY_MERMAID_DATA, ...initialData };
  const [data, setData] = useState<MermaidData>(initial);
  const dataRef = useRef(data);
  const [previewData, setPreviewData] = useState<MermaidData>(initial);
  const [showTemplates, setShowTemplates] = useState(!initialData);
  const [autoUpdate, setAutoUpdate] = useState(true);
  const [validation, setValidation] = useState<MermaidValidationResult>({
    isValid: true,
  });
  const [errorCollapsed, setErrorCollapsed] = useState(false);

  const updateData = (
    patch: Partial<MermaidData>,
    updatePreviewImmediately = true,
  ) => {
    const next = { ...dataRef.current, ...patch };
    dataRef.current = next;
    setData(next);
    if (autoUpdate && updatePreviewImmediately && validation.isValid) {
      setPreviewData(next);
    }
  };

  const handleValidation = (result: MermaidValidationResult) => {
    setValidation(result);
    setErrorCollapsed(result.isValid);
    if (result.isValid && autoUpdate) setPreviewData(dataRef.current);
  };

  const updatePreview = async () => {
    const result = await MermaidValidator.validateCode(dataRef.current.code);
    handleValidation(result);
    if (result.isValid) setPreviewData(dataRef.current);
  };

  const footer = showTemplates ? undefined : (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
      <div className="min-w-0 flex-1">
        <Label htmlFor="mermaid-caption">Caption</Label>
        <Input
          id="mermaid-caption"
          value={data.caption ?? ""}
          onChange={(event) => updateData({ caption: event.target.value })}
          placeholder="Optional diagram caption"
          className="mt-1"
        />
      </div>
      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button
          onClick={() => onSave(dataRef.current)}
          disabled={!validation.isValid || !data.code.trim()}
        >
          <Save className="mr-2 h-4 w-4" />
          Save Diagram
        </Button>
      </div>
    </div>
  );

  return (
    <FeatureEditorShell
      settings={settings}
      title="Mermaid Diagram Editor"
      icon={<GitBranch className="h-5 w-5 text-blue-500" />}
      onClose={onCancel}
      headerMeta={
        <div className="flex items-center gap-2 text-sm">
          {validation.isValid ? (
            <>
              <CheckCircle className="h-4 w-4 text-emerald-500" />
              <span className="text-emerald-600 dark:text-emerald-400">
                Valid
              </span>
            </>
          ) : (
            <>
              <AlertCircle className="h-4 w-4 text-red-500" />
              <span className="text-red-600 dark:text-red-400">Invalid</span>
            </>
          )}
        </div>
      }
      headerActions={
        !showTemplates && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowTemplates(true)}
          >
            <LayoutTemplate className="mr-2 h-4 w-4" />
            <span className="hidden sm:inline">Templates</span>
          </Button>
        )
      }
      footer={footer}
      bodyClassName="overflow-y-auto lg:overflow-hidden"
    >
      {showTemplates ? (
        <MermaidTemplateSelector
          onCancel={() => (initialData ? setShowTemplates(false) : onCancel())}
          onSelect={(template) => {
            updateData({ code: template.code, type: template.type }, false);
            setShowTemplates(false);
          }}
        />
      ) : (
        <>
          <div className="flex shrink-0 flex-wrap items-end gap-3 border-b bg-gray-50 p-3 dark:border-gray-800 dark:bg-gray-900">
            <div className="min-w-44 flex-1 sm:max-w-64">
              <Label htmlFor="mermaid-title">Title</Label>
              <Input
                id="mermaid-title"
                value={data.title ?? ""}
                onChange={(event) => updateData({ title: event.target.value })}
                placeholder="Optional title"
                className="mt-1"
              />
            </div>
            <div className="w-40">
              <Label>Theme</Label>
              <Select
                value={data.theme ?? "default"}
                onValueChange={(theme) =>
                  updateData({ theme: theme as MermaidTheme })
                }
              >
                <SelectTrigger className="mt-1 w-full">
                  <SelectValue />
                </SelectTrigger>
                <FeatureEditorSelectContent>
                  {AVAILABLE_MERMAID_THEMES.map((theme) => (
                    <SelectItem key={theme} value={theme}>
                      {MERMAID_THEME_DESCRIPTIONS[theme]}
                    </SelectItem>
                  ))}
                </FeatureEditorSelectContent>
              </Select>
            </div>
            <div className="w-40">
              <Label>Theme mode</Label>
              <Select
                value={data.themeMode ?? "system"}
                onValueChange={(themeMode) =>
                  updateData({ themeMode: themeMode as MermaidThemeMode })
                }
              >
                <SelectTrigger className="mt-1 w-full">
                  <SelectValue />
                </SelectTrigger>
                <FeatureEditorSelectContent>
                  {Object.entries(MERMAID_THEME_MODE_DESCRIPTIONS).map(
                    ([mode, description]) => (
                      <SelectItem key={mode} value={mode}>
                        {description.label}
                      </SelectItem>
                    ),
                  )}
                </FeatureEditorSelectContent>
              </Select>
            </div>
            <div className="w-28">
              <Label htmlFor="mermaid-size">Width (%)</Label>
              <Input
                id="mermaid-size"
                type="number"
                min={20}
                max={100}
                value={data.size ?? 100}
                onChange={(event) =>
                  updateData({
                    size: Math.max(
                      20,
                      Math.min(100, Number(event.target.value)),
                    ),
                  })
                }
                className="mt-1"
              />
            </div>
            <div className="flex h-9 items-center gap-2 pb-0.5">
              <Switch
                id="mermaid-auto-update"
                checked={autoUpdate}
                onCheckedChange={(checked) => {
                  setAutoUpdate(checked);
                  if (checked && validation.isValid)
                    setPreviewData(dataRef.current);
                }}
              />
              <Label htmlFor="mermaid-auto-update">Live preview</Label>
            </div>
            {!autoUpdate && (
              <Button variant="outline" onClick={() => void updatePreview()}>
                Update preview
              </Button>
            )}
            <div className="ml-auto rounded border bg-background px-2 py-1 text-xs text-muted-foreground">
              {data.type}
            </div>
          </div>

          <div className="flex min-h-0 flex-1 flex-col lg:flex-row">
            <section className="flex min-h-[320px] min-w-0 flex-1 flex-col border-b lg:min-h-0 lg:border-b-0 lg:border-r dark:border-gray-800">
              <div className="flex h-11 shrink-0 items-center gap-2 border-b bg-gray-50 px-4 text-sm font-medium dark:border-gray-800 dark:bg-gray-900">
                <FileText className="h-4 w-4 text-blue-500" /> Mermaid code
              </div>
              <div className="min-h-0 flex-1 bg-gray-950 p-2">
                <MonacoMermaidEditor
                  value={data.code}
                  onChange={(code) => updateData({ code: code ?? "" }, false)}
                  onValidationChange={handleValidation}
                  theme={isDarkMode ? "dark" : "light"}
                  options={settings.editor}
                />
              </div>
              {!validation.isValid && (
                <ValidationPanel
                  error={validation.error ?? "Invalid Mermaid syntax"}
                  collapsed={errorCollapsed}
                  onCollapsedChange={setErrorCollapsed}
                />
              )}
            </section>

            <section className="flex min-h-[360px] min-w-0 flex-1 flex-col lg:min-h-0">
              <div className="flex h-11 shrink-0 items-center gap-2 border-b bg-gray-50 px-4 text-sm font-medium dark:border-gray-800 dark:bg-gray-900">
                <Users className="h-4 w-4 text-blue-500" /> Preview
              </div>
              <div className="min-h-0 flex-1 overflow-auto bg-white p-4 dark:bg-gray-950">
                <MermaidViewer
                  data={previewData}
                  size={100}
                  showControls
                  allowFullscreen
                  className="min-h-full"
                />
              </div>
            </section>
          </div>
        </>
      )}
    </FeatureEditorShell>
  );
}
