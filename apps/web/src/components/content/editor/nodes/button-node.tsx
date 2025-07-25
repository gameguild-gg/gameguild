'use client';

import { useContext, useEffect, useState } from 'react';
import { $getNodeByKey, DecoratorNode, type SerializedLexicalNode } from 'lexical';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { ArrowRight, Check, ChevronDown, Copy, Download, ExternalLink, Mail, Pencil } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import type { JSX } from 'react/jsx-runtime';
import { EditorLoadingContext } from '../lexical-editor';

export type ButtonVariant = 'default' | 'destructive' | 'outline' | 'secondary' | 'ghost' | 'link';
export type ButtonSize = 'default' | 'sm' | 'lg' | 'icon';
export type ButtonActionType = 'url' | 'download' | 'copy' | 'email';

export interface ButtonData {
  text: string;
  url: string;
  actionType: ButtonActionType;
  variant: ButtonVariant;
  size: ButtonSize;
  showIcon: boolean;
  isNew?: boolean;
}

export interface SerializedButtonNode extends SerializedLexicalNode {
  type: 'button';
  data: ButtonData;
  version: 1;
}

export class ButtonNode extends DecoratorNode<JSX.Element> {
  __data: ButtonData;

  constructor(data: ButtonData, key?: string) {
    super(key);
    this.__data = {
      text: data.text || 'Click me',
      url: data.url || '',
      actionType: data.actionType || 'url',
      variant: data.variant || 'default',
      size: data.size || 'default',
      showIcon: data.showIcon ?? true,
      isNew: data.isNew,
    };
  }

  static getType(): string {
    return 'button';
  }

  static clone(node: ButtonNode): ButtonNode {
    return new ButtonNode(node.__data, node.__key);
  }

  static importJSON(serializedNode: SerializedButtonNode): ButtonNode {
    return new ButtonNode(serializedNode.data);
  }

  createDOM(): HTMLElement {
    return document.createElement('div');
  }

  updateDOM(): false {
    return false;
  }

  setData(data: ButtonData): void {
    const writable = this.getWritable();
    writable.__data = data;
  }

  exportJSON(): SerializedButtonNode {
    return {
      type: 'button',
      data: this.__data,
      version: 1,
    };
  }

  decorate(): JSX.Element {
    return <ButtonComponent data={this.__data} nodeKey={this.__key} />;
  }
}

interface ButtonComponentProps {
  data: ButtonData;
  nodeKey: string;
}

