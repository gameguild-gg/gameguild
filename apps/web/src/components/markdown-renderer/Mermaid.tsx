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
      securityLevel: 'loose', // Allow HTML in labels
      fontFamily: 'inherit',
      htmlLabels: true,
      flowchart: {
        htmlLabels: true,
        useMaxWidth: true,
        curve: 'linear',
        nodeSpacing: 50,
        rankSpacing: 50
      },
      themeVariables: {
        primaryTextColor: '#333',
        primaryBorderColor: '#333',
        lineColor: '#333',
        secondaryColor: '#f0f0f0',
        tertiaryColor: '#ffffff',
        background: '#ffffff',
        primaryColor: '#ffffff',
        backgroundSecondary: '#ffffff',
        backgroundTertiary: '#ffffff',
        mainBkg: '#ffffff',
        secondBkg: '#f0f0f0',
        tertiaryBkg: '#ffffff',
      }
    });

    const renderChart = async (): Promise<void> => {
      if (!containerRef.current) return;

      try {
        containerRef.current.innerHTML = '';

        // Convert \n to <br/> for proper line breaks in HTML labels
        const processedChart = chart.replace(/\\n/g, '<br/>');

        const { svg } = await mermaid.render(
          `mermaid-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
          processedChart,
        );
        containerRef.current.innerHTML = svg;

        // Fix node heights - mermaid sometimes miscalculates when using <br/> tags
        setTimeout(() => {
          try {
            const svgElement = containerRef.current?.querySelector('svg');
            if (svgElement) {
              // Find all foreignObject elements (used for HTML labels) and ensure they have proper height
              const foreignObjects = svgElement.querySelectorAll('foreignObject');
              foreignObjects.forEach((fo) => {
                const div = fo.querySelector('div');
                if (div) {
                  // Get actual content height and add padding
                  const contentHeight = div.scrollHeight + 20;
                  const heightAttr = fo.getAttribute('height');
                  const currentHeight = heightAttr !== null && heightAttr !== '' ? parseFloat(heightAttr) : 0;
                  if (contentHeight > currentHeight) {
                    fo.setAttribute('height', String(contentHeight));
                  }
                }
              });

              // Extract dimensions from viewBox to use as intrinsic size
              const viewBox = svgElement.getAttribute('viewBox');
              if (viewBox) {
                const parts = viewBox.split(' ');
                if (parts.length === 4) {
                  const vbWidth = parseFloat(parts[2]);
                  const vbHeight = parseFloat(parts[3]);
                  // Set explicit dimensions from viewBox so SVG renders at natural size
                  // max-width will clamp if it exceeds container
                  svgElement.style.width = `${vbWidth}px`;
                  svgElement.style.height = `${vbHeight}px`;
                  svgElement.style.maxWidth = '100%';
                  svgElement.style.maxHeight = '100%';
                  // Remove width/height attributes to let CSS control sizing
                  svgElement.removeAttribute('width');
                  svgElement.removeAttribute('height');
                }
              }
            }
          } catch {
            // Silently ignore DOM manipulation errors
          }
        }, 100);

        setError(null);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to render the diagram. Please check your syntax.';
        console.error('Mermaid rendering failed:', errorMessage);
        setError(errorMessage);
      }
    };

    renderChart();
  }, [chart, isClient]);

  if (!isClient) {
    return <div>Loading diagram...</div>;
  }

  if (error !== null) {
    return <div className="text-red-500">{error}</div>;
  }

  return (
    <div
      ref={containerRef}
      className="mermaid-container"
      style={{
        textAlign: 'center',
        margin: '1rem auto',
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        width: 'fit-content',
        maxWidth: '100%',
        backgroundColor: '#ffffff',
        borderRadius: '0.5rem',
        padding: '1rem',
        overflow: 'auto'
      }}
    />
  );
};

export default Mermaid;
