'use client';

import { Maximize2, Minimize2 } from 'lucide-react';
import React, { useEffect, useRef, useState } from 'react';
// Import RevealJS styles with scoping to prevent global layout issues
import 'highlight.js/styles/monokai.css';
import 'reveal.js/dist/reveal.css';
import 'reveal.js/dist/theme/white.css';

// Custom styles to fix code block scrolling
const revealCodeStyles = `
  .reveal pre {
    max-height: none !important;
    height: auto !important;
    overflow: visible !important;
  }
  .reveal pre code {
    max-height: none !important;
    height: auto !important;
    overflow: visible !important;
  }
  .reveal pre.code-wrapper {
    max-height: none !important;
    height: auto !important;
    overflow: visible !important;
  }
  .reveal .hljs {
    max-height: none !important;
    overflow: visible !important;
  }
`;

interface RevealJSProps {
  content: string;
  height?: string;
}

// Helper function to get slide number from URL hash
const getSlideFromHash = (): number | null => {
  if (typeof window === 'undefined') return null;
  const hash = window.location.hash.slice(1); // Remove the '#'
  const slideNum = parseInt(hash, 10);
  return !isNaN(slideNum) && slideNum >= 0 ? slideNum : null;
};

// Helper function to update URL hash without triggering navigation
const updateHashWithSlide = (slideIndex: number): void => {
  if (typeof window === 'undefined') return;
  const newUrl = `${window.location.pathname}${window.location.search}#${slideIndex}`;
  window.history.replaceState(null, '', newUrl);
};

// Helper function to render all mermaid diagrams in a container
const renderMermaidDiagrams = async (container: HTMLElement): Promise<void> => {
  try {
    const mermaid = (await import('mermaid')).default;

    // Initialize mermaid with default config
    mermaid.initialize({
      startOnLoad: false,
      theme: 'default',
      securityLevel: 'loose',
      flowchart: {
        useMaxWidth: true,
        htmlLabels: true,
      },
    });

    // Find all code blocks with mermaid language class
    // Markdown ```mermaid blocks get rendered as <code class="language-mermaid"> or <code class="mermaid">
    const mermaidBlocks = container.querySelectorAll(
      'code.language-mermaid, code.mermaid, pre.language-mermaid > code, pre.mermaid > code'
    );

    for (let i = 0; i < mermaidBlocks.length; i++) {
      const block = mermaidBlocks[i];
      if (!block) continue;

      const code = block.textContent ?? '';

      if (!code.trim()) continue;

      try {
        // Generate unique ID for this diagram
        const id = `mermaid-diagram-${Date.now()}-${i}`;

        // Render the mermaid diagram
        const { svg } = await mermaid.render(id, code.trim());

        // Create a container for the rendered SVG
        const svgContainer = document.createElement('div');
        svgContainer.className = 'mermaid-rendered';
        svgContainer.innerHTML = svg;

        // Style the container to center the diagram
        svgContainer.style.display = 'flex';
        svgContainer.style.justifyContent = 'center';
        svgContainer.style.alignItems = 'center';
        svgContainer.style.width = '100%';


        // Replace the code block's parent (pre) or the block itself
        const parent = block.closest('pre') ?? block.parentElement;
        if (parent?.parentElement) {
          parent.parentElement.replaceChild(svgContainer, parent);
        }
      } catch (renderError) {
        console.warn('Failed to render mermaid diagram:', renderError);
      }
    }
  } catch (err) {
    console.warn('Mermaid not available:', err);
  }
};

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
        const mathModule = await import('reveal.js/plugin/math/math.esm.js');

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

        // Add event listener to update URL hash when slide changes
        revealInstance.on('slidechanged', (event: { indexh: number; indexv: number }) => {
          // Use horizontal index (indexh) as the slide number
          // For vertical slides, could use format like "3/1" but keeping simple for now
          updateHashWithSlide(event.indexh);
        });

        // Force layout after initialization to ensure proper sizing
        setTimeout(() => {
          if (revealInstanceRef.current) {
            revealInstanceRef.current.layout();
            // Navigate to slide from URL hash, or first slide if no hash
            const initialSlide = getSlideFromHash();
            revealInstanceRef.current.slide(initialSlide ?? 0);
            // Update hash to reflect current slide (in case hash was invalid)
            const currentSlide = revealInstanceRef.current.getIndices().h;
            updateHashWithSlide(currentSlide);
            // Force sync to make sure markdown is rendered
            revealInstanceRef.current.sync();
          }
        }, 100);

        // Render mermaid diagrams after markdown is processed
        setTimeout(async () => {
          if (containerRef.current) {
            await renderMermaidDiagrams(containerRef.current);
            // Re-layout after mermaid renders
            if (revealInstanceRef.current) {
              revealInstanceRef.current.layout();
            }
          }
        }, 200);

        // Additional sync after a longer delay to handle any late rendering
        setTimeout(() => {
          if (revealInstanceRef.current) {
            revealInstanceRef.current.layout();
          }
        }, 500);

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

    // Handle browser back/forward navigation with hash changes
    const handleHashChange = (): void => {
      const slideNum = getSlideFromHash();
      if (slideNum !== null && revealInstanceRef.current) {
        const currentSlide = revealInstanceRef.current.getIndices().h;
        if (currentSlide !== slideNum) {
          revealInstanceRef.current.slide(slideNum);
        }
      }
    };

    loadRevealJS();

    // Listen for hash changes (browser back/forward)
    window.addEventListener('hashchange', handleHashChange);

    return () => {
      window.removeEventListener('hashchange', handleHashChange);
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

          // Render mermaid diagrams after content sync
          setTimeout(async () => {
            if (containerRef.current) {
              await renderMermaidDiagrams(containerRef.current);
              if (revealInstanceRef.current) {
                revealInstanceRef.current.layout();
              }
            }
          }, 100);
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
    return <div className="reveal-container flex-1">Loading presentation...</div>;
  }

  if (error) {
    return <div className="error-message">Error: {error}</div>;
  }

  return (
    <div
      ref={containerRef}
      className="reveal-container w-full flex-1 flex flex-col overflow-hidden border border-gray-200 dark:border-gray-700 rounded-lg"
    >
      <style dangerouslySetInnerHTML={{ __html: revealCodeStyles }} />
      <div className="reveal w-full flex-1">
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