import 'katex/dist/katex.min.css';
import type React from 'react';
import ReactMarkdown from 'react-markdown';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';
import rehypeKatex from 'rehype-katex';
import rehypeRaw from 'rehype-raw';
import remarkGfm from 'remark-gfm';
import remarkMath from 'remark-math';

export interface MarkdownRendererProps {
    content: string;
}

type MarkdownDivProps = React.HTMLAttributes<HTMLDivElement> & {
    'data-title'?: string;
    'data-type'?: string;
};

export default function MarkdownRenderer({ content }: MarkdownRendererProps) {
    const processedContent = content
        .replace(
            /:::\s*(note|abstract|info|tip|success|question|warning|failure|danger|bug|example|quote)(?:\s+"([^"]*)")?\n([\s\S]*?):::/g,
            (_, type, title, body) => `<div class="admonition admonition-${type}"${title ? ` data-title="${title}"` : ''}>\n\n${body}\n\n</div>`,
        )
        .replace(/!!!\s*(quiz|code)\n([\s\S]*?)\n!!!/g, (_, type, body) => {
            const safeBody = type === 'code' ? body.replace(/</g, '&lt;').replace(/>/g, '&gt;') : body;
            return `<div class="markdown-activity" data-type="${type}">${safeBody}</div>`;
        });

    return (
        <div className="prose prose-invert max-w-none prose-pre:bg-transparent">
            <ReactMarkdown
                remarkPlugins={[remarkGfm, remarkMath]}
                rehypePlugins={[rehypeRaw, rehypeKatex]}
                components={{
                    h1: (props) => <h1 className="mt-6 mb-4 text-4xl font-bold" {...props} />,
                    h2: (props) => <h2 className="mt-5 mb-3 text-3xl font-semibold" {...props} />,
                    h3: (props) => <h3 className="mt-4 mb-2 text-2xl font-semibold" {...props} />,
                    p: (props) => <p className="mb-4 text-slate-200" {...props} />,
                    ul: (props) => <ul className="mb-4 list-disc pl-5" {...props} />,
                    ol: (props) => <ol className="mb-4 list-decimal pl-5" {...props} />,
                    li: (props) => <li className="mb-1" {...props} />,
                    a: (props) => <a className="text-sky-400 hover:text-sky-300 hover:underline" {...props} />,
                    blockquote: (props) => <blockquote className="my-4 border-l-4 border-slate-500 pl-4 italic text-slate-300" {...props} />,
                    code: ({ className, children, ...props }: React.HTMLAttributes<HTMLElement> & { className?: string }) => {
                        const match = /language-(\w+)/.exec(className || '');
                        const language = match && match[1] ? match[1] : '';
                        const code = String(children).replace(/\n$/, '');
                        const inline = !code.includes('\n');

                        if (!inline) {
                            return (
                                <SyntaxHighlighter
                                    style={vscDarkPlus}
                                    language={language}
                                    PreTag="div"
                                    customStyle={{
                                        padding: '1rem',
                                        borderRadius: '0.75rem',
                                        marginBottom: '1rem',
                                        backgroundColor: '#020617',
                                    }}
                                    codeTagProps={{
                                        style: {
                                            whiteSpace: 'pre-wrap',
                                            wordBreak: 'keep-all',
                                            overflowWrap: 'break-word',
                                        },
                                    }}
                                    wrapLines={true}
                                >
                                    {code}
                                </SyntaxHighlighter>
                            );
                        }

                        return (
                            <code className="rounded-full border border-slate-700 bg-slate-900 px-2 py-1 font-mono text-sm text-slate-100" {...props}>
                                {children}
                            </code>
                        );
                    },
                    pre: ({ children }) => <>{children}</>,
                    div: ({ className, children, ...props }: MarkdownDivProps) => {
                        if (className?.includes('admonition')) {
                            const type = className.split('-')[1];
                            const title = props['data-title'];
                            const tone =
                                type === 'warning'
                                    ? 'border-yellow-400 bg-yellow-500/10'
                                    : type === 'danger'
                                        ? 'border-red-400 bg-red-500/10'
                                        : type === 'info'
                                            ? 'border-sky-400 bg-sky-500/10'
                                            : 'border-slate-500 bg-slate-900';

                            return (
                                <div className={`my-4 rounded-xl border-l-4 p-4 ${tone}`}>
                                    {title ? <div className="mb-2 font-semibold text-white">{title}</div> : null}
                                    {children}
                                </div>
                            );
                        }

                        if (className === 'markdown-activity') {
                            const activityType = props['data-type'] === 'code' ? 'code' : 'quiz';
                            const label = activityType === 'code' ? 'Code activity' : 'Knowledge check';
                            const tone = activityType === 'code'
                                ? 'border-violet-500/40 bg-violet-500/10'
                                : 'border-emerald-500/40 bg-emerald-500/10';

                            return (
                                <div className={`my-4 rounded-xl border p-4 text-sm text-slate-100 ${tone}`}>
                                    <div className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-slate-300">{label}</div>
                                    <div className="whitespace-pre-wrap leading-6 text-slate-100">{children}</div>
                                </div>
                            );
                        }

                        return (
                            <div className={className} {...props}>
                                {children}
                            </div>
                        );
                    },
                }}
            >
                {processedContent}
            </ReactMarkdown>
        </div>
    );
}
