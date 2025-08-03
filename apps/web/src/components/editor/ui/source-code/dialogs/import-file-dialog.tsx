'use client';

import type React from 'react';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Upload } from 'lucide-react';
import { cn } from '@/lib/utils';

interface ImportFileDialogProps {
  showImportDialog: boolean;
  setShowImportDialog: (show: boolean) => void;
  importFileNames: string[];
  importContents: { name: string; content: string }[];
  importFileHasStates: boolean;
  setImportFileHasStates: (hasStates: boolean) => void;
  handleFileUpload: (e: React.ChangeEvent<HTMLInputElement>) => void;
  importFile: () => void;
  fileInputRef: React.RefObject<HTMLInputElement>;
  isPreview?: boolean;
}

export function ImportFileDialog({
  showImportDialog,
  setShowImportDialog,
  importFileNames,
  importContents,
  importFileHasStates,
  setImportFileHasStates,
  handleFileUpload,
  importFile,
  fileInputRef,
  isPreview = false,
}: ImportFileDialogProps) {
  return (
    <Dialog open={showImportDialog} onOpenChange={setShowImportDialog}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Import File</DialogTitle>
        </DialogHeader>
        <div className="grid gap-4 py-4">
          <div className="grid gap-2">
            <Label htmlFor="file-upload">Select file(s)</Label>
            <div
              className={cn('border-2 border-dashed rounded-md p-6 flex flex-col items-center justify-center cursor-pointer hover:border-primary/50', importFileNames.length > 0 ? 'border-primary/50' : 'border-muted-foreground/50')}
              onClick={() => fileInputRef.current?.click()}
            >
              <Upload className="h-8 w-8 mb-2 text-muted-foreground" />
              <p className="text-sm text-muted-foreground mb-1">{importFileNames.length === 0 ? 'Click to select files or drag and drop' : `${importFileNames.length} file(s) selected`}</p>
              {importFileNames.length > 0 && (
                <div className="w-full mt-2 max-h-32 overflow-y-auto">
                  <ul className="text-xs space-y-1">
                    {importFileNames.map((name, index) => (
                      <li key={index} className="p-1 bg-muted rounded flex items-center">
                        <span className="truncate">{name}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              <input id="file-upload" type="file" className="hidden" onChange={handleFileUpload} ref={fileInputRef} multiple />
            </div>
          </div>
        </div>
        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => {
              setShowImportDialog(false);
            }}
          >
            Cancel
          </Button>
          <Button onClick={importFile} disabled={importContents.length === 0}>
            Import {importContents.length > 0 ? `(${importContents.length})` : ''}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
