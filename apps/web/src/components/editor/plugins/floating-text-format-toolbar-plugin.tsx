'use client';

import { DropdownMenuTrigger } from '@/components/ui/dropdown-menu';

import { useCallback, useEffect, useRef, useState } from 'react';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { $getSelection, $isRangeSelection, FORMAT_TEXT_COMMAND, SELECTION_CHANGE_COMMAND } from 'lexical';
import { Bold, Italic, Heading1, Quote, LinkIcon, Type, MoreHorizontal } from 'lucide-react';
import { $createHeadingNode, $isHeadingNode, type HeadingTagType } from '@lexical/rich-text';
import { $setBlocksType } from '@lexical/selection';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
} from '@/components/ui/dropdown-menu';
import { TOGGLE_LINK_COMMAND } from '@lexical/link';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';

// Font families with their variations
const FONT_FAMILIES = [
  {
    name: 'Arial',
    family: 'Arial, sans-serif',
    variations: [
      { name: 'Regular', style: 'normal', weight: '400' },
      { name: 'Bold', style: 'normal', weight: '700' },
      { name: 'Italic', style: 'italic', weight: '400' },
      { name: 'Bold Italic', style: 'italic', weight: '700' },
    ],
  },
  {
    name: 'Liberation Sans',
    family: 'Liberation Sans, sans-serif',
    variations: [
      { name: 'Regular', style: 'normal', weight: '400' },
      { name: 'Bold', style: 'normal', weight: '700' },
      { name: 'Italic', style: 'italic', weight: '400' },
      { name: 'Bold Italic', style: 'italic', weight: '700' },
    ],
  },
  {
    name: 'Times New Roman',
    family: 'Times New Roman, serif',
    variations: [
      { name: 'Regular', style: 'normal', weight: '400' },
      { name: 'Bold', style: 'normal', weight: '700' },
      { name: 'Italic', style: 'italic', weight: '400' },
      { name: 'Bold Italic', style: 'italic', weight: '700' },
    ],
  },
  {
    name: 'Liberation Serif',
    family: 'Liberation Serif, serif',
    variations: [
      { name: 'Regular', style: 'normal', weight: '400' },
      { name: 'Bold', style: 'normal', weight: '700' },
      { name: 'Italic', style: 'italic', weight: '400' },
      { name: 'Bold Italic', style: 'italic', weight: '700' },
    ],
  },
  {
    name: 'Courier New',
    family: 'Courier New, monospace',
    variations: [
      { name: 'Regular', style: 'normal', weight: '400' },
      { name: 'Bold', style: 'normal', weight: '700' },
      { name: 'Italic', style: 'italic', weight: '400' },
      { name: 'Bold Italic', style: 'italic', weight: '700' },
    ],
  },
  {
    name: 'Liberation Mono',
    family: 'Liberation Mono, monospace',
    variations: [
      { name: 'Regular', style: 'normal', weight: '400' },
      { name: 'Bold', style: 'normal', weight: '700' },
      { name: 'Italic', style: 'italic', weight: '400' },
      { name: 'Bold Italic', style: 'italic', weight: '700' },
    ],
  },
  {
    name: 'Helvetica',
    family: 'Helvetica, sans-serif',
    variations: [
      { name: 'Regular', style: 'normal', weight: '400' },
      { name: 'Bold', style: 'normal', weight: '700' },
      { name: 'Italic', style: 'italic', weight: '400' },
      { name: 'Bold Italic', style: 'italic', weight: '700' },
    ],
  },
  {
    name: 'Georgia',
    family: 'Georgia, serif',
    variations: [
      { name: 'Regular', style: 'normal', weight: '400' },
      { name: 'Bold', style: 'normal', weight: '700' },
      { name: 'Italic', style: 'italic', weight: '400' },
      { name: 'Bold Italic', style: 'italic', weight: '700' },
    ],
  },
  {
    name: 'Verdana',
    family: 'Verdana, sans-serif',
    variations: [
      { name: 'Regular', style: 'normal', weight: '400' },
      { name: 'Bold', style: 'normal', weight: '700' },
      { name: 'Italic', style: 'italic', weight: '400' },
      { name: 'Bold Italic', style: 'italic', weight: '700' },
    ],
  },
  {
    name: 'Trebuchet MS',
    family: 'Trebuchet MS, sans-serif',
    variations: [
      { name: 'Regular', style: 'normal', weight: '400' },
      { name: 'Bold', style: 'normal', weight: '700' },
      { name: 'Italic', style: 'italic', weight: '400' },
      { name: 'Bold Italic', style: 'italic', weight: '700' },
    ],
  },
];

