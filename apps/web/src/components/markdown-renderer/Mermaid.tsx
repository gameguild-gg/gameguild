'use client';

import React, { useEffect, useRef, useState } from 'react';
import mermaid from 'mermaid';

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
      flowchart: {
        nodeSpacing: 30,
        rankSpacing: 30,
        curve: 'basis',
        useMaxWidth: false,
        htmlLabels: false,
      },
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
    return <div className="mermaid-container">Loading diagram...</div>;
  }

  if (error) {
    return <div className="text-red-500">{error}</div>;
  }

  return (
    <div 
      ref={containerRef} 
      className="mermaid-container" 
      style={{
        width: 'auto',
        margin: '1rem auto',
        overflow: 'visible'
      }}
    />
  );
};

export default Mermaid; 