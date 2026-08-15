import Editor, { type OnMount } from '@monaco-editor/react';
import { useCallback, useEffect, useRef, useState } from 'react';
import DockDropOverlay, { dropZoneToGroup, type DropZone } from './DockDropOverlay.js';
import { tabIcon } from './FileExplorer.js';
import { type DockGroup, type OpenTab, type WorkspaceFile } from './ide-types.js';
import { fileName, inferLanguage } from './ide-utils.js';

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
    /** Called when this panel mounts/unmounts a canvas host container. Ide reparents the real canvas into it. */
    onCanvasHost: (el: HTMLDivElement | null) => void;
    onSetActiveTab: (id: string) => void;
    onCloseTab: (id: string) => void;
    onMoveTab: (tabId: string, group: DockGroup) => void;
    /** Reorder a tab within the global openTabs array (drag within same group). */
    onReorderTab: (tabId: string, beforeTabId: string) => void;
    onEditorMount: OnMount;
    onEditorChange: (path: string, value: string) => void;
    canvasIsRunning: boolean;
}

export default function DockGroupPanel({
    group,
    tabs,
    activeTabId,
    files,
    onCanvasHost,
    onSetActiveTab,
    onCloseTab,
    onMoveTab,
    onReorderTab,
    onEditorMount,
    onEditorChange,
    canvasIsRunning,
}: DockGroupPanelProps) {
    const [draggingTabId, setDraggingTabId] = useState<string | null>(null);

    if (tabs.length === 0) return null;

    const localActive = tabs.find((t) => t.id === activeTabId) ?? tabs[0];
    const localFile = localActive.type !== 'canvas' ? files[localActive.path] : undefined;

    return (
        <DockGroupPanelInner
            group={group}
            tabs={tabs}
            localActive={localActive}
            localFile={localFile}
            files={files}
            onCanvasHost={onCanvasHost}
            draggingTabId={draggingTabId}
            setDraggingTabId={setDraggingTabId}
            onSetActiveTab={onSetActiveTab}
            onCloseTab={onCloseTab}
            onMoveTab={onMoveTab}
            onReorderTab={onReorderTab}
            onEditorMount={onEditorMount}
            onEditorChange={onEditorChange}
            canvasIsRunning={canvasIsRunning}
        />
    );
}

