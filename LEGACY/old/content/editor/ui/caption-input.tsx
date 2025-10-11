'use client';

import { Input } from '@/components/ui/input';

interface CaptionInputProps {
  caption: string;
  onChange: (caption: string) => void;
  placeholder?: string;
  autoFocus?: boolean;
  className?: string;
}

export function CaptionInput({ caption, onChange, placeholder = 'Add a caption...', autoFocus = false, className = '' }: CaptionInputProps) {
  return (
    <Input
      type="text"
      value={caption}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      className={`flex-1 bg-transparent border-none ${className}`}
      autoFocus={autoFocus}
    />
  );
}

export function CaptionDisplay({ caption, onClick, className = '' }: { caption: string; onClick?: () => void; className?: string }) {
  if (!caption) return null;

  return (
    <div className={`mt-2 text-sm text-muted-foreground text-center ${className}`} onClick={onClick}>
      {caption}
    </div>
  );
}
