import Editor, { type OnMount } from '@monaco-editor/react';
import { useEffect, useRef, useState } from 'react';
import { tabIcon } from './FileExplorer';
import type { DockGroup, OpenTab, WorkspaceFile } from './ide-types';
import { fileName, inferLanguage } from './ide-utils';

const DOCK_LABELS: Record<DockGroup, string> = {
    main: 'EDITOR',
    right: 'DOCK: RIGHT',
    bottom: 'DOCK: BOTTOM',
};

interface DockGroupPanelProps {
    group: DockGroup;
    tabs: OpenTab[];
    /** The globally active tab id. This group shows the matching tab or falls back to the first one. */
    activeTabId: string;
    files: Record<string, WorkspaceFile>;
    canvasRef: React.RefObject<HTMLCanvasElement | null>;
    onSetActiveTab: (id: string) => void;
    onCloseTab: (id: string) => void;
    onMoveTab: (tabId: string, group: DockGroup) => void;
    onEditorMount: OnMount;
    onEditorChange: (path: string, value: string) => void;
}

export default function DockGroupPanel({
    group,
    tabs,
    activeTabId,
    files,
    canvasRef,
    onSetActiveTab,
    onCloseTab,
    onMoveTab,
    onEditorMount,
    onEditorChange,
}: DockGroupPanelProps) {
    const [draggingTabId, setDraggingTabId] = useState<string | null>(null);

    if (tabs.length === 0) return null;

    const localActive = tabs.find((t) => t.id === activeTabId) ?? tabs[0];
    const localFile = files[localActive.path];

    return (
        <DockGroupPanelInner
            group={group}
            tabs={tabs}
            localActive={localActive}
            localFile={localFile}
            files={files}
            canvasRef={canvasRef}
            draggingTabId={draggingTabId}
            setDraggingTabId={setDraggingTabId}
            onSetActiveTab={onSetActiveTab}
            onCloseTab={onCloseTab}
            onMoveTab={onMoveTab}
            onEditorMount={onEditorMount}
            onEditorChange={onEditorChange}
        />
    );
}