// Inner component so we can use hooks even with the early return above
function DockGroupPanelInner({
    group,
    tabs,
    localActive,
    localFile,
    onCanvasHost,
    draggingTabId,
    setDraggingTabId,
    onSetActiveTab,
    onCloseTab,
    onMoveTab,
    onReorderTab,
    onEditorMount,
    onEditorChange,
    canvasIsRunning,
}: {
    group: DockGroup;
    tabs: OpenTab[];
    localActive: OpenTab;
    localFile: WorkspaceFile | undefined;
    files: Record<string, WorkspaceFile>;
    onCanvasHost: (el: HTMLDivElement | null) => void;
    draggingTabId: string | null;
    setDraggingTabId: (id: string | null) => void;
    onSetActiveTab: (id: string) => void;
    onCloseTab: (id: string) => void;
    onMoveTab: (tabId: string, group: DockGroup) => void;
    onReorderTab: (tabId: string, beforeTabId: string) => void;
    onEditorMount: OnMount;
    onEditorChange: (path: string, value: string) => void;
    canvasIsRunning: boolean;
}) {
    const animFrameRef = useRef(0);
    const [isDragOver, setIsDragOver] = useState(false);
    const [dropIndicatorTabId, setDropIndicatorTabId] = useState<string | null>(null);
    const dragCounterRef = useRef(0);
    // Cancel any lingering RAF when SDL takes over the canvas
    useEffect(() => {
        if (!canvasIsRunning) return;
        window.cancelAnimationFrame(animFrameRef.current);
    }, [canvasIsRunning]);

    const handleOverlayDrop = useCallback(
        (zone: DropZone, e: React.DragEvent) => {
            // Read tab id from dataTransfer (works for cross-panel drags),
            // fall back to local draggingTabId for same-panel drags.
            const tabId = e.dataTransfer.getData('text/tab-id') || draggingTabId || lastDataTransferTabRef.current;
            if (!tabId) return;
            const targetGroup = dropZoneToGroup(zone, group);
            onMoveTab(tabId, targetGroup);
            setDraggingTabId(null);
            setIsDragOver(false);
            dragCounterRef.current = 0;
            lastDataTransferTabRef.current = null;
        },
        [draggingTabId, group, onMoveTab, setDraggingTabId],
    );

    /** Stash the tab id from dataTransfer for cross-panel drops (dragOver fires
     *  before drop, so we can read it there for Firefox; Chrome restricts reads
     *  to the drop event only, but we fall back to the stashed value). */
    const lastDataTransferTabRef = useRef<string | null>(null);

    return (
        <div
            style={{
                display: 'flex',
                flexDirection: 'column',
                height: '100%',
                border: '1px solid #313244',
                background: '#1e1e2e',
                overflow: 'hidden',
                position: 'relative',
            }}
            onDragOver={(e) => e.preventDefault()}
            onDragEnter={(e) => {
                e.preventDefault();
                dragCounterRef.current++;
                // Show overlay for local drags (draggingTabId) or cross-panel
                // drags (dataTransfer contains 'text/tab-id' type).
                const hasTabType = e.dataTransfer.types.includes('text/tab-id');
                if (draggingTabId || hasTabType) setIsDragOver(true);
            }}
            onDragLeave={() => {
                dragCounterRef.current--;
                if (dragCounterRef.current <= 0) {
                    setIsDragOver(false);
                    dragCounterRef.current = 0;
                }
            }}
            onDrop={(e) => {
                e.preventDefault();
                // Fallback: if dropped outside a specific zone, treat as center
                const tabId = e.dataTransfer.getData('text/tab-id') || draggingTabId;
                if (tabId) onMoveTab(tabId, group);
                setDraggingTabId(null);
                setIsDragOver(false);
                dragCounterRef.current = 0;
                lastDataTransferTabRef.current = null;
            }}
        >
            {/* VS Code–style drop zone overlay */}
            <DockDropOverlay visible={isDragOver} currentGroup={group} onDrop={handleOverlayDrop} />
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
                        const { icon, color } = tab.type === 'canvas' ? tabIcon('canvas', '') : tabIcon(tab.type, fileName(tab.path));
                        return (
                            <div
                                key={tab.id}
                                draggable
                                onDragStart={(e) => {
                                    setDraggingTabId(tab.id);
                                    e.dataTransfer.setData('text/tab-id', tab.id);
                                }}
                                onDragEnd={() => {
                                    setDraggingTabId(null);
                                    setDropIndicatorTabId(null);
                                    setIsDragOver(false);
                                    dragCounterRef.current = 0;
                                }}
                                onDragOver={(e) => {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    const srcId = e.dataTransfer.types.includes('text/tab-id') ? (draggingTabId ?? e.dataTransfer.getData('text/tab-id')) : null;
                                    if (srcId && srcId !== tab.id) setDropIndicatorTabId(tab.id);
                                }}
                                onDragLeave={() => {
                                    setDropIndicatorTabId((prev) => (prev === tab.id ? null : prev));
                                }}
                                onDrop={(e) => {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    const srcId = e.dataTransfer.getData('text/tab-id') || draggingTabId;
                                    if (srcId && srcId !== tab.id) {
                                        onReorderTab(srcId, tab.id);
                                    }
                                    setDropIndicatorTabId(null);
                                    setDraggingTabId(null);
                                    setIsDragOver(false);
                                    dragCounterRef.current = 0;
                                }}
                                style={{
                                    display: 'flex',
                                    alignItems: 'center',
                                    gap: '0.35rem',
                                    padding: '0.3rem 0.55rem',
                                    borderRight: '1px solid #313244',
                                    borderBottom: isActive ? '2px solid #89b4fa' : '2px solid transparent',
                                    borderLeft: dropIndicatorTabId === tab.id ? '2px solid #89b4fa' : '2px solid transparent',
                                    color: isActive ? '#cdd6f4' : '#9399b2',
                                    background: isActive ? '#1e1e2e' : '#181825',
                                    fontSize: '0.75rem',
                                    minWidth: 90,
                                    cursor: 'grab',
                                    userSelect: 'none',
                                    opacity: draggingTabId === tab.id ? 0.5 : 1,
                                    transition: 'opacity 0.15s, border-left-color 0.1s',
                                }}
                                onClick={() => onSetActiveTab(tab.id)}
                                title={tab.type === 'canvas' ? 'Canvas' : tab.path}
                            >
                                <span style={{ color, fontSize: '0.75rem', flexShrink: 0 }}>{icon}</span>
                                <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 110 }}>
                                    {tab.type === 'canvas' ? 'Canvas' : fileName(tab.path)}
                                </span>
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
                                    aria-label={`Close ${tab.type === 'canvas' ? 'Canvas' : tab.path}`}
                                >
                                    ×
                                </button>
                            </div>
                        );
                    })}
                </div>
                {/* Split/dock buttons — VS Code style */}
                <div style={{ display: 'flex', gap: '0.15rem', padding: '0.25rem 0.4rem', flexShrink: 0, alignItems: 'center' }}>
                    {[
                        { target: 'main' as DockGroup, icon: '⬅', title: 'Move to main editor' },
                        { target: 'right' as DockGroup, icon: '⬌', title: 'Split right' },
                        { target: 'bottom' as DockGroup, icon: '⬍', title: 'Split down' },
                    ].map(({ target, icon, title }) => (
                        <button
                            key={target}
                            onClick={() => onMoveTab(localActive.id, target)}
                            style={{
                                border: 'none',
                                background: target === group ? '#313244' : 'transparent',
                                color: target === group ? '#cdd6f4' : '#6c7086',
                                borderRadius: 3,
                                fontSize: '0.8rem',
                                padding: '0.08rem 0.25rem',
                                cursor: 'pointer',
                                lineHeight: 1,
                                transition: 'color 0.12s, background 0.12s',
                            }}
                            title={title}
                        >
                            {icon}
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
                {localActive.type === 'canvas' && (
                    <div
                        ref={onCanvasHost}
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
                        {/* Ide.tsx reparents the persistent <canvas> into this host div */}
                        {!canvasIsRunning && (
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
                                <span style={{ fontSize: '1rem', color: '#a6adc8' }}>Render Canvas</span>
                                <span style={{ fontSize: '0.8rem' }}>
                                    Click <strong style={{ color: '#a6e3a1' }}>&#9654;</strong> to build and render the demo
                                </span>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
}
