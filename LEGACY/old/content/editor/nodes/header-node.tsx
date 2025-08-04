'use client';

import type React from 'react';
import { useContext, useEffect, useState } from 'react';
import type { JSX } from 'react/jsx-runtime';
import { $getNodeByKey, DecoratorNode, type SerializedLexicalNode } from 'lexical';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { ChevronDown } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { EditorLoadingContext } from '../lexical-editor';

// Update the HeaderData interface to include the style
export interface HeaderData {
  text: string;
  level: 1 | 2 | 3 | 4 | 5 | 6;
  style: 'default' | 'underlined' | 'bordered' | 'gradient' | 'accent';
}

export interface SerializedHeaderNode extends SerializedLexicalNode {
  type: 'header';
  data: HeaderData;
  version: 1;
}

export class HeaderNode extends DecoratorNode<JSX.Element> {
  __data: HeaderData;

  // Update the constructor to define a default style
  constructor(data: HeaderData, key?: string) {
    super(key);
    this.__data = {
      ...data,
      style: data.style || 'default', // Default style if not specified
    };
  }

  static getType(): string {
    return 'header';
  }

  static clone(node: HeaderNode): HeaderNode {
    return new HeaderNode(node.__data, node.__key);
  }

  static importJSON(serializedNode: SerializedHeaderNode): HeaderNode {
    return new HeaderNode(serializedNode.data);
  }

  createDOM(): HTMLElement {
    return document.createElement('div');
  }

  updateDOM(): false {
    return false;
  }

  setData(data: HeaderData): void {
    const writable = this.getWritable();
    writable.__data = data;
  }

  exportJSON(): SerializedHeaderNode {
    return {
      type: 'header',
      data: this.__data,
      version: 1,
    };
  }

  decorate(): JSX.Element {
    return <HeaderComponent data={this.__data} nodeKey={this.__key} />;
  }
}

interface HeaderComponentProps {
  data: HeaderData;
  nodeKey: string;
}

