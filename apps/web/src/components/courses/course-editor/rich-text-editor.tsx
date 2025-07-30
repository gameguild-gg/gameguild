'use client';

import React from 'react';
import { Textarea } from '@/components/ui/textarea';

interface RichTextEditorProps {
  content: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
}

export function RichTextEditor({ content, onChange, placeholder = 'Start writing...', className = '' }: RichTextEditorProps) {
  return (
    <div className={`min-h-[200px] ${className}`}>
      <Textarea value={content} onChange={(e) => onChange(e.target.value)} placeholder={placeholder} className="min-h-[200px] resize-y" />
      <div className="mt-2 text-xs text-muted-foreground">
        For now, this is a simple text editor. Rich text editing with markdown support will be added soon.
      </div>
    </div>
  );
}
