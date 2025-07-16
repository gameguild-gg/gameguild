'use client';

import { useState } from 'react';
import { X, Check } from 'lucide-react';
import { Button } from '@game-guild/ui/components';
import { Card, CardContent } from '@game-guild/ui/components';
import { Input } from '@game-guild/ui/components';
import { Switch } from '@game-guild/ui/components';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@game-guild/ui/components';

interface ThemeOption {
  id: string;
  name: string;
  preview: {
    background: string;
    sidebar: string;
    accent: string;
    progress: string;
  };
  isSelected?: boolean;
}

interface AppearanceModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave?: (settings: AppearanceSettings) => void;
}

interface AppearanceSettings {
  selectedTheme: string;
  primaryColor: string;
  transparentSidebar: boolean;
}

export function AppearanceModal({ isOpen, onClose, onSave }: AppearanceModalProps) {
  const [selectedTheme, setSelectedTheme] = useState('avocado-alien');
  const [primaryColor, setPrimaryColor] = useState('#90FB3F');
  const [transparentSidebar, setTransparentSidebar] = useState(true);

  const themes: ThemeOption[] = [
    {
      id: 'avocado-alien',
      name: 'Avocado Alien',
      preview: {
        background: 'bg-gray-900',
        sidebar: 'bg-gray-800',
        accent: 'bg-green-400',
        progress: 'bg-green-400',
      },
      isSelected: selectedTheme === 'avocado-alien',
    },
    {
      id: 'rainbow-candy',
      name: 'Rainbow Candy',
      preview: {
        background: 'bg-gray-900',
        sidebar: 'bg-gray-800',
        accent: 'bg-purple-400',
        progress: 'bg-purple-400',
      },
      isSelected: selectedTheme === 'rainbow-candy',
    },
    {
      id: 'honeydew-punch',
      name: 'Honeydew Punch',
      preview: {
        background: 'bg-gray-900',
        sidebar: 'bg-gray-800',
        accent: 'bg-cyan-400',
        progress: 'bg-cyan-400',
      },
      isSelected: selectedTheme === 'honeydew-punch',
    },
  ];

  const handleSave = () => {
    onSave?.({
      selectedTheme,
      primaryColor,
      transparentSidebar,
    });
    onClose();
  };

  const ThemePreview = ({ theme }: { theme: ThemeOption }) => (
    <Card
      className={`cursor-pointer transition-all duration-200 hover:scale-105 ${theme.isSelected ? 'ring-2 ring-green-500' : 'border-border'}`}
      onClick={() => setSelectedTheme(theme.id)}
    >
      <CardContent className="p-4">
        <div className={`${theme.preview.background} rounded-lg p-3 mb-3 relative overflow-hidden`}>
          {/* Browser chrome */}
          <div className="flex items-center gap-1 mb-2">
            <div className="w-2 h-2 bg-red-500 rounded-full"></div>
            <div className="w-2 h-2 bg-yellow-500 rounded-full"></div>
            <div className="w-2 h-2 bg-green-500 rounded-full"></div>
          </div>

          {/* Content area */}
          <div className="flex gap-2">
            {/* Sidebar */}
            <div className={`${theme.preview.sidebar} w-8 rounded`}>
              <div className="space-y-1 p-1">
                <div className={`h-2 ${theme.preview.accent} rounded opacity-60`}></div>
                <div className="h-2 bg-gray-600 rounded opacity-40"></div>
                <div className="h-2 bg-gray-600 rounded opacity-40"></div>
              </div>
            </div>

            {/* Main content */}
            <div className="flex-1 space-y-1">
              <div className="h-2 bg-gray-600 rounded opacity-60"></div>
              <div className={`h-1 ${theme.preview.progress} rounded w-3/4`}></div>
              <div className="h-1 bg-gray-600 rounded opacity-40 w-1/2"></div>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between">
          <span className="text-sm font-medium text-foreground">{theme.name}</span>
          {theme.isSelected && (
            <div className="w-5 h-5 bg-green-500 rounded-full flex items-center justify-center">
              <Check className="w-3 h-3 text-white" />
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader className="flex flex-row items-center justify-between space-y-0 pb-6">
          <DialogTitle className="text-xl font-semibold">Appearance</DialogTitle>
          <Button variant="ghost" size="icon" onClick={onClose} className="h-8 w-8">
            <X className="h-4 w-4" />
          </Button>
        </DialogHeader>

        <div className="space-y-8">
          {/* Interface Theme Section */}
          <div className="space-y-4">
            <div>
              <h3 className="text-lg font-medium text-foreground mb-1">Interface theme</h3>
              <p className="text-sm text-muted-foreground">Customise your workspace theme</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {themes.map((theme) => (
                <ThemePreview key={theme.id} theme={theme} />
              ))}
            </div>
          </div>

          {/* Primary Color Section */}
          <div className="space-y-4">
            <div>
              <h3 className="text-lg font-medium text-foreground mb-1">Customize primary color</h3>
              <p className="text-sm text-muted-foreground">Customize the look of your workspace. Feeling adventurous?</p>
            </div>

            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded border-2 border-border" style={{ backgroundColor: primaryColor }}></div>
              <Input value={primaryColor} onChange={(e) => setPrimaryColor(e.target.value)} placeholder="#90FB3F" className="font-mono text-sm max-w-32" />
            </div>
          </div>

          {/* Transparent Sidebar Section */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-lg font-medium text-foreground mb-1">Transparent sidebar</h3>
                <p className="text-sm text-muted-foreground">Add a transparency layer to your sidebar</p>
              </div>
              <Switch checked={transparentSidebar} onCheckedChange={setTransparentSidebar} />
            </div>
          </div>

          {/* Action Buttons */}
          <div className="flex justify-end gap-3 pt-6 border-t border-border">
            <Button variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button onClick={handleSave} className="bg-green-500 hover:bg-green-600 text-white">
              Save preferences
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
