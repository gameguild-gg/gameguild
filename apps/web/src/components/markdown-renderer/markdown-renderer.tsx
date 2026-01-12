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
import './markdown-renderer.css';
import { MarkdownCodeActivity } from './MarkdownCodeActivity';
import { MarkdownErrorBoundary } from './MarkdownErrorBoundary';
import { MarkdownQuizActivity } from './MarkdownQuizActivity';
import Marp from './Marp';
import Mermaid from './Mermaid';
import RemarkJS from './RemarkJS';
import RevealJS from './RevealJS';

export type RendererType = 'markdown' | 'reveal' | 'marp' | 'remark';

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
      <div className="gameguild-revealjs-wrapper relative w-full h-full min-h-[50vh] sm:min-h-[60vh] md:min-h-[70vh] lg:min-h-[80vh]">
        <RevealJS content={content}  />
      </div>
    );
  }

  if (renderer === 'marp') {
    return (
      <div className="gameguild-marp-wrapper">
        <Marp content={content} />
      </div>
    );
  }

  if (renderer === 'remark') {
    return (
      <div className="gameguild-remarkjs-wrapper">
        <RemarkJS content={content} />
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
      const isFenced = !!match; // fenced code blocks provide a language class
      const isBlock = isFenced || codeContent.includes('\n');

      if (isBlock) {
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
              overflow: 'visible',
              maxWidth: '100%',
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-word',
              wordWrap: 'break-word',
              overflowWrap: 'break-word',
            }}
            codeTagProps={{
              style: {
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
                wordWrap: 'break-word',
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
        <code
          className="bg-muted border border-border rounded px-1 py-0.5 font-mono text-sm inline text-foreground font-medium"
          style={{ wordBreak: 'break-word', overflowWrap: 'anywhere', whiteSpace: 'normal', verticalAlign: 'baseline' }}
          {...props}
        >
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
      </div>
    </MarkdownErrorBoundary>
  );
};

export default MarkdownRenderer;