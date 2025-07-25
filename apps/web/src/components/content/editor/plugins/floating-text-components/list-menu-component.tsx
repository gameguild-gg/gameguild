'use client';

import { useCallback } from 'react';
import { REMOVE_LIST_COMMAND } from '@lexical/list';
import { List } from 'lucide-react';
import { DropdownMenuSub, DropdownMenuSubContent, DropdownMenuSubTrigger, DropdownMenuItem, DropdownMenuSeparator } from '@/components/ui/dropdown-menu';
import { UnorderedListMenu } from './unordered-list-menu';
import { OrderedListMenu } from './ordered-list-menu';

interface ListMenuComponentProps {
  editor: any;
  currentListType: string;
  updateToolbar: () => void;
}

export function ListMenuComponent({ editor, currentListType, updateToolbar }: ListMenuComponentProps) {
  const handleRemoveList = useCallback(() => {
    editor.dispatchCommand(REMOVE_LIST_COMMAND, undefined);
  }, [editor]);

  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger>
        <List className="mr-2 h-4 w-4" />
        <span>Listas</span>
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent side="right" align="start" className="w-64">
        <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Tipos de Lista</div>
        <DropdownMenuSeparator />

        <UnorderedListMenu editor={editor} currentListType={currentListType} />
        <OrderedListMenu editor={editor} currentListType={currentListType} />

        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={handleRemoveList} onSelect={(e) => e.preventDefault()}>
          <span className="mr-2">✕</span>
          <span>Remover Lista</span>
        </DropdownMenuItem>
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  );
}
