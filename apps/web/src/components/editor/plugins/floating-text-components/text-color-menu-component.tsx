'use client';

import { useCallback } from 'react';
import { $getSelection, $isRangeSelection } from 'lexical';
import { Palette } from 'lucide-react';
import { DropdownMenuSub, DropdownMenuSubContent, DropdownMenuSubTrigger, DropdownMenuSeparator } from '@/components/ui/dropdown-menu';
import { ColorPalette } from '@/components/editor/ui/color-palette';

interface TextColorMenuComponentProps {
  editor: any;
  currentTextColor: string;
  setCurrentTextColor: (color: string) => void;
}

export function TextColorMenuComponent({ editor, currentTextColor, setCurrentTextColor }: TextColorMenuComponentProps) {
  const handleTextColorChange = useCallback(
    (color: string) => {
      editor.update(() => {
        const selection = $getSelection();
        if ($isRangeSelection(selection)) {
          const nodes = selection.getNodes();
          nodes.forEach((node) => {
            if (node.getTextContent()) {
              const currentStyle = node.getStyle() || '';
              // Remove existing color but preserve background-color and other styles
              let newStyle = currentStyle.replace(/(?<!background-)color:\s*[^;]+;?\s*/g, '');

              // Add new text color
              if (color && color !== 'transparent' && color !== '') {
                // Ensure we don't have trailing semicolon issues
                if (newStyle && !newStyle.endsWith(';')) {
                  newStyle += ';';
                }
                newStyle += ` color: ${color};`;
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
      setCurrentTextColor(color);
    },
    [editor, setCurrentTextColor],
  );

  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger>
        <Palette className="mr-2 h-4 w-4" />
        <span>Text Color</span>
        <div className="ml-auto h-4 w-4 rounded-full border" style={{ backgroundColor: currentTextColor || 'transparent' }} />
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent side="right" align="start" className="w-64">
        <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Text Color</div>
        <DropdownMenuSeparator />
        <ColorPalette selectedColor={currentTextColor} onColorChange={handleTextColorChange} customInputLabel="Custom:" />
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  );
}
