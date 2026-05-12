import { useCallback, useState } from 'react';
import type { DockGroup } from './ide-types';

/** VS Code–style drop zone positions. */
export type DropZone = 'center' | 'left' | 'right' | 'top' | 'bottom';

/** Maps a drop zone to the target dock group. Center keeps the tab in `currentGroup`. */
export function dropZoneToGroup(zone: DropZone, currentGroup: DockGroup): DockGroup {
    switch (zone) {
        case 'left':
            return 'main';
        case 'right':
            return 'right';
        case 'top':
            return 'main';
        case 'bottom':
            return 'bottom';
        case 'center':
        default:
            return currentGroup;
    }
}

const ZONE_COLOR = 'rgba(137, 180, 250, 0.18)';
const ZONE_ACTIVE = 'rgba(137, 180, 250, 0.35)';
const ZONE_BORDER = 'rgba(137, 180, 250, 0.6)';

interface DockDropOverlayProps {
    visible: boolean;
    currentGroup: DockGroup;
    /** Called when a tab is dropped on a zone. Receives the zone and the raw drag event. */
    onDrop: (zone: DropZone, e: React.DragEvent) => void;
}

/**
 * VS Code–style drop zone overlay. Shows 5 drop targets (center + 4 edges)
 * when a tab is being dragged over a panel. Each zone highlights on hover.
 */
export default function DockDropOverlay({ visible, currentGroup, onDrop }: DockDropOverlayProps) {
    const [activeZone, setActiveZone] = useState<DropZone | null>(null);

    const handleDragOver = useCallback((e: React.DragEvent, zone: DropZone) => {
        e.preventDefault();
        e.stopPropagation();
        e.dataTransfer.dropEffect = 'move';
        setActiveZone(zone);
    }, []);

    const handleDragLeave = useCallback((e: React.DragEvent) => {
        // Only clear if leaving the zone element itself (not entering a child)
        const related = e.relatedTarget as Node | null;
        if (related && (e.currentTarget as Node).contains(related)) return;
        setActiveZone(null);
    }, []);

    const handleDrop = useCallback(
        (e: React.DragEvent, zone: DropZone) => {
            e.preventDefault();
            e.stopPropagation();
            setActiveZone(null);
            onDrop(zone, e);
        },
        [onDrop],
    );

    if (!visible) return null;

    const zoneStyle = (zone: DropZone): React.CSSProperties => ({
        position: 'absolute',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        transition: 'background 0.12s, border-color 0.12s',
        background: activeZone === zone ? ZONE_ACTIVE : ZONE_COLOR,
        border: activeZone === zone ? `2px solid ${ZONE_BORDER}` : '2px solid transparent',
        borderRadius: 4,
        zIndex: 10,
        pointerEvents: 'auto',
    });

    const iconStyle: React.CSSProperties = {
        fontSize: '1.1rem',
        color: '#89b4fa',
        opacity: 0.9,
        pointerEvents: 'none',
        userSelect: 'none',
    };

    // Edge insets: the edge zones take up 25% of the panel dimension.
    // Center takes the remaining inner area.
    const E = '25%';

    return (
        <div
            style={{
                position: 'absolute',
                inset: 0,
                zIndex: 100,
                pointerEvents: 'none',
            }}
            onDragLeave={() => setActiveZone(null)}
        >
            {/* Center */}
            <div
                style={{ ...zoneStyle('center'), top: E, left: E, right: E, bottom: E }}
                onDragOver={(e) => handleDragOver(e, 'center')}
                onDragLeave={handleDragLeave}
                onDrop={(e) => handleDrop(e, 'center')}
            >
                <span style={iconStyle}>⬚</span>
            </div>

            {/* Left */}
            <div
                style={{ ...zoneStyle('left'), top: 0, left: 0, bottom: 0, width: E }}
                onDragOver={(e) => handleDragOver(e, 'left')}
                onDragLeave={handleDragLeave}
                onDrop={(e) => handleDrop(e, 'left')}
            >
                <span style={iconStyle}>⬅</span>
            </div>

            {/* Right */}
            <div
                style={{ ...zoneStyle('right'), top: 0, right: 0, bottom: 0, width: E }}
                onDragOver={(e) => handleDragOver(e, 'right')}
                onDragLeave={handleDragLeave}
                onDrop={(e) => handleDrop(e, 'right')}
            >
                <span style={iconStyle}>➡</span>
            </div>

            {/* Top */}
            <div
                style={{ ...zoneStyle('top'), top: 0, left: E, right: E, height: E }}
                onDragOver={(e) => handleDragOver(e, 'top')}
                onDragLeave={handleDragLeave}
                onDrop={(e) => handleDrop(e, 'top')}
            >
                <span style={iconStyle}>⬆</span>
            </div>

            {/* Bottom */}
            <div
                style={{ ...zoneStyle('bottom'), bottom: 0, left: E, right: E, height: E }}
                onDragOver={(e) => handleDragOver(e, 'bottom')}
                onDragLeave={handleDragLeave}
                onDrop={(e) => handleDrop(e, 'bottom')}
            >
                <span style={iconStyle}>⬇</span>
            </div>
        </div>
    );
}
