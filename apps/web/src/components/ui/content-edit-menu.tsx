'use client';

import React from 'react';
import { Button } from '@/components/ui/button';
import { MoreHorizontal, Edit, Trash2, Copy } from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

export interface EditMenuOption {
  label: string;
  icon?: React.ReactNode;
  onClick: () => void;
  variant?: 'default' | 'destructive';
}

export interface ContentEditMenuProps {
  options?: EditMenuOption[];
  onEdit?: () => void;
  onDelete?: () => void;
  onDuplicate?: () => void;
  className?: string;
}

export function ContentEditMenu({ 
  options = [], 
  onEdit, 
  onDelete, 
  onDuplicate, 
  className 
}: ContentEditMenuProps) {
  const defaultOptions: EditMenuOption[] = [];
  
  if (onEdit) {
    defaultOptions.push({
      label: 'Edit',
      icon: <Edit className="w-4 h-4" />,
      onClick: onEdit,
    });
  }

  if (onDuplicate) {
    defaultOptions.push({
      label: 'Duplicate',
      icon: <Copy className="w-4 h-4" />,
      onClick: onDuplicate,
    });
  }

  if (onDelete) {
    defaultOptions.push({
      label: 'Delete',
      icon: <Trash2 className="w-4 h-4" />,
      onClick: onDelete,
      variant: 'destructive',
    });
  }

  const allOptions = [...defaultOptions, ...options];

  if (allOptions.length === 0) {
    return null;
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" className={className}>
          <MoreHorizontal className="w-4 h-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {allOptions.map((option, index) => (
          <DropdownMenuItem
            key={index}
            onClick={option.onClick}
            className={option.variant === 'destructive' ? 'text-destructive' : ''}
          >
            {option.icon && <span className="mr-2">{option.icon}</span>}
            {option.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
