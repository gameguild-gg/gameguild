import { useRef } from 'react';

interface ResizeHandleProps {
    direction: 'horizontal' | 'vertical';
    onResize: (delta: number) => void;
}

export default function ResizeHandle({ direction, onResize }: ResizeHandleProps) {
    const lastPos = useRef(0);

    const handlePointerDown = (e: React.PointerEvent<HTMLDivElement>) => {
        e.preventDefault();
        e.currentTarget.setPointerCapture(e.pointerId);
        lastPos.current = direction === 'horizontal' ? e.clientX : e.clientY;
    };

    const handlePointerMove = (e: React.PointerEvent<HTMLDivElement>) => {
        if (!(e.buttons & 1)) return;
        const pos = direction === 'horizontal' ? e.clientX : e.clientY;
        const delta = pos - lastPos.current;
        lastPos.current = pos;
        if (delta !== 0) onResize(delta);
    };

    const isH = direction === 'horizontal';
    return (
        <div
            onPointerDown={handlePointerDown}
            onPointerMove={handlePointerMove}
            onMouseEnter={(e) => {
                (e.currentTarget as HTMLDivElement).style.background = '#45475a';
            }}
            onMouseLeave={(e) => {
                (e.currentTarget as HTMLDivElement).style.background = '#313244';
            }}
            style={{
                width: isH ? 4 : '100%',
                height: isH ? '100%' : 4,
                background: '#313244',
                cursor: isH ? 'col-resize' : 'row-resize',
                flexShrink: 0,
                zIndex: 10,
            }}
        />
    );
}
