import { type JSX, type ReactNode, useCallback, useRef, useState } from 'react';
import type { DockGroup, TreeNode, WorkspaceFile } from './ide-types.js';
import { fileName } from './ide-utils.js';

/** Simple VS Code–style file/folder icons using Unicode symbols + color */
function fileIcon(name: string, isDir = false, isOpen = false): { icon: string; color: string } {
    if (isDir) return { icon: isOpen ? '📂' : '📁', color: '#e6c07b' };
    if (name.endsWith('.cpp') || name.endsWith('.cc') || name.endsWith('.cxx')) return { icon: '󰙱', color: '#6FB3D2' };
    if (name.endsWith('.c')) return { icon: '󰙱', color: '#6FB3D2' };
    if (name.endsWith('.h') || name.endsWith('.hpp')) return { icon: '󰙱', color: '#8cc8bf' };
    if (name.endsWith('.svg') || name.endsWith('.png') || name.endsWith('.jpg') || name.endsWith('.jpeg') || name.endsWith('.gif') || name.endsWith('.webp'))
        return { icon: '🖼', color: '#e06c75' };
    if (name.endsWith('.json')) return { icon: '{}', color: '#d19a66' };
    if (name.endsWith('.md')) return { icon: 'M↓', color: '#7fc1e8' };
    if (name === 'sdl-canvas' || name.includes('canvas')) return { icon: '🎨', color: '#c792ea' };
    return { icon: '📄', color: '#abb2bf' };
}

/** Map TabType to a small descriptive icon + color */
export function tabIcon(type: string, name: string): { icon: string; color: string } {
    if (type === 'image') return { icon: '🖼', color: '#e06c75' };
    if (type === 'canvas') return { icon: '🎨', color: '#c792ea' };
    return fileIcon(name);
}

const actionBtnStyle: React.CSSProperties = {
    border: '1px solid #45475a',
    background: '#1e1e2e',
    color: '#cdd6f4',
    borderRadius: 4,
    fontSize: '0.72rem',
    padding: '0.12rem 0.35rem',
    cursor: 'pointer',
};

interface FileExplorerProps {
    files: Record<string, WorkspaceFile>;
    selectedPath: string;
    expandedDirs: Set<string>;
    onSelectPath: (path: string) => void;
    onToggleDir: (path: string) => void;
    onOpenTab: (path: string, group?: DockGroup) => void;
    onCreateFile: (kind: 'text' | 'image') => void;
    onRename: () => void;
    onDelete: () => void;
    fileTree: TreeNode[];
    footer?: ReactNode;
}

