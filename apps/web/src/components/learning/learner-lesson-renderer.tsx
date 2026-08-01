'use client';

import { recordLessonEvent } from '@/lib/learner/lesson-interaction-actions';
import type { LearningCoursesLessonContentFormat } from '@game-guild/client';
import { MarkdownRenderer } from '@game-guild/content-rendering';
import { Button } from '@game-guild/ui/components/button';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import Image from 'next/image';
import { createElement, type ReactNode, useRef, useState } from 'react';

type ContentRecord = Record<string, unknown>;
type LexicalNode = ContentRecord & { type?: string; text?: string; tag?: string; url?: string; children?: LexicalNode[] };

function asRecord(content: unknown): ContentRecord | null {
    if (content && typeof content === 'object' && !Array.isArray(content)) return content as ContentRecord;
    if (typeof content === 'string') {
        try { const parsed = JSON.parse(content) as unknown; return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed as ContentRecord : null; } catch { return null; }
    }
    return null;
}

function textContent(content: unknown): string {
    if (typeof content === 'string') return content;
    const record = asRecord(content);
    if (!record) return '';
    for (const key of ['markdown', 'content', 'text', 'source']) {
        if (typeof record[key] === 'string') return record[key] as string;
    }
    return '';
}

function renderLexicalNode(node: LexicalNode, key: string): ReactNode {
    if (node.type === 'text') {
        let child: ReactNode = node.text ?? '';
        const format = typeof node.format === 'number' ? node.format : 0;
        if (format & 1) child = <strong>{child}</strong>;
        if (format & 2) child = <em>{child}</em>;
        if (format & 8) child = <u>{child}</u>;
        if (format & 16) child = <code>{child}</code>;
        return <span key={key}>{child}</span>;
    }
    if (node.type === 'linebreak') return <br key={key} />;
    if (node.type === 'markdown') return <MarkdownRenderer key={key} content={typeof node.data === 'string' ? node.data : textContent(node)} />;
    if (node.type === 'image') {
        const src = typeof node.src === 'string' ? node.src : typeof node.url === 'string' ? node.url : '';
        return src ? <Image key={key} src={src} alt={typeof node.altText === 'string' ? node.altText : ''} width={1200} height={675} unoptimized className="my-6 max-h-[36rem] h-auto w-auto max-w-full rounded-md" /> : null;
    }
    if (node.type === 'video') {
        const src = typeof node.src === 'string' ? node.src : typeof node.url === 'string' ? node.url : '';
        return src ? <video key={key} controls src={src} className="my-6 aspect-video w-full bg-black" /> : null;
    }
    const children = (node.children ?? []).map((child, index) => renderLexicalNode(child, `${key}-${index}`));
    switch (node.type) {
        case 'root': return <div key={key} className="space-y-4">{children}</div>;
        case 'heading': {
            const tag = ['h1', 'h2', 'h3', 'h4', 'h5', 'h6'].includes(node.tag ?? '') ? node.tag as 'h1' : 'h2';
            return createElement(tag, { key, className: 'mt-7 font-semibold text-foreground' }, children);
        }
        case 'paragraph': return <p key={key} className="leading-7 text-muted-foreground">{children}</p>;
        case 'quote': return <blockquote key={key} className="border-l-2 border-primary pl-4 italic text-muted-foreground">{children}</blockquote>;
        case 'list': return node.listType === 'number' ? <ol key={key} className="list-decimal space-y-2 pl-6">{children}</ol> : <ul key={key} className="list-disc space-y-2 pl-6">{children}</ul>;
        case 'listitem': return <li key={key}>{children}</li>;
        case 'link': return <a key={key} href={node.url ?? '#'} className="text-primary underline" rel="noreferrer">{children}</a>;
        case 'code': return <pre key={key} className="overflow-x-auto bg-black/40 p-4"><code>{children}</code></pre>;
        default: return children.length ? <div key={key}>{children}</div> : null;
    }
}

