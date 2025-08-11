'use client';

import { X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import type { ProgrammingLanguage } from '@/lib/types';

interface SettingsPanelProps {
  selectedLanguage: ProgrammingLanguage;
  setSelectedLanguage: (language: ProgrammingLanguage) => void;
  fontSize: number;
  setFontSize: (size: number) => void;
  tabSize: number;
  setTabSize: (size: number) => void;
  wordWrap: boolean;
  setWordWrap: (wrap: boolean) => void;
  showLineNumbers: boolean;
  setShowLineNumbers: (show: boolean) => void;
  showMinimap: boolean;
  setShowMinimap: (show: boolean) => void;
  isDarkTheme: boolean;
  setIsDarkTheme: (dark: boolean) => void;
  showExecution: boolean;
  setShowExecution: (show: boolean) => void;
  clearTerminalOnRun: boolean;
  setClearTerminalOnRun: (clear: boolean) => void;
  showTests: boolean;
  setShowTests: (show: boolean) => void;
  readonly: boolean;
  setReadonly: (readonly: boolean) => void;
  setShowSettings: (show: boolean) => void;
  updateSourceCode: (updates: Partial<{ readonly: boolean; showExecution: boolean; showTests: boolean }>) => void;
}

export function SettingsPanel({ isDarkTheme, readonly, setReadonly, showExecution, setShowExecution, showTests, setShowTests, setShowSettings, updateSourceCode }: SettingsPanelProps) {
  return (
    <div className="border-t p-3 bg-muted/50">
      <div className="flex items-center justify-between">
        <div className="text-sm font-medium">Settings</div>
        <Button variant="ghost" size="sm" onClick={() => setShowSettings(false)} className="h-6 w-6 p-0">
          <X className="h-4 w-4" />
        </Button>
      </div>
      <div className="mt-2 space-y-2">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Switch
              id="readonly-toggle"
              checked={readonly}
              onCheckedChange={(checked) => {
                // Primeiro atualize o estado local
                setReadonly(checked);

                // Em seguida, atualize o nó com uma cópia profunda dos dados atuais
                updateSourceCode({
                  readonly: checked,
                  // Não inclua os arquivos aqui, pois a função updateSourceCode já lida com isso
                });
              }}
            />
            <Label htmlFor="readonly-toggle" className="text-sm">
              Read-only
            </Label>
          </div>
        </div>
        <div className="flex items-center justify-between">
          <Label htmlFor="execution-toggle" className="text-sm">
            Show Execution Environment
          </Label>
          <Switch
            id="execution-toggle"
            checked={showExecution}
            onCheckedChange={(checked) => {
              setShowExecution(checked);
              updateSourceCode({ showExecution: checked });
            }}
          />
        </div>
        {showExecution && (
          <div className="flex items-center justify-between">
            <Label htmlFor="tests-toggle" className="text-sm">
              Enable Test Cases
            </Label>
            <Switch
              id="tests-toggle"
              checked={showTests}
              onCheckedChange={(checked) => {
                setShowTests(checked);
                updateSourceCode({ showTests: checked });
              }}
            />
          </div>
        )}
        {/* Show Tests Toggle */}
        {showExecution && (
          <div className="flex items-center justify-between">
            <span className="text-sm">Show Tests</span>
            <Switch checked={showTests} onCheckedChange={setShowTests} aria-label="Toggle tests visibility" />
          </div>
        )}
      </div>
    </div>
  );
}