// Update the HeaderComponent to include style selection
function HeaderComponent({ data, nodeKey }: HeaderComponentProps) {
  const [editor] = useLexicalComposerContext();
  const isLoading = useContext(EditorLoadingContext);
  const [text, setText] = useState(data.text);
  const [level, setLevel] = useState<1 | 2 | 3 | 4 | 5 | 6>(data.level);
  const [style, setStyle] = useState<'default' | 'underlined' | 'bordered' | 'gradient' | 'accent'>(data.style || 'default');
  const [isEditing, setIsEditing] = useState(!data.text && !isLoading);
  // First, add a new state variable to track whether the title is centered
  const [alignment, setAlignment] = useState<'left' | 'center' | 'right'>('left');

  useEffect(() => {
    setText(data.text);
    setLevel(data.level);
    setStyle(data.style || 'default');
  }, [data]);

  useEffect(() => {
    if (isLoading) {
      setIsEditing(false);
    }
  }, [isLoading]);

  // Then, update the updateHeader function to include the centered state
  const updateHeader = (newData: Partial<HeaderData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node instanceof HeaderNode) {
        node.setData({ ...data, ...newData });
      }
    });
  };

  const handleTextChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newText = e.target.value;
    setText(newText);
    updateHeader({ text: newText });
  };

  const handleLevelChange = (newLevel: 1 | 2 | 3 | 4 | 5 | 6) => {
    setLevel(newLevel);
    updateHeader({ level: newLevel });
  };

  const handleStyleChange = (newStyle: 'default' | 'underlined' | 'bordered' | 'gradient' | 'accent') => {
    setStyle(newStyle);
    updateHeader({ style: newStyle });
  };

  const getStyleClass = () => {
    switch (style) {
      case 'underlined':
        return 'border-b-2 border-primary pb-1';
      case 'bordered':
        return 'border-2 border-primary p-2 rounded-md';
      case 'gradient':
        return 'bg-gradient-to-r from-primary to-primary/30 text-primary-foreground p-2 rounded-md';
      case 'accent':
        return 'border-l-4 border-primary pl-2 py-1';
      default:
        return '';
    }
  };

  const renderHeader = () => {
    const styleClass = getStyleClass();
    const alignClass = `text-${alignment}`;

    switch (level) {
      case 1:
        return <h1 className={`text-4xl font-bold ${styleClass} ${alignClass}`}>{text}</h1>;
      case 2:
        return <h2 className={`text-3xl font-bold ${styleClass} ${alignClass}`}>{text}</h2>;
      case 3:
        return <h3 className={`text-2xl font-bold ${styleClass} ${alignClass}`}>{text}</h3>;
      case 4:
        return <h4 className={`text-xl font-bold ${styleClass} ${alignClass}`}>{text}</h4>;
      case 5:
        return <h5 className={`text-lg font-bold ${styleClass} ${alignClass}`}>{text}</h5>;
      case 6:
        return <h6 className={`text-base font-bold ${styleClass} ${alignClass}`}>{text}</h6>;
      default:
        return <h2 className={`text-3xl font-bold ${styleClass} ${alignClass}`}>{text}</h2>;
    }
  };

  if (!isEditing) {
    return (
      <div className="group relative my-4">
        {renderHeader()}
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setIsEditing(true)}
          className="absolute right-0 top-0 opacity-0 transition-opacity group-hover:opacity-100 h-8 w-8 p-0"
          title="Edit header"
        >
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
            />
          </svg>
        </Button>
      </div>
    );
  }

  return (
    <div className="my-4 space-y-4">
      <div className="rounded-lg border bg-card p-4 shadow-sm">
        <div className="mb-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10">
              <svg className="h-4 w-4 text-primary" fill="currentColor" viewBox="0 0 20 20">
                <path
                  fillRule="evenodd"
                  d="M3 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z"
                  clipRule="evenodd"
                />
              </svg>
            </div>
            <div>
              <h3 className="font-semibold">Header Settings</h3>
              <p className="text-sm text-muted-foreground">Configure your header appearance</p>
            </div>
          </div>
          <Button variant="ghost" size="sm" onClick={() => setIsEditing(false)} className="h-8 px-3 text-xs font-medium">
            <svg className="mr-1 h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
            </svg>
            Done
          </Button>
        </div>

        <div className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium">Header Level</label>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" className="w-full justify-between">
                    <span className="flex items-center gap-2">
                      <span className="flex h-5 w-5 items-center justify-center rounded bg-primary/10 text-xs font-bold text-primary">{level}</span>H{level}
                    </span>
                    <ChevronDown className="h-4 w-4 opacity-50" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-48">
                  <DropdownMenuItem onClick={() => handleLevelChange(1)} className="flex items-center gap-2">
                    <span className="flex h-5 w-5 items-center justify-center rounded bg-red-100 text-xs font-bold text-red-700">1</span>
                    <div>
                      <div className="font-medium">H1 - Main Title</div>
                      <div className="text-xs text-muted-foreground">Largest heading</div>
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleLevelChange(2)} className="flex items-center gap-2">
                    <span className="flex h-5 w-5 items-center justify-center rounded bg-orange-100 text-xs font-bold text-orange-700">2</span>
                    <div>
                      <div className="font-medium">H2 - Subtitle</div>
                      <div className="text-xs text-muted-foreground">Section heading</div>
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleLevelChange(3)} className="flex items-center gap-2">
                    <span className="flex h-5 w-5 items-center justify-center rounded bg-yellow-100 text-xs font-bold text-yellow-700">3</span>
                    <div>
                      <div className="font-medium">H3 - Section</div>
                      <div className="text-xs text-muted-foreground">Subsection</div>
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleLevelChange(4)} className="flex items-center gap-2">
                    <span className="flex h-5 w-5 items-center justify-center rounded bg-green-100 text-xs font-bold text-green-700">4</span>
                    <div>
                      <div className="font-medium">H4 - Subsection</div>
                      <div className="text-xs text-muted-foreground">Minor heading</div>
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleLevelChange(5)} className="flex items-center gap-2">
                    <span className="flex h-5 w-5 items-center justify-center rounded bg-blue-100 text-xs font-bold text-blue-700">5</span>
                    <div>
                      <div className="font-medium">H5 - Minor Title</div>
                      <div className="text-xs text-muted-foreground">Small heading</div>
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleLevelChange(6)} className="flex items-center gap-2">
                    <span className="flex h-5 w-5 items-center justify-center rounded bg-purple-100 text-xs font-bold text-purple-700">6</span>
                    <div>
                      <div className="font-medium">H6 - Smallest</div>
                      <div className="text-xs text-muted-foreground">Smallest heading</div>
                    </div>
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Style</label>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" className="w-full justify-between">
                    <span className="flex items-center gap-2">
                      <div
                        className={`h-3 w-3 rounded-sm ${
                          style === 'default'
                            ? 'bg-gray-300'
                            : style === 'underlined'
                              ? 'border-b-2 border-primary bg-transparent'
                              : style === 'bordered'
                                ? 'border border-primary bg-transparent'
                                : style === 'gradient'
                                  ? 'bg-gradient-to-r from-primary to-primary/50'
                                  : 'border-l-2 border-primary bg-transparent'
                        }`}
                      />
                      {style === 'default'
                        ? 'Default'
                        : style === 'underlined'
                          ? 'Underlined'
                          : style === 'bordered'
                            ? 'Bordered'
                            : style === 'gradient'
                              ? 'Gradient'
                              : 'Side Accent'}
                    </span>
                    <ChevronDown className="h-4 w-4 opacity-50" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-40">
                  <DropdownMenuItem onClick={() => handleStyleChange('default')} className="flex items-center gap-2">
                    <div className="h-3 w-3 rounded-sm bg-gray-300" />
                    Default
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleStyleChange('underlined')} className="flex items-center gap-2">
                    <div className="h-3 w-3 border-b-2 border-primary bg-transparent" />
                    Underlined
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleStyleChange('bordered')} className="flex items-center gap-2">
                    <div className="h-3 w-3 border border-primary bg-transparent rounded-sm" />
                    Bordered
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleStyleChange('gradient')} className="flex items-center gap-2">
                    <div className="h-3 w-3 rounded-sm bg-gradient-to-r from-primary to-primary/50" />
                    Gradient
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleStyleChange('accent')} className="flex items-center gap-2">
                    <div className="h-3 w-3 border-l-2 border-primary bg-transparent" />
                    Side Accent
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Alignment</label>
              <div className="flex rounded-md border overflow-hidden">
                <Button
                  variant={alignment === 'left' ? 'secondary' : 'ghost'}
                  size="sm"
                  onClick={() => setAlignment('left')}
                  title="Align left"
                  className="flex-1 rounded-none border-0 px-2"
                >
                  <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <line x1="21" x2="3" y1="6" y2="6" />
                    <line x1="15" x2="3" y1="12" y2="12" />
                    <line x1="17" x2="3" y1="18" y2="18" />
                  </svg>
                </Button>
                <Button
                  variant={alignment === 'center' ? 'secondary' : 'ghost'}
                  size="sm"
                  onClick={() => setAlignment('center')}
                  title="Align center"
                  className="flex-1 rounded-none border-0 border-l px-2"
                >
                  <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <line x1="21" x2="3" y1="6" y2="6" />
                    <line x1="18" x2="6" y1="12" y2="12" />
                    <line x1="21" x2="3" y1="18" y2="18" />
                  </svg>
                </Button>
                <Button
                  variant={alignment === 'right' ? 'secondary' : 'ghost'}
                  size="sm"
                  onClick={() => setAlignment('right')}
                  title="Align right"
                  className="flex-1 rounded-none border-0 border-l px-2"
                >
                  <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <line x1="21" x2="3" y1="6" y2="6" />
                    <line x1="21" x2="9" y1="12" y2="12" />
                    <line x1="21" x2="7" y1="18" y2="18" />
                  </svg>
                </Button>
              </div>
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">Header Text</label>
            <div className={`relative rounded-lg border p-4 transition-colors focus-within:border-primary ${getStyleClass()} text-${alignment}`}>
              <input
                type="text"
                value={text}
                onChange={handleTextChange}
                placeholder="Type your header text..."
                className="w-full border-none bg-transparent p-0 placeholder:text-muted-foreground/50 focus:outline-none focus:ring-0"
                style={{
                  fontSize:
                    level === 1 ? '2.25rem' : level === 2 ? '1.875rem' : level === 3 ? '1.5rem' : level === 4 ? '1.25rem' : level === 5 ? '1.125rem' : '1rem',
                  fontWeight: 'bold',
                }}
              />
              {!text && <div className="absolute right-2 top-2 text-xs text-muted-foreground/60">Preview as you type</div>}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// Update the $createHeaderNode function to include the style
export function $createHeaderNode(data: HeaderData): HeaderNode {
  return new HeaderNode({
    ...data,
    style: data.style || 'default',
  });
}
