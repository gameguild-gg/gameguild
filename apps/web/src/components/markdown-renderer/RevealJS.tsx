'use client';

import { Maximize2, Minimize2 } from 'lucide-react';
import { useTheme } from 'next-themes';
import React, { useEffect, useRef, useState } from 'react';
// Import RevealJS base styles
import 'highlight.js/styles/monokai.css';
import 'reveal.js/dist/reveal.css';

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

// Dark theme overrides applied on top of Reveal's own theme
const darkModeOverrides = `
  .reveal-container.theme-dark .reveal {
    color: #e4e4e7;
  }
  .reveal-container.theme-dark .reveal h1,
  .reveal-container.theme-dark .reveal h2,
  .reveal-container.theme-dark .reveal h3,
  .reveal-container.theme-dark .reveal h4,
  .reveal-container.theme-dark .reveal h5,
  .reveal-container.theme-dark .reveal h6 {
    color: #fafafa;
  }
  .reveal-container.theme-dark .reveal {
    background: #18181b;
  }
  .reveal-container.theme-dark .reveal .slide-background {
    background: #18181b;
  }
  .reveal-container.theme-dark .reveal a {
    color: #60a5fa;
  }
  .reveal-container.theme-dark .reveal strong {
    color: #fafafa;
  }
  .reveal-container.theme-dark .reveal .controls button {
    color: #a1a1aa;
  }
  .reveal-container.theme-dark .reveal .progress span {
    background: #60a5fa;
  }
  .reveal-container.theme-dark .reveal .slide-number {
    color: #a1a1aa;
  }
  .reveal-container.theme-dark .reveal table th,
  .reveal-container.theme-dark .reveal table td {
    border-color: #3f3f46;
  }
  .reveal-container.theme-dark .reveal table th {
    color: #fafafa;
  }
  .reveal-container.theme-dark .reveal blockquote {
    background: rgba(255,255,255,0.05);
    border-left-color: #60a5fa;
    color: #d4d4d8;
  }
`;