export default function FileExplorer({
    files,
    selectedPath,
    expandedDirs,
    onSelectPath,
    onToggleDir,
    onOpenTab,
    onCreateFile,
    onRename,
    onDelete,
    fileTree,
    footer,
}: FileExplorerProps) {
    const [ctxMenu, setCtxMenu] = useState<{ x: number; y: number; path: string } | null>(null);
    const containerRef = useRef<HTMLDivElement>(null);

    const dismissCtx = useCallback(() => setCtxMenu(null), []);

    const renderFileNode = (node: TreeNode, depth = 0): JSX.Element => {
        if (node.isDir) {
            const isOpen = expandedDirs.has(node.path);
            const { icon, color } = fileIcon(node.name, true, isOpen);
            return (
                <div key={node.path}>
                    <button
                        onClick={() => onToggleDir(node.path)}
                        style={{
                            width: '100%',
                            textAlign: 'left',
                            border: 'none',
                            background: 'transparent',
                            color: '#bac2de',
                            padding: `0.22rem 0.4rem 0.22rem ${0.35 + depth * 1.1}rem`,
                            fontSize: '0.8rem',
                            cursor: 'pointer',
                            display: 'flex',
                            alignItems: 'center',
                            gap: '0.4rem',
                        }}
                    >
                        <span style={{ color: '#a6adc8', fontSize: '0.6rem', width: '0.7rem', display: 'inline-block' }}>{isOpen ? '▾' : '▸'}</span>
                        <span style={{ color }}>{icon}</span>
                        <span>{node.name}</span>
                    </button>
                    {isOpen && node.children.map((child) => renderFileNode(child, depth + 1))}
                </div>
            );
        }

        const isSelected = node.path === selectedPath;
        const { icon, color } = fileIcon(node.name);
        return (
            <button
                key={node.path}
                onClick={() => onSelectPath(node.path)}
                onDoubleClick={() => onOpenTab(node.path)}
                onContextMenu={(e) => {
                    e.preventDefault();
                    onSelectPath(node.path);
                    setCtxMenu({ x: e.clientX, y: e.clientY, path: node.path });
                }}
                style={{
                    width: '100%',
                    textAlign: 'left',
                    border: 'none',
                    padding: `0.22rem 0.4rem 0.22rem ${1.4 + depth * 1.1}rem`,
                    fontSize: '0.8rem',
                    cursor: 'pointer',
                    background: isSelected ? '#2a2d3e' : 'transparent',
                    color: isSelected ? '#cdd6f4' : '#bac2de',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.4rem',
                }}
                title="Single-click to select · Double-click to open · Right-click for menu"
            >
                <span style={{ color, fontSize: '0.78rem', flexShrink: 0 }}>{icon}</span>
                <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{fileName(node.path)}</span>
            </button>
        );
    };

    return (
        <aside
            ref={containerRef}
            style={{ width: '100%', height: '100%', background: '#11111b', display: 'flex', flexDirection: 'column', overflow: 'hidden', position: 'relative' }}
            onClick={dismissCtx}
        >
            {/* Header */}
            <div
                style={{
                    fontSize: '0.68rem',
                    fontWeight: 700,
                    letterSpacing: '0.1em',
                    color: '#6c7086',
                    padding: '0.55rem 0.7rem',
                    borderBottom: '1px solid #313244',
                    flexShrink: 0,
                }}
            >
                EXPLORER
            </div>

            {/* New file buttons */}
            <div style={{ display: 'flex', gap: '0.25rem', padding: '0.35rem 0.5rem', borderBottom: '1px solid #313244', background: '#181825', flexShrink: 0 }}>
                <button onClick={() => onCreateFile('text')} style={actionBtnStyle} title="New source file (.cpp)">
                    +Code
                </button>
                <button onClick={() => onCreateFile('image')} style={actionBtnStyle} title="New image file">
                    +Img
                </button>
            </div>

            {/* Workspace label */}
            <div style={{ fontSize: '0.73rem', color: '#89b4fa', padding: '0.4rem 0.7rem', borderBottom: '1px solid #313244', fontWeight: 600, flexShrink: 0 }}>
                workspace
            </div>

            {/* File tree */}
            <div style={{ flex: 1, overflow: 'auto', paddingTop: '0.2rem' }}>{fileTree.map((node) => renderFileNode(node))}</div>

            {/* Rename / Delete buttons */}
            <div style={{ display: 'flex', gap: '0.3rem', padding: '0.4rem 0.5rem', borderTop: '1px solid #313244', background: '#181825', flexShrink: 0 }}>
                <button onClick={onRename} disabled={!selectedPath || !files[selectedPath]} style={actionBtnStyle}>
                    Rename
                </button>
                <button
                    onClick={onDelete}
                    disabled={!selectedPath || !files[selectedPath]}
                    style={{ ...actionBtnStyle, border: '1px solid #7f1d1d', background: '#3b0f19', color: '#f2cdcd' }}
                >
                    Delete
                </button>
            </div>

            {/* Right-click context menu */}
            {ctxMenu && (
                <div
                    style={{
                        position: 'fixed',
                        top: ctxMenu.y,
                        left: ctxMenu.x,
                        zIndex: 9999,
                        background: '#1e1e2e',
                        border: '1px solid #45475a',
                        borderRadius: 6,
                        boxShadow: '0 4px 20px rgba(0,0,0,0.6)',
                        padding: '0.3rem 0',
                        minWidth: 160,
                    }}
                    onMouseDown={(e) => e.stopPropagation()}
                >
                    {[
                        {
                            label: '↗ Open',
                            action: () => {
                                onOpenTab(ctxMenu.path);
                                dismissCtx();
                            },
                        },
                        {
                            label: '↗ Open to the Right',
                            action: () => {
                                onOpenTab(ctxMenu.path, 'right');
                                dismissCtx();
                            },
                        },
                        {
                            label: '✏ Rename',
                            action: () => {
                                dismissCtx();
                                onRename();
                            },
                        },
                        {
                            label: '🗑 Delete',
                            action: () => {
                                dismissCtx();
                                onDelete();
                            },
                            danger: true,
                        },
                    ].map((item) => (
                        <button
                            key={item.label}
                            onClick={item.action}
                            style={{
                                display: 'block',
                                width: '100%',
                                textAlign: 'left',
                                background: 'transparent',
                                border: 'none',
                                color: (item as { danger?: boolean }).danger ? '#f38ba8' : '#cdd6f4',
                                padding: '0.4rem 0.9rem',
                                fontSize: '0.8rem',
                                cursor: 'pointer',
                            }}
                            onMouseEnter={(e) => {
                                (e.currentTarget as HTMLButtonElement).style.background = '#313244';
                            }}
                            onMouseLeave={(e) => {
                                (e.currentTarget as HTMLButtonElement).style.background = 'transparent';
                            }}
                        >
                            {item.label}
                        </button>
                    ))}
                </div>
            )}
            {footer}
        </aside>
    );
}