export function FloatingTextFormatToolbarPlugin() {
  const [editor] = useLexicalComposerContext();
  const toolbarRef = useRef<HTMLDivElement>(null);
  const [isText, setIsText] = useState(false);
  const [isLink, setIsLink] = useState(false);
  const [isBold, setIsBold] = useState(false);
  const [isItalic, setIsItalic] = useState(false);
  const [isUnderline, setIsUnderline] = useState(false);
  const [isStrikethrough, setIsStrikethrough] = useState(false);
  const [isSubscript, setIsSubscript] = useState(false);
  const [isSuperscript, setIsSuperscript] = useState(false);
  const [isCode, setIsCode] = useState(false);
  const [selectedElementKey, setSelectedElementKey] = useState<string | null>(null);
  const [position, setPosition] = useState({ top: 0, left: 0 });
  const [currentHeadingLevel, setCurrentHeadingLevel] = useState<HeadingTagType | null>(null);
  const [isQuote, setIsQuote] = useState(false);
  const [currentFontFamily, setCurrentFontFamily] = useState<string>('');
  const [currentFontSize, setCurrentFontSize] = useState<string>('');
  const [showFontSizeInput, setShowFontSizeInput] = useState(false);

  const [showLinkDialog, setShowLinkDialog] = useState(false);
  const [linkUrl, setLinkUrl] = useState('');
  const [selectedText, setSelectedText] = useState('');

  const updateToolbar = useCallback(() => {
    const selection = $getSelection();
    if (!$isRangeSelection(selection)) {
      setIsText(false);
      return;
    }

    setIsBold(selection.hasFormat('bold'));
    setIsItalic(selection.hasFormat('italic'));
    setIsUnderline(selection.hasFormat('underline'));
    setIsStrikethrough(selection.hasFormat('strikethrough'));
    setIsSubscript(selection.hasFormat('subscript'));
    setIsSuperscript(selection.hasFormat('superscript'));
    setIsCode(selection.hasFormat('code'));

    setIsText(selection.getTextContent().length > 0);

    // Check current heading level
    const anchorNode = selection.anchor.getNode();
    const element = anchorNode.getKey() === 'root' ? anchorNode : anchorNode.getTopLevelElementOrThrow();
    if ($isHeadingNode(element)) {
      setCurrentHeadingLevel(element.getTag());
    } else {
      setCurrentHeadingLevel(null);
    }

    // Check if current selection is a quote
    const parentElement = anchorNode.getParent();
    if (parentElement && parentElement.getType() === 'quote') {
      setIsQuote(true);
    } else {
      setIsQuote(false);
    }

    // Get current font family
    const nodes = selection.getNodes();
    if (nodes.length > 0) {
      const firstNode = nodes[0];
      // Ensure the node has getStyle and explicitly convert its result to string
      const style = firstNode.getStyle ? String(firstNode.getStyle()) : '';
      const fontFamilyMatch = style.match(/font-family:\s*([^;]+)/);
      if (fontFamilyMatch) {
        setCurrentFontFamily(fontFamilyMatch[1].replace(/['"]/g, ''));
      } else {
        setCurrentFontFamily('');
      }
    } else {
      setCurrentFontFamily(''); // Reset if no nodes are selected
    }

    // Get current font size
    if (nodes.length > 0) {
      const firstNode = nodes[0];
      const style = firstNode.getStyle ? String(firstNode.getStyle()) : '';
      const fontSizeMatch = style.match(/font-size:\s*([^;]+)/);
      if (fontSizeMatch) {
        setCurrentFontSize(fontSizeMatch[1].replace(/['"]/g, ''));
      } else {
        setCurrentFontSize('');
      }
    } else {
      setCurrentFontSize('');
    }

    const nativeSelection = window.getSelection();
    const range = nativeSelection?.getRangeAt(0);
    const rect = range?.getBoundingClientRect();

    if (rect && toolbarRef.current) {
      const toolbarRect = toolbarRef.current.getBoundingClientRect();
      setPosition({
        top: rect.top - toolbarRect.height - 8,
        left: rect.left + (rect.width - toolbarRect.width) / 2,
      });
    }
  }, []);

  const handleInsertLink = useCallback(() => {
    if (linkUrl.trim()) {
      editor.dispatchCommand(TOGGLE_LINK_COMMAND, linkUrl.trim());
      setShowLinkDialog(false);
      setLinkUrl('');
      setSelectedText('');
    }
  }, [editor, linkUrl]);

  const handleLinkButtonClick = useCallback(() => {
    editor.getEditorState().read(() => {
      const selection = $getSelection();
      if ($isRangeSelection(selection)) {
        const text = selection.getTextContent();
        setSelectedText(text);
        setShowLinkDialog(true);
      }
    });
  }, [editor]);

  const handleFontChange = useCallback(
    (fontFamily: string, fontWeight: string, fontStyle: string) => {
      editor.update(() => {
        const selection = $getSelection();
        if ($isRangeSelection(selection)) {
          const nodes = selection.getNodes();
          nodes.forEach((node) => {
            if (node.getTextContent()) {
              const currentStyle = node.getStyle() || '';
              // Remove existing font properties
              let newStyle = currentStyle
                .replace(/font-family:\s*[^;]+;?/g, '')
                .replace(/font-weight:\s*[^;]+;?/g, '')
                .replace(/font-style:\s*[^;]+;?/g, '');

              // Add new font properties
              newStyle += `font-family: ${fontFamily}; font-weight: ${fontWeight}; font-style: ${fontStyle};`;

              node.setStyle(newStyle.trim());
            }
          });
        }
      });
    },
    [editor],
  );

  const handleFontSizeChange = useCallback(
    (fontSize: string) => {
      editor.update(() => {
        const selection = $getSelection();
        if ($isRangeSelection(selection)) {
          const nodes = selection.getNodes();
          nodes.forEach((node) => {
            if (node.getTextContent()) {
              const currentStyle = node.getStyle() || '';
              let newStyle = currentStyle.replace(/font-size:\s*[^;]+;?/g, '');
              newStyle += `font-size: ${fontSize};`;
              node.setStyle(newStyle.trim());
            }
          });
        }
      });
    },
    [editor],
  );

  useEffect(() => {
    return editor.registerCommand(
      SELECTION_CHANGE_COMMAND,
      () => {
        updateToolbar();
        return false;
      },
      1,
    );
  }, [editor, updateToolbar]);

  if (!isText) {
    return null;
  }

  const getCurrentFontDisplay = () => {
    if (!currentFontFamily) return 'Default';

    const matchedFont = FONT_FAMILIES.find((font) => currentFontFamily.toLowerCase().includes(font.name.toLowerCase()));

    return matchedFont ? matchedFont.name : 'Custom';
  };

  return (
    <>
      <div
        ref={toolbarRef}
        className="fixed z-50 flex items-center gap-1 rounded-md border bg-popover p-1 shadow-md"
        style={{
          top: `${position.top}px`,
          left: `${position.left}px`,
        }}
      >
        <Button
          variant={isBold ? 'secondary' : 'ghost'}
          size="icon"
          onClick={() => {
            editor.dispatchCommand(FORMAT_TEXT_COMMAND, 'bold');
          }}
          className="h-8 w-8"
        >
          <Bold className="h-4 w-4" />
        </Button>
        <Button
          variant={isItalic ? 'secondary' : 'ghost'}
          size="icon"
          onClick={() => {
            editor.dispatchCommand(FORMAT_TEXT_COMMAND, 'italic');
          }}
          className="h-8 w-8"
        >
          <Italic className="h-4 w-4" />
        </Button>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant={currentHeadingLevel ? 'secondary' : 'ghost'} size="icon" className="h-8 w-8">
              {currentHeadingLevel ? <span className="text-xs font-bold">{currentHeadingLevel.toUpperCase()}</span> : <Heading1 className="h-4 w-4" />}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            <DropdownMenuItem
              onClick={() => {
                editor.update(() => {
                  const selection = $getSelection();
                  if ($isRangeSelection(selection)) {
                    $setBlocksType(selection, () => $createHeadingNode('h1'));
                  }
                });
              }}
            >
              <span className="text-2xl font-bold">H1 - Large Title</span>
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={() => {
                editor.update(() => {
                  const selection = $getSelection();
                  if ($isRangeSelection(selection)) {
                    $setBlocksType(selection, () => $createHeadingNode('h2'));
                  }
                });
              }}
            >
              <span className="text-xl font-bold">H2 - Medium Title</span>
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={() => {
                editor.update(() => {
                  const selection = $getSelection();
                  if ($isRangeSelection(selection)) {
                    $setBlocksType(selection, () => $createHeadingNode('h3'));
                  }
                });
              }}
            >
              <span className="text-lg font-bold">H3 - Small Title</span>
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={() => {
                editor.update(() => {
                  const selection = $getSelection();
                  if ($isRangeSelection(selection)) {
                    $setBlocksType(selection, () => $createHeadingNode('h4'));
                  }
                });
              }}
            >
              <span className="text-base font-bold">H4 - Extra Small</span>
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={() => {
                editor.update(() => {
                  const selection = $getSelection();
                  if ($isRangeSelection(selection)) {
                    $setBlocksType(selection, () => $createHeadingNode('h5'));
                  }
                });
              }}
            >
              <span className="text-sm font-bold">H5 - Tiny</span>
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={() => {
                editor.update(() => {
                  const selection = $getSelection();
                  if ($isRangeSelection(selection)) {
                    $setBlocksType(selection, () => $createHeadingNode('h6'));
                  }
                });
              }}
            >
              <span className="text-xs font-bold">H6 - Smallest</span>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
        <Button
          variant={isQuote ? 'secondary' : 'ghost'}
          size="icon"
          onClick={() => {
            editor.update(() => {
              const selection = $getSelection();
              if ($isRangeSelection(selection)) {
                const selectedText = selection.getTextContent();
                if (selectedText) {
                  const quotedText = `"${selectedText}"`;
                  selection.insertText(quotedText);
                }
              }
            });
          }}
          className="h-8 w-8"
        >
          <Quote className="h-4 w-4" />
        </Button>
        <Button variant={isLink ? 'secondary' : 'ghost'} size="icon" onClick={handleLinkButtonClick} className="h-8 w-8">
          <LinkIcon className="h-4 w-4" />
        </Button>

        {/* Font Family Selector */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="h-8 w-8">
              <Type className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent className="w-64">
            <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Current: {getCurrentFontDisplay()}</div>
            <DropdownMenuSeparator />
            {FONT_FAMILIES.map((fontFamily) => (
              <DropdownMenuSub key={fontFamily.name}>
                <DropdownMenuSubTrigger>
                  <span style={{ fontFamily: fontFamily.family }}>{fontFamily.name}</span>
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent>
                  {fontFamily.variations.map((variation) => (
                    <DropdownMenuItem
                      key={`${fontFamily.name}-${variation.name}`}
                      onClick={() => handleFontChange(fontFamily.family, variation.weight, variation.style)}
                    >
                      <span
                        style={{
                          fontFamily: fontFamily.family,
                          fontWeight: variation.weight,
                          fontStyle: variation.style,
                        }}
                      >
                        {variation.name}
                      </span>
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuSubContent>
              </DropdownMenuSub>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* Font Size Selector */}
        <DropdownMenu open={showFontSizeInput} onOpenChange={setShowFontSizeInput}>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="h-8 w-8">
              <span className="text-xs font-bold">{currentFontSize || 'Size'}</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent className="w-40">
            <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Current: {currentFontSize || 'Default'}</div>
            <DropdownMenuSeparator />
            {[8, 10, 12, 14, 16, 18, 20, 24, 28, 32, 36].map((size) => (
              <DropdownMenuItem key={size} onClick={() => handleFontSizeChange(`${size}px`)}>
                <span style={{ fontSize: `${size}px` }}>{size}px</span>
              </DropdownMenuItem>
            ))}
            <DropdownMenuSeparator />
            <div className="p-2">
              <Label htmlFor="custom-font-size" className="sr-only">
                Custom Size
              </Label>
              <Input
                id="custom-font-size"
                type="number"
                placeholder="Custom (px)"
                value={Number.parseInt(currentFontSize) || ''}
                onChange={(e) => {
                  const value = e.target.value;
                  if (value) {
                    setCurrentFontSize(`${value}px`);
                  } else {
                    setCurrentFontSize('');
                  }
                }}
                onBlur={() => {
                  if (currentFontSize) {
                    handleFontSizeChange(currentFontSize);
                  }
                  setShowFontSizeInput(false);
                }}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && currentFontSize) {
                    handleFontSizeChange(currentFontSize);
                    setShowFontSizeInput(false);
                  }
                }}
                className="w-full text-xs"
              />
            </div>
          </DropdownMenuContent>
        </DropdownMenu>

        <Button variant="ghost" size="icon" className="h-8 w-8">
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </div>
      <Dialog open={showLinkDialog} onOpenChange={setShowLinkDialog}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Add Link</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="selected-text">Selected Text</Label>
              <Input id="selected-text" value={selectedText} readOnly className="bg-muted" />
            </div>
            <div>
              <Label htmlFor="link-url">URL</Label>
              <Input
                id="link-url"
                type="url"
                placeholder="https://example.com"
                value={linkUrl}
                onChange={(e) => setLinkUrl(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    handleInsertLink();
                  }
                }}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowLinkDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleInsertLink} disabled={!linkUrl.trim()}>
              Add Link
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