const lightModeOverrides = `
  .reveal-container.theme-light .reveal {
    background: #ffffff;
  }
  .reveal-container.theme-light .reveal .slide-background {
    background: #ffffff;
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

// ── Mermaid pipeline ──
// Two-phase design:
//   Phase 1 — extractMermaidSources: runs once per slide right after Reveal
//     parses markdown. Replaces every <pre><code class="language-mermaid">
//     block with a host <div> that stores the original source text in
//     data-mermaid-source. After this, the original code blocks are gone from
//     the DOM and can never be confused with rendered output.
//   Phase 2 — renderMermaidInSlide: can be called many times (retries, theme
//     changes, resize). It only reads from data-mermaid-source, never from
//     innerHTML, so it is immune to re-parsing its own SVG output.

/** Replace mermaid code blocks with host divs that preserve the source text. */
const extractMermaidSources = (root: HTMLElement): void => {
  const codeBlocks = root.querySelectorAll(
    'pre > code.language-mermaid, pre > code.mermaid, pre.language-mermaid > code, pre.mermaid > code'
  );
  codeBlocks.forEach((block) => {
    if (!(block instanceof HTMLElement)) return;
    const source = (block.textContent ?? '').trim();
    if (!source) return;

    const host = document.createElement('div');
    host.className = 'mermaid-host';
    host.dataset.mermaidSource = source;
    host.style.display = 'flex';
    host.style.justifyContent = 'center';
    host.style.alignItems = 'center';
    host.style.width = '100%';

    const pre = block.closest('pre') ?? block.parentElement;
    pre?.parentElement?.replaceChild(host, pre);
  });
};

/** Render all mermaid host divs inside a single slide that haven't been rendered yet. */
const renderMermaidInSlide = async (slideEl: HTMLElement, isDark: boolean): Promise<void> => {
  // Collect hosts that still need rendering.
  const hosts = Array.from(
    slideEl.querySelectorAll<HTMLDivElement>('div.mermaid-host[data-mermaid-source]')
  ).filter((h) => h.dataset.mermaidRendered !== 'true');

  if (hosts.length === 0) return;

  // Prevent concurrent renders on the same slide.
  if (slideEl.dataset.mermaidRendering === 'true') return;
  slideEl.dataset.mermaidRendering = 'true';

  try {
    const mermaid = (await import('mermaid')).default;

    // Wait for web fonts so mermaid text measurement is stable.
    if ('fonts' in document && document.fonts?.ready) {
      await document.fonts.ready;
    }

    // Ensure slide has real dimensions before rendering.
    let ready = false;
    for (let i = 0; i < 6; i++) {
      const rect = slideEl.getBoundingClientRect();
      if (rect.width > 0 && rect.height > 0) { ready = true; break; }
      await new Promise<void>((r) => requestAnimationFrame(() => r()));
    }
    if (!ready) {
      // Schedule a retry — slide may not be visible yet.
      setTimeout(() => void renderMermaidInSlide(slideEl, isDark), 180);
      return;
    }

    const mermaidTheme: 'dark' | 'default' = isDark ? 'dark' : 'default';
    const baseConfig = {
      startOnLoad: false,
      theme: mermaidTheme,
      securityLevel: 'loose' as const,
      fontFamily: 'Inter, system-ui, -apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif',
      flowchart: { useMaxWidth: true, htmlLabels: false, curve: 'linear' as const },
      suppressErrorRendering: true,
    };
    mermaid.initialize(baseConfig);

    let hadFailure = false;
    const rendered: HTMLDivElement[] = [];

    for (let i = 0; i < hosts.length; i++) {
      const host = hosts[i]!;
      const source = host.dataset.mermaidSource!;
      // Normalize any <br/> from HTML into \n for the SVG-text primary path.
      const code = source.replace(/<br\s*\/?>/gi, '\\n');
      const id = `reveal-mermaid-${Date.now()}-${i}`;

      host.innerHTML = '';

      try {
        const { svg } = await mermaid.render(id, code);
        host.innerHTML = svg;
        host.dataset.mermaidRendered = 'true';
        rendered.push(host);
      } catch (primaryErr) {
        // Fallback: htmlLabels:true with \n→<br/> conversion.
        try {
          mermaid.initialize({ ...baseConfig, flowchart: { ...baseConfig.flowchart, htmlLabels: true } });
          const { svg } = await mermaid.render(`${id}-fb`, source.replace(/\\n/g, '<br/>'));
          host.innerHTML = svg;
          host.dataset.mermaidRendered = 'true';
          rendered.push(host);
          mermaid.initialize(baseConfig);
        } catch (fallbackErr) {
          hadFailure = true;
          console.warn('Mermaid render failed:', { primaryErr, fallbackErr, codePreview: source.slice(0, 160) });
          host.innerHTML = '<div style="color:#ef4444;font-size:0.9rem;">Mermaid render failed</div>';
        }
      }
    }

    // Post-render SVG sizing.
    if (rendered.length > 0) {
      const slideRect = slideEl.getBoundingClientRect();

      for (const node of rendered) {
        const svg = node.querySelector('svg');
        if (!svg) continue;

        let siblingHeight = 0;
        const parent = node.parentElement;
        if (parent) {
          for (let c = 0; c < parent.children.length; c++) {
            const child = parent.children[c] as HTMLElement;
            if (child !== node) siblingHeight += child.getBoundingClientRect().height || 0;
          }
        }

        const availH = Math.max(slideRect.height - siblingHeight - 32, 120);
        const availW = Math.max(slideRect.width - 24, 200);

        svg.querySelectorAll('foreignObject').forEach((fo) => {
          const w = Number(fo.getAttribute('width'));
          const x = Number(fo.getAttribute('x'));
          if (w > 0) {
            fo.setAttribute('width', String(w + 8));
            if (!Number.isNaN(x)) fo.setAttribute('x', String(x - 4));
          }
          const label = fo.querySelector('div');
          if (label instanceof HTMLElement) {
            label.style.overflow = 'visible';
            label.style.paddingRight = '4px';
          }
        });

        svg.style.display = 'block';
        svg.style.margin = '0 auto';
        svg.style.overflow = 'visible';
        svg.style.maxWidth = '100%';
        svg.style.maxHeight = `${availH}px`;
        svg.style.width = 'auto';
        svg.style.height = 'auto';
        svg.setAttribute('preserveAspectRatio', 'xMidYMid meet');

        node.style.margin = '0 auto';
        node.style.maxWidth = `${availW}px`;
        node.style.width = '100%';
      }
    }

    // Retry failed diagrams once after layout settles.
    if (hadFailure) {
      setTimeout(() => void renderMermaidInSlide(slideEl, isDark), 250);
    }
  } catch (err) {
    console.warn('Mermaid rendering failed:', err);
  } finally {
    delete slideEl.dataset.mermaidRendering;
  }
};

// Detect if a slide's rendered content overflows its bounds.
const isSlideOverflowing = (slideEl: HTMLElement): boolean => {
  const slideRect = slideEl.getBoundingClientRect();
  if (slideRect.width <= 0 || slideRect.height <= 0) return false;

  let maxBottom = slideRect.top;
  let maxRight = slideRect.left;

  for (let i = 0; i < slideEl.children.length; i++) {
    const child = slideEl.children[i] as HTMLElement;
    const rect = child.getBoundingClientRect();
    if (rect.width === 0 && rect.height === 0) continue;
    maxBottom = Math.max(maxBottom, rect.bottom);
    maxRight = Math.max(maxRight, rect.right);
  }

  // Add a tiny tolerance to avoid jitter on sub-pixel layouts.
  return maxBottom > slideRect.bottom + 1 || maxRight > slideRect.right + 1;
};

// Reduce slide font size until all content fits inside the slide bounds.
const fitSlideFontSize = (slideEl: HTMLElement): void => {
  // Start from default size each time so resizing/navigation can recover.
  slideEl.style.fontSize = '';

  // If already fitting, keep default font size.
  if (!isSlideOverflowing(slideEl)) return;

  const minScale = 0.72; // Don't shrink below 72% for readability.
  const step = 0.03;
  let scale = 1;

  while (scale > minScale && isSlideOverflowing(slideEl)) {
    scale = Math.max(minScale, Number((scale - step).toFixed(2)));
    slideEl.style.fontSize = `${Math.round(scale * 100)}%`;
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
  const { resolvedTheme } = useTheme();
  const isDark = resolvedTheme === 'dark';
  const themeLoadedRef = useRef<string | null>(null);

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

        // Protect underscores inside math blocks from being interpreted
        // as emphasis by the markdown parser (marked) before MathJax3
        // processes them. We must skip fenced code blocks and inline
        // code spans so that $operators (e.g. MongoDB's $lookup) aren't
        // mistaken for math delimiters.
        const protectedContent = (() => {
          const out: string[] = [];
          let i = 0;
          while (i < content.length) {
            // Skip fenced code blocks (``` ... ```)
            if (content.startsWith('```', i)) {
              const end = content.indexOf('```', i + 3);
              if (end !== -1) {
                out.push(content.slice(i, end + 3));
                i = end + 3;
                continue;
              }
            }
            // Skip inline code (` ... `)
            if (content[i] === '`') {
              const end = content.indexOf('`', i + 1);
              if (end !== -1) {
                out.push(content.slice(i, end + 1));
                i = end + 1;
                continue;
              }
            }
            // Display math: $$ ... $$
            if (content.startsWith('$$', i)) {
              const end = content.indexOf('$$', i + 2);
              if (end !== -1) {
                const math = content.slice(i + 2, end);
                out.push('$$' + math.replace(/_/g, '\\_') + '$$');
                i = end + 2;
                continue;
              }
            }
            // Inline math: $ ... $ (not followed by a word char, which
            // would indicate a MongoDB operator like $match)
            if (content[i] === '$' && !/[a-zA-Z]/.test(content[i + 1] ?? '')) {
              const lineEnd = content.indexOf('\n', i + 1);
              const searchEnd = lineEnd === -1 ? content.length : lineEnd;
              const end = content.indexOf('$', i + 1);
              if (end !== -1 && end < searchEnd) {
                const math = content.slice(i + 1, end);
                out.push('$' + math.replace(/_/g, '\\_') + '$');
                i = end + 1;
                continue;
              }
            }
            out.push(content[i]!);
            i++;
          }
          return out.join('');
        })();

        slidesRef.current.innerHTML = `<section data-markdown><textarea data-template>${protectedContent}</textarea></section>`;

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

        // Phase 1: extract mermaid sources from all slides immediately
        // after Reveal parses markdown. This replaces code blocks with
        // stable host divs before any render attempt can race.
        if (containerRef.current) {
          extractMermaidSources(containerRef.current);
        }

        // Add event listener to update URL hash and lazily render mermaid
        revealInstance.on('slidechanged', async (event: { indexh: number; indexv: number; currentSlide: HTMLElement }) => {
          updateHashWithSlide(event.indexh);
          // Render mermaid on the newly-visible slide
          if (event.currentSlide) {
            await renderMermaidInSlide(event.currentSlide, isDark);
            fitSlideFontSize(event.currentSlide);
            revealInstance.layout();
          }
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

        // Render mermaid on the initially visible slide
        setTimeout(async () => {
          if (revealInstanceRef.current) {
            const currentSlide = revealInstanceRef.current.getCurrentSlide();
            if (currentSlide) {
              await renderMermaidInSlide(currentSlide, isDark);
              fitSlideFontSize(currentSlide);
              revealInstanceRef.current.layout();
            }
          }
        }, 200);

        // Re-layout after deferred rendering settles
        setTimeout(() => {
          if (revealInstanceRef.current) {
            revealInstanceRef.current.layout();
            const currentSlide = revealInstanceRef.current.getCurrentSlide();
            if (currentSlide) {
              fitSlideFontSize(currentSlide);
              revealInstanceRef.current.layout();
            }
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
              const currentSlide = revealInstanceRef.current.getCurrentSlide();
              if (currentSlide) {
                fitSlideFontSize(currentSlide);
                revealInstanceRef.current.layout();
              }
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
  }, [isClient, isDark]);

  useEffect(() => {
    const updateContent = async () => {
      if (revealInstanceRef.current && slidesRef.current) {
        try {
          slidesRef.current.innerHTML = `<section data-markdown><textarea data-template>${content}</textarea></section>`;
          await revealInstanceRef.current.sync();

          // Re-extract mermaid sources after content change.
          if (containerRef.current) {
            extractMermaidSources(containerRef.current);
          }

          // Render mermaid on the current slide after content sync
          setTimeout(async () => {
            if (revealInstanceRef.current) {
              const currentSlide = revealInstanceRef.current.getCurrentSlide();
              if (currentSlide) {
                await renderMermaidInSlide(currentSlide, isDark);
                fitSlideFontSize(currentSlide);
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
  }, [content, isDark]);

  // Dynamically load the correct Reveal.js theme CSS
  useEffect(() => {
    if (!isClient) return;
    const themeFile = isDark ? 'black' : 'white';
    if (themeLoadedRef.current === themeFile) return;

    // Remove previously injected theme link
    const existingLink = document.getElementById('reveal-theme-link');
    if (existingLink) existingLink.remove();

    const link = document.createElement('link');
    link.id = 'reveal-theme-link';
    link.rel = 'stylesheet';
    link.href = `https://cdn.jsdelivr.net/npm/reveal.js@5/dist/theme/${themeFile}.css`;
    document.head.appendChild(link);
    themeLoadedRef.current = themeFile;

    // Re-layout after theme loads
    link.onload = () => {
      if (revealInstanceRef.current) {
        revealInstanceRef.current.layout();
      }
    };

    return () => {
      // Cleanup only when component unmounts, not on theme switch
    };
  }, [isClient, isDark]);

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
      className={`reveal-container w-full flex-1 flex flex-col overflow-hidden border rounded-lg ${isDark
        ? 'theme-dark border-gray-700 bg-[#18181b]'
        : 'theme-light border-gray-200 bg-white'
        }`}
    >
      <style dangerouslySetInnerHTML={{ __html: revealCodeStyles + darkModeOverrides + lightModeOverrides }} />
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