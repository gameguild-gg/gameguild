'use client';

import mermaid from 'mermaid';
import React, { useEffect, useRef, useState } from 'react';
import './mermaid.css';

interface MermaidProps {
  chart: string;
}

const Mermaid: React.FC<MermaidProps> = ({ chart }) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const [error, setError] = useState<string | null>(null);
  const [isClient, setIsClient] = useState(false);

  useEffect(() => {
    setIsClient(true);
  }, []);

  useEffect(() => {
    if (!isClient) return;

    mermaid.initialize({
      startOnLoad: false,
      theme: 'default',
      securityLevel: 'strict',
      fontFamily: 'inherit',
      // Prevent Mermaid from automatically scaling the SVG
      htmlLabels: true,
      themeVariables: {
        primaryTextColor: '#333',
        primaryBorderColor: '#333',
        lineColor: '#333',
        secondaryColor: '#f0f0f0',
        tertiaryColor: '#fff',
      }
    });

    const renderChart = async () => {
      if (!containerRef.current) return;

      try {
        containerRef.current.innerHTML = '';
        const { svg } = await mermaid.render(
          `mermaid-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
          chart,
        );
        containerRef.current.innerHTML = svg;

        // Apply proper scaling behavior with a small delay to ensure proper measurement
        setTimeout(() => {
          const svgElement = containerRef.current?.querySelector('svg');
          if (svgElement && containerRef.current) {
            const containerWidth = containerRef.current.offsetWidth;

            // Remove any width/height attributes that Mermaid might have set
            svgElement.removeAttribute('width');
            svgElement.removeAttribute('height');

            // Get the viewBox to calculate natural dimensions
            const viewBox = svgElement.getAttribute('viewBox');
            let naturalWidth = 0;
            let naturalHeight = 0;

            if (viewBox) {
              const parts = viewBox.split(' ').map(Number);
              if (parts.length >= 4 && parts[2] !== undefined && parts[3] !== undefined) {
                naturalWidth = parts[2];
                naturalHeight = parts[3];
              }
            }

            // Get computed styles to see what's actually being applied
            const computedStyle = window.getComputedStyle(svgElement);
            const computedWidth = computedStyle.width;
            const computedHeight = computedStyle.height;

            console.log('Mermaid scaling debug:', {
              containerWidth,
              naturalWidth,
              naturalHeight,
              viewBox,
              computedWidth,
              computedHeight,
              chart: chart.substring(0, 50) + '...'
            });

            if (naturalWidth > 0) {
              // Only scale down if the SVG is larger than the container
              if (naturalWidth > containerWidth) {
                console.log('Scaling DOWN: natural width', naturalWidth, '> container width', containerWidth);
                svgElement.style.setProperty('width', '100%', 'important');
                svgElement.style.setProperty('height', 'auto', 'important');
                svgElement.style.setProperty('max-width', '100%', 'important');
              } else {
                console.log('Keeping natural size: natural width', naturalWidth, '<= container width', containerWidth);
                // Force natural size and prevent any scaling up
                svgElement.style.setProperty('width', `${naturalWidth}px`, 'important');
                svgElement.style.setProperty('height', `${naturalHeight}px`, 'important');
                svgElement.style.setProperty('max-width', `${naturalWidth}px`, 'important');
                svgElement.style.setProperty('min-width', `${naturalWidth}px`, 'important');
                svgElement.style.setProperty('flex-shrink', '0', 'important');
                svgElement.style.setProperty('flex-grow', '0', 'important');
              }

              // Log the final computed styles
              setTimeout(() => {
                const finalComputedStyle = window.getComputedStyle(svgElement);
                console.log('Final computed styles:', {
                  width: finalComputedStyle.width,
                  height: finalComputedStyle.height,
                  maxWidth: finalComputedStyle.maxWidth,
                  minWidth: finalComputedStyle.minWidth
                });
              }, 50);
            }
          }
        }, 10);

        setError(null);
      } catch (err) {
        console.error('Mermaid rendering failed:', err);
        const errorMessage = err instanceof Error ? err.message : 'Failed to render the diagram. Please check your syntax.';
        setError(errorMessage);
      }
    };

    renderChart();
  }, [chart, isClient]);

  if (!isClient) {
    return <div>Loading diagram...</div>;
  }

  if (error) {
    return <div className="text-red-500">{error}</div>;
  }

  return (
    <div
      ref={containerRef}
      className="mermaid-container"
      style={{
        textAlign: 'center',
        margin: '1rem 0',
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        width: 'auto',
        maxWidth: '100%'
      }}
    />
  );
};

export default Mermaid;