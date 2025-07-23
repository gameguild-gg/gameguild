'use client';

import { useCallback } from 'react';
import { $getSelection, $isRangeSelection } from 'lexical';
import { Paintbrush } from 'lucide-react';
import { DropdownMenuSub, DropdownMenuSubContent, DropdownMenuSubTrigger, DropdownMenuSeparator } from '@/components/ui/dropdown-menu';
import { ColorPalette } from '@/components/editor/ui/color-palette';

interface BackgroundColorMenuComponentProps {
  editor: any;
  currentBackgroundColor: string;
  setCurrentBackgroundColor: (color: string) => void;
}

export function BackgroundColorMenuComponent({ editor, currentBackgroundColor, setCurrentBackgroundColor }: BackgroundColorMenuComponentProps) {
  const handleBackgroundColorChange = useCallback(
    (color: string) => {
      editor.update(() => {
        const selection = $getSelection();
        if ($isRangeSelection(selection)) {
          const nodes = selection.getNodes();
          nodes.forEach((node) => {
            if (node.getTextContent()) {
              const currentStyle = node.getStyle() || '';
              // Remove existing background-color but preserve other styles
              let newStyle = currentStyle.replace(/background-color:\s*[^;]+;?\s*/g, '');

              // Add new background color if not transparent
              if (color && color !== 'transparent' && color !== '') {
                // Ensure we don't have trailing semicolon issues
                if (newStyle && !newStyle.endsWith(';')) {
                  newStyle += ';';
                }
                newStyle += ` background-color: ${color};`;
              }

              // Clean up the style string
              newStyle = newStyle
                .replace(/;\s*;/g, ';')
                .replace(/^\s*;\s*/, '')
                .trim();

              node.setStyle(newStyle);
            }
          });
        }
      });
      setCurrentBackgroundColor(color);
    },
    [editor, setCurrentBackgroundColor],
  );

  const handleRemoveBackground = useCallback(() => {
    editor.update(() => {
      const selection = $getSelection();
      if ($isRangeSelection(selection)) {
        const nodes = selection.getNodes();
        nodes.forEach((node) => {
          if (node.getTextContent()) {
            const currentStyle = node.getStyle() || '';
            // Remove only background-color, preserve other styles
            const newStyle = currentStyle
              .replace(/background-color:\s*[^;]+;?\s*/g, '')
              .replace(/;\s*;/g, ';')
              .replace(/^\s*;\s*/, '')
              .trim();

            node.setStyle(newStyle);
          }
        });
      }
    });
    setCurrentBackgroundColor('');
  }, [editor, setCurrentBackgroundColor]);

  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger>
        <Paintbrush className="mr-2 h-4 w-4" />
        <span>Background Color</span>
        <div
          className="ml-auto h-4 w-4 rounded-full border"
          style={{
            backgroundColor: currentBackgroundColor || 'transparent',
            backgroundImage: currentBackgroundColor
              ? 'none'
              : 'linear-gradient(45deg, #ccc 25%, transparent 25%), linear-gradient(-45deg, #ccc 25%, transparent 25%), linear-gradient(45deg, transparent 75%, #ccc 75%), linear-gradient(-45deg, transparent 75%, #ccc 75%)',
            backgroundSize: currentBackgroundColor ? 'auto' : '4px 4px',
            backgroundPosition: currentBackgroundColor ? 'auto' : '0 0, 0 2px, 2px -2px, -2px 0px',
          }}
        />
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent side="right" align="start" className="w-64">
        <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Background Color</div>
        <DropdownMenuSeparator />
        <div className="p-2">
          <button onClick={handleRemoveBackground} className="w-full mb-2 px-3 py-2 text-sm border rounded hover:bg-accent transition-colors">
            Background Remove
          </button>
        </div>
        <ColorPalette selectedColor={currentBackgroundColor} onColorChange={handleBackgroundColorChange} customInputLabel="Custom:" />
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  );
}
