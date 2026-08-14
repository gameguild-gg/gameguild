import { render, screen, fireEvent } from '@testing-library/react';

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
