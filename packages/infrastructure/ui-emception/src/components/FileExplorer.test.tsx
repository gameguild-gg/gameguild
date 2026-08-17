import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';

import FileExplorer from './FileExplorer';
import type { TreeNode } from './ide-types';

function makeProps(overrides: Partial<Parameters<typeof FileExplorer>[0]> = {}) {
  return {
    files: {
      '/user/main.cpp': { path: '/user/main.cpp', type: 'text' as const, content: 'int main(){}' },
    },
    selectedPath: '/user/main.cpp',
    expandedDirs: new Set(['/user']),
    onSelectPath: jest.fn(),
    onToggleDir: jest.fn(),
    onOpenTab: jest.fn(),
    onCreateFile: jest.fn(),
    onRename: jest.fn(),
    onDelete: jest.fn(),
    fileTree: [
      { name: 'user', path: '/user', isDir: true, children: [] },
    ] as TreeNode[],
    ...overrides,
  };
}

function imageFile(name = 'pic.png', type = 'image/png'): File {
  return new File(['fake-bytes'], name, { type });
}

describe('FileExplorer upload + new-file header', () => {
  it('renders icon buttons instead of the old +Code/+Img text buttons', () => {
    render(<FileExplorer {...makeProps()} />);
    const newFile = screen.getByTestId('explorer-new-file');
    const upload = screen.getByTestId('explorer-upload');
    expect(newFile.textContent).toBe('📄+');
    expect(upload.textContent).toBe('⬆');
    expect(screen.queryByText('+Code')).toBeNull();
    expect(screen.queryByText('+Img')).toBeNull();
  });

  it('new-file button calls onCreateFile("text") (prompt-for-path flow)', () => {
    const promptSpy = jest.spyOn(window, 'prompt').mockReturnValue(null);
    const onCreateFile = jest.fn();
    render(<FileExplorer {...makeProps({ onCreateFile })} />);
    fireEvent.click(screen.getByTestId('explorer-new-file'));
    expect(onCreateFile).toHaveBeenCalledWith('text');
    promptSpy.mockRestore();
  });

  it('upload button is wired to a hidden image-only multi-file input', () => {
    render(<FileExplorer {...makeProps()} />);
    const input = screen.getByTestId('explorer-upload-input') as HTMLInputElement;
    expect(input.type).toBe('file');
    expect(input.accept).toBe('image/*');
    expect(input.multiple).toBe(true);
    expect(input.hidden).toBe(true);
  });

  it('input change fires onUploadFiles with image files only', () => {
    const onUploadFiles = jest.fn();
    render(<FileExplorer {...makeProps({ onUploadFiles })} />);
    const input = screen.getByTestId('explorer-upload-input');
    const img = imageFile();
    const txt = new File(['x'], 'notes.txt', { type: 'text/plain' });
    fireEvent.change(input, { target: { files: [img, txt] } });
    expect(onUploadFiles).toHaveBeenCalledTimes(1);
    expect(onUploadFiles).toHaveBeenCalledWith([img]);
  });

  it('drop on the explorer fires onUploadFiles with image files only', () => {
    const onUploadFiles = jest.fn();
    const { container } = render(<FileExplorer {...makeProps({ onUploadFiles })} />);
    const aside = container.querySelector('aside')!;
    const img = imageFile();
    const txt = new File(['x'], 'notes.txt', { type: 'text/plain' });
    fireEvent.drop(aside, { dataTransfer: { types: ['Files'], files: [img, txt] } });
    expect(onUploadFiles).toHaveBeenCalledTimes(1);
    expect(onUploadFiles).toHaveBeenCalledWith([img]);
  });

  it('drop without image files does not fire onUploadFiles', () => {
    const onUploadFiles = jest.fn();
    const { container } = render(<FileExplorer {...makeProps({ onUploadFiles })} />);
    const aside = container.querySelector('aside')!;
    fireEvent.drop(aside, {
      dataTransfer: { types: ['Files'], files: [new File(['x'], 'notes.txt', { type: 'text/plain' })] },
    });
    expect(onUploadFiles).not.toHaveBeenCalled();
  });

  it('dragOver highlights the explorer; dragLeave clears it', () => {
    const onUploadFiles = jest.fn();
    const { container } = render(<FileExplorer {...makeProps({ onUploadFiles })} />);
    const aside = container.querySelector('aside')!;
    expect(aside.getAttribute('style')).not.toContain('dashed');
    fireEvent.dragEnter(aside, { dataTransfer: { types: ['Files'] } });
    fireEvent.dragOver(aside, { dataTransfer: { types: ['Files'] } });
    expect(aside.getAttribute('style')).toContain('dashed');
    fireEvent.dragLeave(aside);
    expect(aside.getAttribute('style')).not.toContain('dashed');
  });
});

