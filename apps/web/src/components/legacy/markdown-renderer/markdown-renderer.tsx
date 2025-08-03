import type React from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import remarkMath from 'remark-math';
import rehypeRaw from 'rehype-raw';
import rehypeKatex from 'rehype-katex';
import 'katex/dist/katex.min.css';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';

// Simple enum for renderer types since LectureEntity is not available
enum RendererType {
  Markdown = 'markdown',
  Reveal = 'reveal',
}

export interface MarkdownRendererProps {
  content: string;
  renderer?: RendererType;
}

const MarkdownRenderer: React.FC<MarkdownRendererProps> = ({ content, renderer = RendererType.Markdown }) => {
  if (renderer === RendererType.Reveal) {
    return (
      <div className="gameguild-revealjs-wrapper">
        <div>RevealJS renderer not available</div>
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
    h1: (props) => <h1 className="text-4xl font-bold mt-6 mb-4" {...props} />,
    h2: (props) => <h2 className="text-3xl font-semibold mt-5 mb-3" {...props} />,
    h3: (props) => <h3 className="text-2xl font-semibold mt-4 mb-2" {...props} />,
    h4: (props) => <h4 className="text-xl font-semibold mt-3 mb-2" {...props} />,
    h5: (props) => <h5 className="text-lg font-semibold mt-2 mb-1" {...props} />,
    h6: (props) => <h6 className="text-base font-semibold mt-2 mb-1" {...props} />,
    p: (props) => <p className="mb-4" {...props} />,
    ul: (props) => <ul className="list-disc pl-5 mb-4" {...props} />,
    ol: (props) => <ol className="list-decimal pl-5 mb-4" {...props} />,
    li: (props) => <li className="mb-1" {...props} />,
    a: (props) => <a className="text-blue-600 hover:underline" {...props} />,
    blockquote: (props) => <blockquote className="border-l-4 border-gray-300 pl-4 italic my-4" {...props} />,
    code: ({ node, className, children, ...props }: any) => {
      const match = /language-(\w+)/.exec(className || '');
      const lang = match && match[1] ? match[1] : '';

      if (lang === 'mermaid') {
        return <div>Mermaid chart not available</div>;
      }

      const codeContent = String(children).replace(/\n$/, '');
      const inline = !codeContent.includes('\n');

      if (!inline) {
        return (
          <SyntaxHighlighter
            style={vscDarkPlus}
            language={lang}
            PreTag="div"
            customStyle={{
              padding: '1rem',
              borderRadius: '0.375rem',
              marginBottom: '1rem',
            }}
            codeTagProps={{
              style: {
                whiteSpace: 'pre-wrap',
                wordBreak: 'keep-all',
                overflowWrap: 'break-word',
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
        <code className="bg-gray-100 border border-gray-300 rounded-full px-2 py-1 font-mono text-sm inline whitespace-nowrap" {...props}>
          {children}
        </code>
      );
    },
    pre: ({ children }: any) => <>{children}</>,
    div: ({ className, children, ...props }: any) => {
      if (className?.includes('admonition')) {
        const type = className.split('-')[1];
        const title = props['data-title'] as string | undefined;
        return (
          <div className={`border-l-4 p-4 my-4 ${type === 'warning' ? 'border-yellow-400 bg-yellow-50' : type === 'danger' ? 'border-red-400 bg-red-50' : type === 'info' ? 'border-blue-400 bg-blue-50' : 'border-gray-400 bg-gray-50'}`}>
            {title && <div className="font-semibold mb-2">{title}</div>}
            {children}
          </div>
        );
      }
      if (className === 'markdown-activity') {
        const type = props['data-type'];
        if (type === 'quiz' || type === 'code') {
          return <div className="p-4 border rounded-md">Activity: {type} (not implemented)</div>;
        }
      }
      return (
        <div className={className} {...props}>
          {children}
        </div>
      );
    },
  };

  return (
    <div className="markdown-content">
      <ReactMarkdown remarkPlugins={[remarkGfm, remarkMath]} rehypePlugins={[rehypeRaw, rehypeKatex]} components={components}>
        {processedContent}
      </ReactMarkdown>
      <style jsx global>{`
        .syntax-highlighter {
          overflow-x: auto;
        }

        .syntax-highlighter pre {
          white-space: pre-wrap !important;
          word-break: keep-all !important;
          overflow-wrap: break-word !important;
        }

        .katex-display {
          overflow-x: auto;
          overflow-y: hidden;
          padding: 0.5rem 0;
        }
      `}</style>
    </div>
  );
};

export default MarkdownRenderer;