function LexicalRenderer({ content }: { content: unknown }) {
    const record = asRecord(content);
    const root = record?.root as LexicalNode | undefined;
    if (!root) return <p className="text-sm text-muted-foreground">This Lexical lesson has no published content.</p>;
    return <div className="learner-lexical-content">{renderLexicalNode(root, 'root')}</div>;
}

function RevealRenderer({ content }: { content: unknown }) {
    const slides = textContent(content).split(/^\s*---\s*$/m).map((slide) => slide.trim()).filter(Boolean);
    const [index, setIndex] = useState(0);
    if (!slides.length) return <p className="text-sm text-muted-foreground">This presentation has no published slides.</p>;
    return <section aria-label="Slide presentation" className="bg-muted/30 p-6 sm:p-10">
        <div className="min-h-72"><MarkdownRenderer content={slides[index]} /></div>
        <footer className="mt-6 flex items-center justify-between border-t pt-4">
            <Button variant="outline" size="icon" aria-label="Previous slide" disabled={index === 0} onClick={() => setIndex((value) => Math.max(0, value - 1))}><ChevronLeft /></Button>
            <span className="text-sm text-muted-foreground">{index + 1} / {slides.length}</span>
            <Button variant="outline" size="icon" aria-label="Next slide" disabled={index === slides.length - 1} onClick={() => setIndex((value) => Math.min(slides.length - 1, value + 1))}><ChevronRight /></Button>
        </footer>
    </section>;
}

function VideoRenderer({ courseId, enrollmentId, itemId, content }: { courseId: string; enrollmentId?: string; itemId: string; content: unknown }) {
    const record = asRecord(content);
    const src = typeof record?.videoUrl === 'string' ? record.videoUrl : typeof record?.url === 'string' ? record.url : typeof record?.src === 'string' ? record.src : textContent(content);
    const lastHeartbeat = useRef(0);
    const send = (type: 'Opened' | 'Progressed' | 'Paused' | 'Completed', video: HTMLVideoElement) => {
        if (!enrollmentId) return;
        void recordLessonEvent({ courseId, enrollmentId, contentId: itemId, type, positionSeconds: Math.round(video.currentTime), durationSeconds: Number.isFinite(video.duration) ? Math.round(video.duration) : undefined, progressPercentage: Number.isFinite(video.duration) && video.duration > 0 ? Math.round((video.currentTime / video.duration) * 100) : undefined, idempotencyKey: crypto.randomUUID() });
    };
    if (!src) return <p className="text-sm text-muted-foreground">This video lesson has no published media.</p>;
    return <video aria-label="Video lesson" controls preload="metadata" src={src} className="aspect-video w-full bg-black" onPlay={(event) => send('Opened', event.currentTarget)} onPause={(event) => send('Paused', event.currentTarget)} onEnded={(event) => send('Completed', event.currentTarget)} onTimeUpdate={(event) => { const second = Math.floor(event.currentTarget.currentTime); if (second - lastHeartbeat.current >= 15) { lastHeartbeat.current = second; send('Progressed', event.currentTarget); } }} />;
}

export function LearnerLessonRenderer({ courseId, enrollmentId, itemId, format, content }: { courseId: string; enrollmentId?: string; itemId: string; format?: LearningCoursesLessonContentFormat; content: unknown }) {
    switch (format ?? 'Markdown') {
        case 'Lexical': return <LexicalRenderer content={content} />;
        case 'RevealJs': return <RevealRenderer content={content} />;
        case 'Video': return <VideoRenderer courseId={courseId} enrollmentId={enrollmentId} itemId={itemId} content={content} />;
        case 'Markdown':
        default: {
            const markdown = textContent(content);
            return markdown ? <MarkdownRenderer content={markdown} /> : <p className="text-sm text-muted-foreground">This lesson has no published content.</p>;
        }
    }
}