function ButtonComponent({ data, nodeKey }: ButtonComponentProps) {
  const [editor] = useLexicalComposerContext();
  const isLoading = useContext(EditorLoadingContext);
  const [isEditing, setIsEditing] = useState((data.isNew || false) && !isLoading);
  const [text, setText] = useState(data.text || 'Click me');
  const [url, setUrl] = useState(data.url || '');
  const [actionType, setActionType] = useState<ButtonActionType>(data.actionType || 'url');
  const [variant, setVariant] = useState<ButtonVariant>(data.variant || 'default');
  const [size, setSize] = useState<ButtonSize>(data.size || 'default');
  const [showIcon, setShowIcon] = useState(data.showIcon ?? true);

  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey);
        if (node instanceof ButtonNode) {
          const { isNew, ...rest } = data;
          node.setData(rest);
        }
      });
    }
  }, [data, editor, nodeKey]);

  useEffect(() => {
    if (isLoading) {
      setIsEditing(false);
    }
  }, [isLoading]);

  const updateButton = (newData: Partial<ButtonData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node instanceof ButtonNode) {
        node.setData({ ...data, ...newData });
      }
    });
  };

  const handleTextChange = (newText: string) => {
    setText(newText);
    updateButton({ text: newText });
  };

  const handleUrlChange = (newUrl: string) => {
    setUrl(newUrl);
    updateButton({ url: newUrl });
  };

  const handleActionTypeChange = (newActionType: ButtonActionType) => {
    setActionType(newActionType);
    updateButton({ actionType: newActionType });
  };

  const handleVariantChange = (newVariant: ButtonVariant) => {
    setVariant(newVariant);
    updateButton({ variant: newVariant });
  };

  const handleSizeChange = (newSize: ButtonSize) => {
    setSize(newSize);
    updateButton({ size: newSize });
  };

  const handleShowIconChange = (newShowIcon: boolean) => {
    setShowIcon(newShowIcon);
    updateButton({ showIcon: newShowIcon });
  };

  const getActionTypeLabel = (type: ButtonActionType): string => {
    switch (type) {
      case 'url':
        return 'Abrir página';
      case 'download':
        return 'Download';
      case 'copy':
        return 'Copiar texto';
      case 'email':
        return 'Enviar email';
      default:
        return 'Abrir página';
    }
  };

  const getVariantLabel = (variant: ButtonVariant): string => {
    switch (variant) {
      case 'default':
        return 'Padrão';
      case 'destructive':
        return 'Destrutivo';
      case 'outline':
        return 'Contorno';
      case 'secondary':
        return 'Secundário';
      case 'ghost':
        return 'Fantasma';
      case 'link':
        return 'Link';
      default:
        return 'Padrão';
    }
  };

  const getSizeLabel = (size: ButtonSize): string => {
    switch (size) {
      case 'default':
        return 'Médio';
      case 'sm':
        return 'Pequeno';
      case 'lg':
        return 'Grande';
      case 'icon':
        return 'Ícone';
      default:
        return 'Médio';
    }
  };

  const getActionIcon = () => {
    switch (actionType) {
      case 'url':
        return <ExternalLink className="h-4 w-4" />;
      case 'download':
        return <Download className="h-4 w-4" />;
      case 'copy':
        return <Copy className="h-4 w-4" />;
      case 'email':
        return <Mail className="h-4 w-4" />;
      default:
        return <ArrowRight className="h-4 w-4" />;
    }
  };

  const handleButtonAction = () => {
    switch (actionType) {
      case 'url':
        window.open(url, '_blank');
        break;
      case 'download':
        const link = document.createElement('a');
        link.href = url;
        link.download = '';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        break;
      case 'copy':
        navigator.clipboard.writeText(url);
        break;
      case 'email':
        window.location.href = `mailto:${url}`;
        break;
    }
  };

  if (!isEditing) {
    return (
      <div className="my-4 relative group flex justify-center">
        <Button variant={variant} size={size} className="relative group-hover:opacity-90" onClick={handleButtonAction}>
          {text}
          {showIcon && <span className="ml-2">{getActionIcon()}</span>}
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="absolute -right-8 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity"
          onClick={(e) => {
            e.stopPropagation();
            setIsEditing(true);
          }}
        >
          <Pencil className="h-4 w-4" />
        </Button>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white dark:bg-gray-900 rounded-lg border max-w-2xl w-full max-h-[90vh] overflow-y-auto shadow-xl">
        <div className="sticky top-0 bg-white dark:bg-gray-900 border-b px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
              <ArrowRight className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            </div>
            <div>
              <h3 className="text-lg font-semibold">Configurar Botão</h3>
              <p className="text-sm text-muted-foreground">Personalize o texto, ação e aparência do botão</p>
            </div>
          </div>
          <Button variant="ghost" size="sm" onClick={() => setIsEditing(false)}>
            <Check className="h-4 w-4 mr-2" />
            Concluído
          </Button>
        </div>

        <div className="p-6 space-y-6">
          <div className="grid gap-6">
            <div className="space-y-2">
              <Label htmlFor="button-text" className="text-sm font-medium">
                Texto do botão
              </Label>
              <Input id="button-text" value={text} onChange={(e) => handleTextChange(e.target.value)} placeholder="Clique aqui" className="w-full" />
            </div>

            <div className="space-y-2">
              <Label htmlFor="button-action" className="text-sm font-medium">
                Tipo de ação
              </Label>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" className="w-full justify-between">
                    <div className="flex items-center">
                      {actionType === 'url' && <ExternalLink className="h-4 w-4 mr-2" />}
                      {actionType === 'download' && <Download className="h-4 w-4 mr-2" />}
                      {actionType === 'copy' && <Copy className="h-4 w-4 mr-2" />}
                      {actionType === 'email' && <Mail className="h-4 w-4 mr-2" />}
                      {getActionTypeLabel(actionType)}
                    </div>
                    <ChevronDown className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-full">
                  <DropdownMenuItem onClick={() => handleActionTypeChange('url')}>
                    <ExternalLink className="h-4 w-4 mr-2" />
                    Abrir página
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleActionTypeChange('download')}>
                    <Download className="h-4 w-4 mr-2" />
                    Download
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleActionTypeChange('copy')}>
                    <Copy className="h-4 w-4 mr-2" />
                    Copiar texto
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleActionTypeChange('email')}>
                    <Mail className="h-4 w-4 mr-2" />
                    Enviar email
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>

            <div className="space-y-2">
              <Label htmlFor="button-url" className="text-sm font-medium">
                {actionType === 'url'
                  ? 'URL da página'
                  : actionType === 'download'
                    ? 'URL do arquivo'
                    : actionType === 'copy'
                      ? 'Texto a ser copiado'
                      : 'Endereço de email'}
              </Label>
              <Input
                id="button-url"
                value={url}
                onChange={(e) => handleUrlChange(e.target.value)}
                placeholder={
                  actionType === 'url'
                    ? 'https://exemplo.com'
                    : actionType === 'download'
                      ? 'https://exemplo.com/arquivo.pdf'
                      : actionType === 'copy'
                        ? 'Texto a ser copiado'
                        : 'email@exemplo.com'
                }
                className="w-full"
              />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="button-variant" className="text-sm font-medium">
                  Estilo
                </Label>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="outline" className="w-full justify-between">
                      {getVariantLabel(variant)}
                      <ChevronDown className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent>
                    <DropdownMenuItem onClick={() => handleVariantChange('default')}>Padrão</DropdownMenuItem>
                    <DropdownMenuItem onClick={() => handleVariantChange('secondary')}>Secundário</DropdownMenuItem>
                    <DropdownMenuItem onClick={() => handleVariantChange('destructive')}>Destrutivo</DropdownMenuItem>
                    <DropdownMenuItem onClick={() => handleVariantChange('outline')}>Contorno</DropdownMenuItem>
                    <DropdownMenuItem onClick={() => handleVariantChange('ghost')}>Fantasma</DropdownMenuItem>
                    <DropdownMenuItem onClick={() => handleVariantChange('link')}>Link</DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>

              <div className="space-y-2">
                <Label htmlFor="button-size" className="text-sm font-medium">
                  Tamanho
                </Label>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="outline" className="w-full justify-between">
                      {getSizeLabel(size)}
                      <ChevronDown className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent>
                    <DropdownMenuItem onClick={() => handleSizeChange('sm')}>Pequeno</DropdownMenuItem>
                    <DropdownMenuItem onClick={() => handleSizeChange('default')}>Médio</DropdownMenuItem>
                    <DropdownMenuItem onClick={() => handleSizeChange('lg')}>Grande</DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>

            <div className="flex items-center space-x-3 p-4 bg-muted/20 rounded-lg">
              <Switch id="show-icon" checked={showIcon} onCheckedChange={handleShowIconChange} />
              <Label htmlFor="show-icon" className="text-sm font-medium">
                Mostrar ícone
              </Label>
            </div>

            <div className="border rounded-lg p-6 bg-muted/10">
              <h4 className="text-sm font-medium mb-4 text-center">Prévia do Botão</h4>
              <div className="flex justify-center">
                <Button variant={variant} size={size} className={cn('pointer-events-none', size === 'icon' && 'p-0 w-10 h-10 rounded-full')}>
                  {size !== 'icon' ? text : ''}
                  {showIcon && <span className={size !== 'icon' ? 'ml-2' : ''}>{getActionIcon()}</span>}
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export function $createButtonNode(data: Partial<ButtonData> = {}): ButtonNode {
  return new ButtonNode({
    text: data.text || 'Click me',
    url: data.url || '',
    actionType: data.actionType || 'url',
    variant: data.variant || 'default',
    size: data.size || 'default',
    showIcon: data.showIcon ?? true,
    isNew: true,
  });
}