describe('allowCreateFiles workspace toggle', () => {
  it('renders the compact toggle ONLY in authoring mode (onAllowCreateFilesChange present)', () => {
    const onAllowCreateFilesChange = jest.fn();
    render(<FileExplorer {...makeProps({ allowCreateFiles: true, onAllowCreateFilesChange })} />);
    const toggle = screen.getByTestId('allow-student-create');
    expect(toggle.textContent).toBe('🔓');
    expect(toggle).toHaveAttribute('aria-pressed', 'true');
    expect(toggle).toHaveAttribute('title', 'Students can create new files');
  });

  it('toggle click fires onAllowCreateFilesChange with the flipped value', () => {
    const onAllowCreateFilesChange = jest.fn();
    render(<FileExplorer {...makeProps({ allowCreateFiles: true, onAllowCreateFilesChange })} />);
    fireEvent.click(screen.getByTestId('allow-student-create'));
    expect(onAllowCreateFilesChange).toHaveBeenCalledWith(false);
  });

  it('toggle shows 🔒 + aria-pressed=false when disallowed; clicking fires true', () => {
    const onAllowCreateFilesChange = jest.fn();
    render(<FileExplorer {...makeProps({ allowCreateFiles: false, onAllowCreateFilesChange })} />);
    const toggle = screen.getByTestId('allow-student-create');
    expect(toggle.textContent).toBe('🔒');
    expect(toggle).toHaveAttribute('aria-pressed', 'false');
    fireEvent.click(toggle);
    expect(onAllowCreateFilesChange).toHaveBeenCalledWith(true);
  });

  it('runtime mode (no onAllowCreateFilesChange) renders NO toggle', () => {
    render(<FileExplorer {...makeProps({ allowCreateFiles: false })} />);
    expect(screen.queryByTestId('allow-student-create')).toBeNull();
  });

  it('authoring mode NEVER hides the new-file/upload row, even when allowCreateFiles=false', () => {
    render(<FileExplorer {...makeProps({ allowCreateFiles: false, onAllowCreateFilesChange: jest.fn() })} />);
    expect(screen.getByTestId('explorer-new-file')).not.toBeNull();
    expect(screen.getByTestId('explorer-upload')).not.toBeNull();
  });

  it('runtime mode with allowCreateFiles=false hides the new-file/upload row', () => {
    render(<FileExplorer {...makeProps({ allowCreateFiles: false })} />);
    expect(screen.queryByTestId('explorer-new-file')).toBeNull();
    expect(screen.queryByTestId('explorer-upload')).toBeNull();
    expect(screen.queryByTestId('explorer-upload-input')).toBeNull();
  });

  it('runtime mode with allowCreateFiles=true (default) keeps the new-file/upload row', () => {
    render(<FileExplorer {...makeProps()} />);
    expect(screen.getByTestId('explorer-new-file')).not.toBeNull();
    expect(screen.getByTestId('explorer-upload')).not.toBeNull();
  });
});

describe('read-only Rename/Delete gating', () => {
  it('disables Rename and Delete with an explanatory title when the selected file is modifiable:false', () => {
    render(
      <FileExplorer
        {...makeProps({ fileMeta: { '/user/main.cpp': { visibility: 'Public', modifiable: false } } })}
      />,
    );
    const rename = screen.getByText('Rename');
    expect(rename).toBeDisabled();
    expect(rename).toHaveAttribute('title', 'Read-only file — renaming is disabled');
    const del = screen.getByText('Delete');
    expect(del).toBeDisabled();
    expect(del).toHaveAttribute('title', 'Read-only file — deletion is disabled');
  });

  it('keeps Rename/Delete enabled when the fileMeta entry is absent (absence = modifiable)', () => {
    render(<FileExplorer {...makeProps({ fileMeta: {} })} />);
    expect(screen.getByText('Rename')).toBeEnabled();
    expect(screen.getByText('Delete')).toBeEnabled();
  });

  it('keeps Rename/Delete enabled when the selected file is modifiable', () => {
    render(
      <FileExplorer
        {...makeProps({ fileMeta: { '/user/main.cpp': { visibility: 'Public', modifiable: true } } })}
      />,
    );
    expect(screen.getByText('Rename')).toBeEnabled();
    expect(screen.getByText('Delete')).toBeEnabled();
  });
});