// Inner component so we can use hooks even with the early return above
function DockGroupPanelInner({
    group,
    tabs,
    localActive,
    localFile,
    canvasRef,
    draggingTabId,
    setDraggingTabId,
    onSetActiveTab,
    onCloseTab,
    onMoveTab,
    onEditorMount,
    onEditorChange,
}: {
    group: DockGroup;
    tabs: OpenTab[];
    localActive: OpenTab;
    localFile: WorkspaceFile | undefined;
    files: Record<string, WorkspaceFile>;
    canvasRef: React.RefObject<HTMLCanvasElement | null>;
    draggingTabId: string | null;
    setDraggingTabId: (id: string | null) => void;
    onSetActiveTab: (id: string) => void;
    onCloseTab: (id: string) => void;
    onMoveTab: (tabId: string, group: DockGroup) => void;
    onEditorMount: OnMount;
    onEditorChange: (path: string, value: string) => void;
}) {
    const animFrameRef = useRef(0);
    // Cancel any lingering RAF when SDL takes over (content becomes truthy)
    useEffect(() => {
        if (localFile?.type !== 'canvas' || !localFile.content) return;
        window.cancelAnimationFrame(animFrameRef.current);
    }, [localFile?.type, localFile?.content]);

    return (
        <div
            style={{
                display: 'flex',
                flexDirection: 'column',
                height: '100%',
                border: '1px solid #313244',
                background: '#1e1e2e',
                overflow: 'hidden',
            }}
            onDragOver={(e) => e.preventDefault()}
            onDrop={(e) => {
                e.preventDefault();
                const tabId = e.dataTransfer.getData('text/tab-id') || draggingTabId;
                if (tabId) onMoveTab(tabId, group);
                setDraggingTabId(null);
            }}
        >
            {/* Tab strip */}
            <div
                style={{
                    display: 'flex',
                    alignItems: 'center',
                    background: '#181825',
                    borderBottom: '1px solid #313244',
                }}
            >
                <div
                    style={{
                        fontSize: '0.7rem',
                        color: '#6c7086',
                        padding: '0.35rem 0.5rem',
                        borderRight: '1px solid #313244',
                        whiteSpace: 'nowrap',
                    }}
                >
                    {DOCK_LABELS[group]}
                </div>
                <div style={{ display: 'flex', overflowX: 'auto', flex: 1 }}>
                    {tabs.map((tab) => {
                        const isActive = tab.id === localActive.id;
                        const { icon, color } = tabIcon(tab.type, fileName(tab.path));
                        return (
                            <div
                                key={tab.id}
                                draggable
                                onDragStart={(e) => {
                                    setDraggingTabId(tab.id);
                                    e.dataTransfer.setData('text/tab-id', tab.id);
                                }}
                                onDragEnd={() => setDraggingTabId(null)}
                                style={{
                                    display: 'flex',
                                    alignItems: 'center',
                                    gap: '0.35rem',
                                    padding: '0.3rem 0.55rem',
                                    borderRight: '1px solid #313244',
                                    borderBottom: isActive ? '2px solid #89b4fa' : '2px solid transparent',
                                    color: isActive ? '#cdd6f4' : '#9399b2',
                                    background: isActive ? '#1e1e2e' : '#181825',
                                    fontSize: '0.75rem',
                                    minWidth: 90,
                                    cursor: 'pointer',
                                    userSelect: 'none',
                                }}
                                onClick={() => onSetActiveTab(tab.id)}
                                title={tab.path}
                            >
                                <span style={{ color, fontSize: '0.75rem', flexShrink: 0 }}>{icon}</span>
                                <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 110 }}>{fileName(tab.path)}</span>
                                <button
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        onCloseTab(tab.id);
                                    }}
                                    style={{
                                        border: 'none',
                                        background: 'transparent',
                                        color: '#6c7086',
                                        cursor: 'pointer',
                                        padding: 0,
                                        fontSize: '0.75rem',
                                        flexShrink: 0,
                                        lineHeight: 1,
                                    }}
                                    aria-label={`Close ${tab.path}`}
                                >
                                    ×
                                </button>
                            </div>
                        );
                    })}
                </div>
                {/* Dock target buttons */}
                <div style={{ display: 'flex', gap: '0.3rem', padding: '0.25rem 0.4rem', flexShrink: 0 }}>
                    {(['main', 'right', 'bottom'] as DockGroup[]).map((target) => (
                        <button
                            key={target}
                            onClick={() => onMoveTab(localActive.id, target)}
                            style={{
                                border: '1px solid #45475a',
                                background: target === group ? '#313244' : '#1e1e2e',
                                color: '#a6adc8',
                                borderRadius: 4,
                                fontSize: '0.66rem',
                                padding: '0.1rem 0.3rem',
                                cursor: 'pointer',
                            }}
                            title={`Dock to ${target}`}
                        >
                            {target[0].toUpperCase()}
                        </button>
                    ))}
                </div>
            </div>

            {/* Content area */}
            <div style={{ flex: 1, minHeight: 80, overflow: 'hidden', position: 'relative' }}>
                {localFile?.type === 'text' && (
                    <div data-testid="editor-pane" style={{ height: '100%' }}>
                        <Editor
                            height="100%"
                            path={localFile.path}
                            language={inferLanguage(localFile.path)}
                            value={localFile.content}
                            theme="vs-dark"
                            onMount={onEditorMount}
                            onChange={(value) => onEditorChange(localFile.path, value ?? '')}
                            options={{
                                minimap: { enabled: false },
                                fontSize: 14,
                                fontFamily: '"Fira Code", monospace',
                                scrollBeyondLastLine: false,
                                automaticLayout: true,
                            }}
                        />
                    </div>
                )}
                {localFile?.type === 'image' && (
                    <div
                        style={{
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            height: '100%',
                            background: '#11111b',
                        }}
                    >
                        <img src={localFile.content} alt={fileName(localFile.path)} style={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }} />
                    </div>
                )}
                {localFile?.type === 'canvas' && (
                    <div
                        style={{
                            height: '100%',
                            width: '100%',
                            background: '#11111b',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            position: 'relative',
                        }}
                    >
                        {/* SDL renders into this canvas — id="canvas" is required by SDL3's */}
                        {/* Emscripten backend default selector (SDL_HINT_EMSCRIPTEN_CANVAS_SELECTOR). */}
                        <canvas
                            id="canvas"
                            data-testid="sdl-canvas"
                            tabIndex={0}
                            ref={canvasRef}
                            style={{ width: '100%', height: '100%', display: localFile.content ? 'block' : 'none' }}
                        />
                        {!localFile.content && (
                            <div
                                style={{
                                    position: 'absolute',
                                    inset: 0,
                                    display: 'flex',
                                    flexDirection: 'column',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    color: '#6c7086',
                                    fontFamily: 'Menlo, Monaco, "Courier New", monospace',
                                    gap: '0.6rem',
                                    pointerEvents: 'none',
                                    userSelect: 'none',
                                }}
                            >
                                <span style={{ fontSize: '2.5rem' }}>🎮</span>
                                <span style={{ fontSize: '1rem', color: '#a6adc8' }}>SDL Canvas</span>
                                <span style={{ fontSize: '0.8rem' }}>Click <strong style={{ color: '#a6e3a1' }}>&#9654;</strong> to build and render the SDL3 demo</span>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
}
