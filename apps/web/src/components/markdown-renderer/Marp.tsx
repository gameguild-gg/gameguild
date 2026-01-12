'use client';

import { Marp as MarpCore } from '@marp-team/marp-core';
import { Maximize2, Minimize2 } from 'lucide-react';
import React, { useEffect, useRef, useState } from 'react';
import './marp.css';

interface MarpProps {
    content: string;
    height?: string;
}

const Marp: React.FC<MarpProps> = ({ content, height = '600px' }) => {
    const containerRef = useRef<HTMLDivElement>(null);
    const [isFullscreen, setIsFullscreen] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [isClient, setIsClient] = useState(false);
    const [htmlContent, setHtmlContent] = useState<string>('');

    useEffect(() => {
        setIsClient(true);
    }, []);

    useEffect(() => {
        if (!isClient) return;

        const renderMarp = async () => {
            try {
                // Create a new Marp instance
                const marp = new MarpCore({
                    html: true,
                });

                // Render the markdown to HTML
                const { html, css } = marp.render(content);

                // Combine HTML and CSS
                const fullHtml = `
          <style>${css}</style>
          <div class="marp">
            ${html}
          </div>
        `;

                setHtmlContent(fullHtml);
                setError(null);
            } catch (err) {
                console.error('Marp rendering error:', err);
                setError(err instanceof Error ? err.message : 'Failed to render Marp presentation');
            }
        };

        renderMarp();
    }, [isClient, content]);

    const toggleFullscreen = () => {
        setIsFullscreen(!isFullscreen);
    };

    if (!isClient) {
        return (
            <div className="flex items-center justify-center" style={{ height }}>
                <p>Loading presentation...</p>
            </div>
        );
    }

    if (error) {
        return (
            <div className="text-red-500 p-4 border border-red-300 rounded bg-red-50 dark:bg-red-950">
                <p className="font-semibold">Marp Rendering Error:</p>
                <p className="text-sm mt-2">{error}</p>
            </div>
        );
    }

    return (
        <div
            ref={containerRef}
            className={`marp-container ${isFullscreen ? 'marp-fullscreen' : ''}`}
            style={
                !isFullscreen
                    ? {
                        height,
                        minHeight: '600px',
                        position: 'relative',
                        overflow: 'hidden',
                    }
                    : {
                        position: 'fixed',
                        top: 0,
                        left: 0,
                        width: '100vw',
                        height: '100vh',
                        zIndex: 9999,
                        backgroundColor: 'var(--background)',
                    }
            }
        >
            {/* Fullscreen toggle button */}
            <button
                onClick={toggleFullscreen}
                className="absolute top-4 right-4 z-50 p-2 rounded-full bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors"
                aria-label={isFullscreen ? 'Exit fullscreen' : 'Enter fullscreen'}
            >
                {isFullscreen ? <Minimize2 size={20} /> : <Maximize2 size={20} />}
            </button>

            {/* Marp content */}
            <div
                className="marp-slides-container"
                dangerouslySetInnerHTML={{ __html: htmlContent }}
                style={{
                    width: '100%',
                    height: '100%',
                }}
            />
        </div>
    );
};

export default Marp;
