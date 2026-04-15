'use client';

import { Maximize2, Minimize2 } from 'lucide-react';
import React, { useEffect, useRef, useState } from 'react';
import './remarkjs.css';

interface RemarkJSProps {
    content: string;
    height?: string;
}

const RemarkJS: React.FC<RemarkJSProps> = ({ content, height = '600px' }) => {
    const containerRef = useRef<HTMLDivElement>(null);
    const slideshowRef = useRef<any>(null);
    const [isFullscreen, setIsFullscreen] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [isClient, setIsClient] = useState(false);
    const [isLoaded, setIsLoaded] = useState(false);

    useEffect(() => {
        setIsClient(true);
    }, []);

    useEffect(() => {
        if (!isClient || !containerRef.current) return;

        const loadRemarkJS = async (): Promise<void> => {
            try {
                // Load remark from CDN if not already loaded
                if (!(window as any).remark) {
                    const script = document.createElement('script');
                    script.src = 'https://remarkjs.com/downloads/remark-latest.min.js';
                    script.async = true;

                    await new Promise<void>((resolve, reject) => {
                        script.onload = () => resolve();
                        script.onerror = () => reject(new Error('Failed to load RemarkJS'));
                        document.head.appendChild(script);
                    });
                }

                // Wait for container to have dimensions
                const containerRect = containerRef.current?.getBoundingClientRect();
                if (!containerRect || containerRect.height === 0 || containerRect.width === 0) {
                    requestAnimationFrame(() => {
                        requestAnimationFrame(() => {
                            loadRemarkJS();
                        });
                    });
                    return;
                }

                // Create slideshow - specify container to prevent body takeover
                const remark = (window as any).remark;
                if (remark && containerRef.current) {
                    // Create slideshow with container option
                    slideshowRef.current = remark.create({
                        source: content,
                        ratio: '16:9',
                        highlightStyle: 'monokai',
                        highlightLines: true,
                        highlightSpans: true,
                        countIncrementalSlides: false,
                        slideNumberFormat: '%current% / %total%',
                        container: containerRef.current,
                    });

                    setIsLoaded(true);
                    setError(null);
                }
            } catch (err) {
                console.error('RemarkJS loading error:', err);
                setError(err instanceof Error ? err.message : 'Failed to load RemarkJS presentation');
            }
        };

        loadRemarkJS();

        return () => {
            // Cleanup: destroy slideshow
            try {
                if (slideshowRef.current && typeof slideshowRef.current.destroy === 'function') {
                    slideshowRef.current.destroy();
                }
            } catch (e) {
                console.error('Error cleaning up RemarkJS:', e);
            }
        };
    }, [isClient, content]);

    const toggleFullscreen = (): void => {
        setIsFullscreen(!isFullscreen);

        // Trigger resize after fullscreen toggle
        if (slideshowRef.current) {
            setTimeout(() => {
                window.dispatchEvent(new Event('resize'));
            }, 100);
        }
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
                <p className="font-semibold">RemarkJS Rendering Error:</p>
                <p className="text-sm mt-2">{error}</p>
            </div>
        );
    }

    return (
        <div
            className={`remarkjs-container ${isFullscreen ? 'remarkjs-fullscreen' : ''}`}
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

            {/* RemarkJS container */}
            <div
                ref={containerRef}
                className="remarkjs-slides-container"
                style={{
                    width: '100%',
                    height: '100%',
                }}
            />

            {!isLoaded && (
                <div className="absolute inset-0 flex items-center justify-center">
                    <p>Initializing presentation...</p>
                </div>
            )}
        </div>
    );
};

export default RemarkJS;
