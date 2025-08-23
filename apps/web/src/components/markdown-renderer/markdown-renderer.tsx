'use client';

import 'katex/dist/katex.min.css';
import { useTheme } from 'next-themes';
import NextImage from 'next/image';
import React, { useEffect, useState } from 'react';
import ReactMarkdown from 'react-markdown';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vs, vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';
import rehypeKatex from 'rehype-katex';
import rehypeRaw from 'rehype-raw';
import remarkGfm from 'remark-gfm';
import remarkMath from 'remark-math';
import { Admonition } from './Admonition';
import { MarkdownCodeActivity } from './MarkdownCodeActivity';
import { MarkdownErrorBoundary } from './MarkdownErrorBoundary';
import { MarkdownQuizActivity } from './MarkdownQuizActivity';
import Mermaid from './Mermaid';
import RevealJS from './RevealJS';

export type RendererType = 'markdown' | 'reveal';

export interface MarkdownRendererProps {
  content: string;
  renderer?: RendererType;
}

const MarkdownRenderer: React.FC<MarkdownRendererProps> = ({ content, renderer = 'markdown' }) => {
  const [isClient, setIsClient] = useState(false);
  const { theme, resolvedTheme } = useTheme();
  const isDark = resolvedTheme === 'dark';

  useEffect(() => {
    setIsClient(true);
  }, []);

  if (renderer === 'reveal') {
    return (
      <div className="gameguild-revealjs-wrapper">
        <RevealJS content={content} />
      </div>
    );
  }

  const processedContent = content
    .replace(
      /:::\s*(note|abstract|info|tip|success|question|warning|failure|danger|bug|example|quote)(?:\s+"([^"]*)")?\n([\s\S]*?):::/g,
      (_, type, title, body) => `<div class="admonition admonition-${type}"${title ? ` data-title="${title}"` : ''}>\n\n${body}\n\n</div>`,
    )
    .replace(/!!!\s*(quiz|code)\n([\s\S]*?)\n!!!/g, (_, type, content) => {
      // HTML escape angle brackets in the content if it's a code block
      if (type === 'code') {
        content = content.replace(/</g, '&lt;').replace(/>/g, '&gt;');
      }
      return `<div class="markdown-activity" data-type="${type}">${content}</div>`;
    });

  const components: Record<string, React.FC<any>> = {
    h1: (props) => <h1 className="text-4xl font-bold mt-6 mb-4 text-foreground" {...props} />,
    h2: (props) => <h2 className="text-3xl font-semibold mt-5 mb-3 text-foreground" {...props} />,
    h3: (props) => <h3 className="text-2xl font-semibold mt-4 mb-2 text-foreground" {...props} />,
    h4: (props) => <h4 className="text-xl font-semibold mt-3 mb-2 text-foreground" {...props} />,
    h5: (props) => <h5 className="text-lg font-semibold mt-2 mb-1 text-foreground" {...props} />,
    h6: (props) => <h6 className="text-base font-semibold mt-2 mb-1 text-foreground" {...props} />,
    p: (props) => <p className="mb-4 text-foreground" {...props} />,
    img: (props) => (
      <NextImage
        className="max-w-full h-auto rounded-lg shadow-sm"
        width={0}
        height={0}
        sizes="100vw"
        style={{ width: '100%', height: 'auto', maxWidth: '100%' }}
        {...props}
      />
    ),
    ul: (props) => <ul className="list-disc pl-5 mb-4 text-foreground" {...props} />,
    ol: (props) => <ol className="list-decimal pl-5 mb-4 text-foreground" {...props} />,
    li: (props) => <li className="mb-1 text-foreground" {...props} />,
    a: (props) => <a className="text-primary hover:text-primary/80 underline" {...props} />,
    blockquote: (props) => <blockquote className="border-l-4 border-border pl-4 italic my-4 text-muted-foreground" {...props} />,
    code: ({ node, className, children, ...props }) => {
      const match = /language-(\w+)/.exec(className || '');
      const lang = match && match[1] ? match[1] : '';

      if (lang === 'mermaid') {
        return <Mermaid chart={String(children).replace(/\n$/, '')} />;
      }

      const codeContent = String(children).replace(/\n$/, '');
      const inline = !codeContent.includes('\n');

      if (!inline) {
        return (
          <SyntaxHighlighter
            style={isDark ? vscDarkPlus : vs}
            language={lang}
            PreTag="div"
            customStyle={{
              padding: '1rem',
              borderRadius: '0.5rem',
              marginBottom: '1rem',
              backgroundColor: isDark ? 'hsl(var(--muted))' : 'hsl(var(--background))',
              border: '1px solid hsl(var(--border))',
              color: 'hsl(var(--foreground))',
            }}
            codeTagProps={{
              style: {
                whiteSpace: 'pre-wrap',
                wordBreak: 'keep-all',
                overflowWrap: 'break-word',
                fontSize: '0.875rem',
                lineHeight: '1.6',
                fontFamily: 'ui-monospace, SFMono-Regular, "SF Mono", Consolas, "Liberation Mono", Menlo, monospace',
                color: 'inherit',
              },
            }}
            wrapLines={true}
            className="syntax-highlighter"
          >
            {String(children).replace(/\n$/, '')}
          </SyntaxHighlighter>
        );
      }

      return (
        <code className="bg-muted border border-border rounded px-1.5 py-0.5 font-mono text-sm inline whitespace-nowrap text-foreground font-medium" {...props}>
          {children}
        </code>
      );
    },
    pre: ({ children }) => <>{children}</>,
    div: ({ className, children, ...props }) => {
      if (className?.includes('admonition')) {
        const type = className.split('-')[1] as
          | 'note'
          | 'abstract'
          | 'info'
          | 'tip'
          | 'success'
          | 'question'
          | 'warning'
          | 'failure'
          | 'danger'
          | 'bug'
          | 'example'
          | 'quote';
        const title = props['data-title'] as string | undefined;
        return (
          <Admonition type={type} title={title}>
            {children}
          </Admonition>
        );
      }
      if (className === 'markdown-activity') {
        const type = props['data-type'];
        if (type === 'quiz' || type === 'code') {
          try {
            // remove new lines if it is code
            const jsonString = children as string;
            const processedString = type === 'code' ? jsonString.replace(/\n/g, '') : jsonString;
            const data = JSON.parse(processedString);
            if (type === 'quiz') {
              return <MarkdownQuizActivity {...data} />;
            } else if (type === 'code') {
              return <MarkdownCodeActivity {...data} />;
            }
          } catch (error) {
            console.error('Error parsing custom block:', error);
            // Create a proper error object if the caught error is not an Error instance
            const errorObj = error instanceof Error ? error : new Error(String(error));
            return (
              <div className="p-4 bg-red-50 border border-red-200 rounded-md">
                <p className="text-red-800 font-medium">Error rendering custom block</p>
                <p className="text-red-600 text-sm mt-1">{errorObj.message}</p>
              </div>
            );
          }
        }
      }
      return (
        <div className={className} {...props}>
          {children}
        </div>
      );
    },
  };

  if (!isClient) {
    return <div className="markdown-content">Loading content...</div>;
  }

  return (
    <MarkdownErrorBoundary>
      <div className="markdown-content">
        <ReactMarkdown
          remarkPlugins={[remarkGfm, remarkMath]}
          rehypePlugins={[
            rehypeRaw,
            [
              rehypeKatex,
              {
                strict: false,
                trust: true,
                throwOnError: false,
                errorColor: 'hsl(var(--destructive))',
                macros: {
                  '\\RR': '\\mathbb{R}',
                  '\\NN': '\\mathbb{N}',
                  '\\ZZ': '\\mathbb{Z}',
                  '\\QQ': '\\mathbb{Q}',
                  '\\CC': '\\mathbb{C}',
                },
              },
            ],
          ]}
          components={components}
        >
          {processedContent}
        </ReactMarkdown>
        <style jsx global>{`
        .markdown-content {
          line-height: 1.7;
          width: 100% !important;
          max-width: none !important;
          min-width: 100% !important;
          color: hsl(var(--foreground)) !important;
        }

        .markdown-content h1,
        .markdown-content h2,
        .markdown-content h3,
        .markdown-content h4,
        .markdown-content h5,
        .markdown-content h6 {
          font-weight: 600;
          line-height: 1.25;
          color: hsl(var(--foreground)) !important;
        }

        .markdown-content h1 {
          font-size: 2.25rem;
          margin-top: 2rem;
          margin-bottom: 1rem;
        }

        .markdown-content h2 {
          font-size: 1.875rem;
          margin-top: 1.75rem;
          margin-bottom: 0.75rem;
        }

        .markdown-content h3 {
          font-size: 1.5rem;
          margin-top: 1.5rem;
          margin-bottom: 0.5rem;
        }

        .markdown-content p {
          margin-bottom: 1rem;
          line-height: 1.7;
          color: hsl(var(--foreground)) !important;
        }

        .markdown-content ul,
        .markdown-content ol {
          margin-bottom: 1rem;
          padding-left: 1.5rem;
          color: hsl(var(--foreground)) !important;
        }

        .markdown-content li {
          margin-bottom: 0.25rem;
          color: hsl(var(--foreground)) !important;
        }

        .markdown-content img {
          max-width: 100% !important;
          width: 100% !important;
          height: auto !important;
          display: block !important;
          margin: 1rem auto !important;
          border-radius: 0.5rem !important;
          box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06) !important;
          object-fit: contain !important;
        }

        .markdown-content blockquote {
          border-left: 4px solid hsl(var(--border));
          padding-left: 1rem;
          margin: 1rem 0;
          font-style: italic;
          color: hsl(var(--muted-foreground));
        }

        .markdown-content a {
          color: hsl(var(--primary));
          text-decoration: underline;
        }

        .markdown-content a:hover {
          color: hsl(var(--primary) / 0.8);
        }

        .syntax-highlighter {
          overflow-x: auto;
          box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
          background-color: hsl(var(--muted)) !important;
          border: 1px solid hsl(var(--border)) !important;
        }

        .syntax-highlighter pre {
          white-space: pre-wrap !important;
          word-break: keep-all !important;
          overflow-wrap: break-word !important;
          margin: 0 !important;
          background-color: transparent !important;
        }

        .syntax-highlighter code {
          font-family: ui-monospace, SFMono-Regular, "SF Mono", Consolas, "Liberation Mono", Menlo, monospace !important;
          color: hsl(var(--foreground)) !important;
        }

        /* KaTeX LaTeX formula styling */
        .katex-display {
          overflow-x: auto;
          overflow-y: hidden;
          padding: 1rem 0;
          margin: 1.5rem 0;
          text-align: center;
          background-color: hsl(var(--muted) / 0.3);
          border-radius: 0.5rem;
          border: 1px solid hsl(var(--border));
        }

        .katex {
          font-size: 1.1em;
          color: hsl(var(--foreground)) !important;
        }

        .katex-display .katex {
          font-size: 1.2em;
        }

        /* Inline math styling */
        .markdown-content .katex {
          background-color: hsl(var(--muted) / 0.2);
          padding: 0.1rem 0.3rem;
          border-radius: 0.25rem;
          border: 1px solid hsl(var(--border) / 0.5);
        }

        /* Display math should not have inline styling */
        .katex-display .katex {
          background-color: transparent;
          padding: 0;
          border: none;
          border-radius: 0;
        }

        /* Dark mode adjustments for KaTeX */
        .katex .mord,
        .katex .mop,
        .katex .mrel,
        .katex .mbin,
        .katex .mpunct,
        .katex .mopen,
        .katex .mclose {
          color: hsl(var(--foreground)) !important;
        }

        /* Ensure proper spacing and alignment */
        .katex-display {
          line-height: 1.2;
        }

        /* Handle long formulas with horizontal scroll */
        .katex-display .katex {
          max-width: 100%;
          overflow-x: auto;
          overflow-y: hidden;
        }

        /* Error handling for malformed LaTeX */
        .katex-error {
          color: hsl(var(--destructive)) !important;
          background-color: hsl(var(--destructive) / 0.1);
          padding: 0.25rem 0.5rem;
          border-radius: 0.25rem;
          border: 1px solid hsl(var(--destructive) / 0.3);
          font-family: ui-monospace, SFMono-Regular, monospace;
          font-size: 0.875rem;
        }

        .markdown-content table {
          width: 100%;
          border-collapse: collapse;
          margin: 1rem 0;
        }

        .markdown-content th,
        .markdown-content td {
          border: 1px solid hsl(var(--border));
          padding: 0.5rem;
          text-align: left;
        }

        .markdown-content th {
          background-color: hsl(var(--muted));
          font-weight: 600;
        }

        /* Ensure RevealJS doesn't interfere with page layout */
        .reveal-container {
          position: relative !important;
          z-index: 1;
        }

        .reveal-container .reveal {
          position: relative !important;
          height: 100% !important;
        }

        .reveal-container .slides {
          position: relative !important;
          height: 100% !important;
        }

        /* Override any global RevealJS styles that might affect layout */
        .reveal {
          position: relative !important;
          height: 100% !important;
          width: 100% !important;
          background: hsl(var(--background)) !important;
        }
        
        .reveal .slides {
          position: relative !important;
          height: 100% !important;
          width: 100% !important;
          background: hsl(var(--background)) !important;
        }
        
        .reveal .slides section {
          background: hsl(var(--background)) !important;
        }

        /* Prevent any black backgrounds from affecting the page */
        body {
          background-color: hsl(var(--background)) !important;
        }
        
        html {
          background-color: hsl(var(--background)) !important;
        }

        /* Ensure proper width constraints for markdown content */
        .markdown-content pre,
        .markdown-content code {
          max-width: 100% !important;
          overflow-x: auto !important;
        }

        .syntax-highlighter {
          max-width: 100% !important;
          overflow-x: auto !important;
        }

        /* Mermaid diagram sizing - smart scaling */
        .mermaid-container {
          width: auto !important;
          max-width: 100% !important;
          margin: 1rem auto !important;
          overflow: visible !important;
          display: flex !important;
          justify-content: center !important;
          align-items: center !important;
          text-align: center !important;
        }

        .mermaid-container svg {
          max-width: none !important;
          height: auto !important;
          display: inline-block !important;
          flex-shrink: 0 !important;
          flex-grow: 0 !important;
        }

        /* Let Mermaid handle text sizing and positioning naturally */
        .mermaid-container {
          font-family: inherit !important;
        }

        /* Responsive design for LaTeX formulas on mobile */
        @media (max-width: 768px) {
          .katex-display {
            padding: 0.75rem 0.5rem;
            margin: 1rem 0;
            font-size: 0.9em;
          }

          .katex {
            font-size: 1em;
          }

          .katex-display .katex {
            font-size: 1.1em;
          }

          /* Ensure formulas don't break layout on small screens */
          .katex-display .katex {
            white-space: nowrap;
            overflow-x: auto;
            overflow-y: hidden;
            padding-bottom: 0.5rem;
          }
        }

        /* Improve readability of math symbols */
        .katex .accent-body {
          color: hsl(var(--foreground)) !important;
        }

        .katex .frac-line {
          border-bottom-color: hsl(var(--foreground)) !important;
        }

        .katex .sqrt > .root {
          color: hsl(var(--foreground)) !important;
        }
      `}</style>
      </div>
    </MarkdownErrorBoundary>
  );
};

export default MarkdownRenderer;