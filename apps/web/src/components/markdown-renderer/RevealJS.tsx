'use client';

import { Maximize2, Minimize2 } from 'lucide-react';
import React, { useEffect, useRef, useState } from 'react';
// Import RevealJS styles with scoping to prevent global layout issues
import 'highlight.js/styles/monokai.css';
import 'reveal.js/dist/reveal.css';
import 'reveal.js/dist/theme/white.css';

interface RevealJSProps {
  content: string;
  height?: string;
}

const RevealJS: React.FC<RevealJSProps> = ({ content, height = '600px' }) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const slidesRef = useRef<HTMLDivElement>(null);
  const revealInstanceRef = useRef<any>(null);
  const resizeObserverRef = useRef<ResizeObserver | null>(null);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isClient, setIsClient] = useState(false);

  useEffect(() => {
    setIsClient(true);
  }, []);

  useEffect(() => {
    if (!isClient) return;

    let Reveal: any;
    let Markdown: any;
    let Highlight: any;
    let RevealMath: any;

    const loadRevealJS = async () => {
      try {
        // Wait for the container to have actual dimensions
        if (!containerRef.current || !slidesRef.current) return;

        // Check if container has dimensions, if not wait for next frame
        const containerRect = containerRef.current.getBoundingClientRect();
        if (containerRect.height === 0 || containerRect.width === 0) {
          // Use requestAnimationFrame to wait for layout
          requestAnimationFrame(() => {
            requestAnimationFrame(() => {
              // Double RAF to ensure layout is complete
              loadRevealJS();
            });
          });
          return;
        }

        // Importação dinâmica
        const revealModule = await import('reveal.js');
        const markdownModule = await import(
          'reveal.js/plugin/markdown/markdown.esm.js'
        );
        const highlightModule = await import(
          'reveal.js/plugin/highlight/highlight.esm.js'
        );
        const mathModule = await import(
          'reveal.js/plugin/math/math.esm.js'
        );

        Reveal = revealModule.default;
        Markdown = markdownModule.default;
        Highlight = highlightModule.default;
        RevealMath = mathModule.default;

        if (!containerRef.current || !slidesRef.current) return;

        slidesRef.current.innerHTML = `<section data-markdown><textarea data-template>${content}</textarea></section>`;

        // Get container dimensions for Reveal.js
        const containerWidth = containerRef.current.offsetWidth;
        const containerHeight = containerRef.current.offsetHeight;

        // Initialize Reveal.js with markdown and auto-resize support
        const revealInstance = new Reveal(containerRef.current, {
          plugins: [Markdown, Highlight, RevealMath.MathJax3],
          width: containerWidth || 960,
          height: containerHeight || 700,
          margin: 0.04,
          embedded: true,
          hash: false,
          mouseWheel: true,
          transition: 'none',
          controls: true,
          progress: true,
          controlsTutorial: true,
          controlsLayout: 'bottom-right',
          center: true,
          touch: true,
          minScale: 0.1,
          maxScale: 2.0,
          slideNumber: 'c/t', // Show current/total slide count
          highlight: {
            highlightOnLoad: true,
            escapeHTML: false,
          },
          markdown: {
            animateLists: false,
            smartypants: true,
          },
        });

        await revealInstance.initialize();
        revealInstanceRef.current = revealInstance;

        // Force layout after initialization to ensure proper sizing
        setTimeout(() => {
          if (revealInstanceRef.current) {
            revealInstanceRef.current.layout();
            // Ensure we're on the first slide
            revealInstanceRef.current.slide(0);
            // Force sync to make sure markdown is rendered
            revealInstanceRef.current.sync();
          }
        }, 100);

        // Additional sync after a longer delay to handle any late rendering
        setTimeout(() => {
          if (revealInstanceRef.current) {
            revealInstanceRef.current.layout();
          }
        }, 300);

        // Setup ResizeObserver to auto-resize presentation when container changes
        if (containerRef.current) {
          if (resizeObserverRef.current) {
            resizeObserverRef.current.disconnect();
          }

          resizeObserverRef.current = new ResizeObserver((entries) => {
            if (revealInstanceRef.current && entries[0]) {
              const { width, height } = entries[0].contentRect;
              revealInstanceRef.current.configure({
                width: width || 960,
                height: height || 700,
              });
              revealInstanceRef.current.layout();
            }
          });

          resizeObserverRef.current.observe(containerRef.current);
        }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : String(err);
        setError(`Error initializing Reveal.js: ${errorMessage}`);
      }
    };

    loadRevealJS();

    return () => {
      if (revealInstanceRef.current) {
        revealInstanceRef.current.destroy();
        revealInstanceRef.current = null;
      }
      if (resizeObserverRef.current) {
        resizeObserverRef.current.disconnect();
        resizeObserverRef.current = null;
      }
    };
  }, [isClient]);

  useEffect(() => {
    const updateContent = async () => {
      if (revealInstanceRef.current && slidesRef.current) {
        try {
          slidesRef.current.innerHTML = `<section data-markdown><textarea data-template>${content}</textarea></section>`;
          await revealInstanceRef.current.sync();
        } catch (err) {
          const errorMessage = err instanceof Error ? err.message : String(err);
          setError(`Error syncing Reveal.js: ${errorMessage}`);
        }
      }
    };

    updateContent();
  }, [content]);

  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      containerRef.current?.requestFullscreen();
      setIsFullscreen(true);
      if (revealInstanceRef.current) {
        revealInstanceRef.current.configure({ embedded: false });
        // Force layout recalculation in fullscreen
        setTimeout(() => {
          if (revealInstanceRef.current) {
            revealInstanceRef.current.layout();
          }
        }, 100);
      }
    } else {
      document.exitFullscreen();
      setIsFullscreen(false);
      if (revealInstanceRef.current) {
        revealInstanceRef.current.configure({ embedded: true });
        // Force layout recalculation when exiting fullscreen
        setTimeout(() => {
          if (revealInstanceRef.current) {
            revealInstanceRef.current.layout();
          }
        }, 100);
      }
    }
  };

  if (!isClient) {
    return <div className="reveal-container">Loading presentation...</div>;
  }

  if (error) {
    return <div className="error-message">Error: {error}</div>;
  }

  return (
    <div
      ref={containerRef}
      className="reveal-container w-full min-h-[50vh] sm:min-h-[60vh] md:min-h-[70vh] lg:min-h-[80vh] overflow-hidden border border-gray-200 dark:border-gray-700 rounded-lg"
    >
      <div className="reveal w-full min-h-[50vh] sm:min-h-[60vh] md:min-h-[70vh] lg:min-h-[80vh]">
        <div className="slides" ref={slidesRef}></div>
      </div>
      <button
        onClick={toggleFullscreen}
        className="absolute top-4 right-4 bg-blue-500 hover:bg-blue-600 text-white p-2 rounded-full shadow-lg transition-colors duration-200 z-10"
        aria-label={isFullscreen ? 'Exit fullscreen' : 'Enter fullscreen'}
      >
        {isFullscreen ? <Minimize2 size={24} /> : <Maximize2 size={24} />}
      </button>
    </div>
  );
};

export default RevealJS; 