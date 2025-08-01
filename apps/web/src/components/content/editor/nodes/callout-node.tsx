'use client';

import { useContext, useEffect, useState } from 'react';
import { $getNodeByKey, DecoratorNode, type SerializedLexicalNode } from 'lexical';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import { Check, ChevronDown, Pencil } from 'lucide-react';
import type { JSX } from 'react/jsx-runtime';

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Callout as UICallout, type CalloutType } from "@/components/content/editor/ui/callout"
import { ContentEditMenu } from "@/components/content/editor/ui/content-edit-menu"
import { EditorLoadingContext } from "../lexical-editor"

export interface CalloutData {
  calloutTitle: string;
  content: string;
  type: CalloutType;
  isNew?: boolean;
}

export interface SerializedCalloutNode extends SerializedLexicalNode {
  type: 'callout';
  data: CalloutData;
  version: 1;
}

export class CalloutNode extends DecoratorNode<JSX.Element> {
  __data: CalloutData;

  // constructor(data: CalloutData, key?: string) {
  //   super(key);
  //   this.__data = {
  //     title: data.title || '',
  //     content: data.content || '',
  //     type: data.type || 'note',
  //     isNew: data.isNew,
  //   };
  // }

  static getType(): string {
    return 'callout';
  }

  static clone(node: CalloutNode): CalloutNode {
    return new CalloutNode(node.__data, node.__key);
  }

  constructor(data: CalloutData, key?: string) {
    super(key);
    this.__data = {
      calloutTitle: data.calloutTitle || '',
      content: data.content || '',
      type: data.type || 'note',
      isNew: data.isNew,
    };
  }

  createDOM(): HTMLElement {
    return document.createElement('div');
  }

  updateDOM(): false {
    return false;
  }

  setData(data: CalloutData): void {
    const writable = this.getWritable();
    writable.__data = data;
  }

  exportJSON(): SerializedCalloutNode {
    return {
      type: 'callout',
      data: this.__data,
      version: 1,
    };
  }

  decorate(): JSX.Element {
    return <CalloutComponent data={this.__data} nodeKey={this.__key} />;
  }
}

interface CalloutComponentProps {
  data: CalloutData;
  nodeKey: string;
}

function CalloutComponent({ data, nodeKey }: CalloutComponentProps) {
  const [editor] = useLexicalComposerContext();
  const isLoading = useContext(EditorLoadingContext);
  const [isEditing, setIsEditing] = useState((data.isNew || false) && !isLoading);
  const [calloutTitle, setcalloutTitle] = useState(data.calloutTitle || '');
  const [content, setContent] = useState(data.content || '');
  const [type, setType] = useState<CalloutType>(data.type || 'note');

  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey);
        if (node instanceof CalloutNode) {
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

  const updateCallout = (newData: Partial<CalloutData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node instanceof CalloutNode) {
        node.setData({ ...data, ...newData });
      }
    });
  };

  const handlecalloutTitleChange = (newcalloutTitle: string) => {
    setcalloutTitle(newcalloutTitle);
    updateCallout({ calloutTitle: newcalloutTitle });
  };

  const handleContentChange = (newContent: string) => {
    setContent(newContent);
    updateCallout({ content: newContent });
  };

  const handleTypeChange = (newType: CalloutType) => {
    setType(newType);
    updateCallout({ type: newType });
  };

  if (!isEditing) {
    return (
      <div className="my-4 relative">
        <UICallout type={type}>
          {calloutTitle && <div className="font-semibold mb-1">{calloutTitle}</div>}
          <div>{content}</div>
        </UICallout>
        <ContentEditMenu
          options={[
            {
              id: 'edit',
              icon: <Pencil className="h-4 w-4" />,
              label: 'Edit Callout',
              action: () => setIsEditing(true),
            },
          ]}
        />
      </div>
    );
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" onClick={() => setIsEditing(false)}>
      <div className="bg-white rounded-lg border shadow-lg p-6 max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto" onClick={(e) => e.stopPropagation()}>
        <div className="space-y-4">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-medium">Configure Callout</h3>
            <Button variant="ghost" size="sm" onClick={() => setIsEditing(false)}>
              <Check className="h-4 w-4 mr-2" />
              Done
            </Button>
          </div>

          <div className="grid gap-4">
            <div className="grid gap-2">
              <label htmlFor="callout-type" className="text-sm font-medium">
                Type
              </label>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" className="w-full justify-between">
                    <div className="flex items-center gap-2">{type}</div>
                    <ChevronDown className="h-4 w-4 ml-2" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-full">
                  <DropdownMenuItem onClick={() => handleTypeChange('note')}>Note</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange('abstract')}>Abstract</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange('info')}>Info</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange('tip')}>Tip</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange('success')}>Success</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange('question')}>Question</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange('warning')}>Warning</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange('failure')}>Failure</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange('danger')}>Danger</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange('bug')}>Bug</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange('example')}>Example</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange('quote')}>Quote</DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>

            <div className="grid gap-2">
              <label htmlFor="callout-calloutTitle" className="text-sm font-medium">
                calloutTitle (optional)
              </label>
              <Input
                id="callout-calloutTitle"
                value={calloutTitle}
                onChange={(e) => handlecalloutTitleChange(e.target.value)}
                placeholder="Callout calloutTitle"
              />
            </div>

            <div className="grid gap-2">
              <label htmlFor="callout-content" className="text-sm font-medium">
                Content
              </label>
              <Textarea id="callout-content" value={content} onChange={(e) => handleContentChange(e.target.value)} placeholder="Callout content" rows={4} />
            </div>

            <div className="mt-4">
              <h4 className="text-sm font-medium mb-2">Preview</h4>
              <UICallout type={type}>
                {calloutTitle && <div className="font-semibold mb-1">{calloutTitle}</div>}
                <div>{content}</div>
              </UICallout>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export function $createCalloutNode(data: Partial<CalloutData> = {}): CalloutNode {
  return new CalloutNode({
    calloutTitle: data.calloutTitle || '',
    content: data.content || '',
    type: data.type || 'note',
    isNew: true,
  });
}
