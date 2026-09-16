'use client';

import { AssetImage } from '@/components/block-content-editor/extras/media/asset-image';
import { getLearningAssetRepository } from '@/lib/learning/assets/learning-asset-repository';
import { recordLessonEvent } from '@/lib/learner/lesson-interaction-actions';
import { AssetsProvider, useResolvedAssetUrl } from '@game-guild/assets/react';
import type { LearningCoursesLessonContentFormat } from '@game-guild/client';
import { MarkdownRenderer } from '@game-guild/content-rendering';
import { Button } from '@game-guild/ui/components/button';
import { ChevronLeft, ChevronRight, ExternalLink } from 'lucide-react';
import { lazy, Suspense, useRef, useState } from 'react';
import { defaultUrlTransform, type Components } from 'react-markdown';

type ContentRecord = Record<string, unknown>;

export function learningImageSource(source: string | Blob | undefined): string | undefined {
    return typeof source === 'string' ? source : undefined;
}

const ASSET_MARKDOWN_COMPONENTS: Components = {
    img: ({ src, alt, ...props }) => <AssetImage src={learningImageSource(src)} alt={alt} {...props} />,
};

export function learningUrlTransform(url: string): string {
    return url.startsWith('asset://') ? url : defaultUrlTransform(url);
}

function LessonMarkdown({ content }: { content: string }) {
    return <MarkdownRenderer content={content} components={ASSET_MARKDOWN_COMPONENTS} urlTransform={learningUrlTransform} />;
}

const LexicalLessonRenderer = lazy(async () => {
    const mod = await import('./lexical-lesson-renderer');
    return { default: mod.LexicalLessonRenderer };
});

export function asRecord(content: unknown): ContentRecord | null {
    if (content && typeof content === 'object' && !Array.isArray(content)) return content as ContentRecord;
    if (typeof content === 'string') {
        try { const parsed = JSON.parse(content) as unknown; return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed as ContentRecord : null; } catch { return null; }
    }
    return null;
}

export function textContent(content: unknown): string {
    if (typeof content === 'string') return content;
    const record = asRecord(content);
    if (!record) return '';
    for (const key of ['markdown', 'content', 'text', 'source']) {
        if (typeof record[key] === 'string') return record[key] as string;
    }
    return '';
}

export function videoSource(content: unknown): string {
    const record = asRecord(content);
    return typeof record?.videoUrl === 'string'
        ? record.videoUrl
        : typeof record?.url === 'string'
          ? record.url
          : typeof record?.src === 'string'
            ? record.src
            : textContent(content);
}

export function externalLinkSource(content: unknown): string {
    const record = asRecord(content);
    const candidate = typeof record?.url === 'string'
        ? record.url
        : typeof record?.href === 'string'
          ? record.href
          : typeof record?.src === 'string'
            ? record.src
            : textContent(content);
    try {
        const url = new URL(candidate);
        return url.protocol === 'http:' || url.protocol === 'https:' ? url.href : '';
    } catch {
        return '';
    }
}

function RevealRenderer({ content }: { content: unknown }) {
    const slides = textContent(content).split(/^\s*---\s*$/m).map((slide) => slide.trim()).filter(Boolean);
    const [index, setIndex] = useState(0);
    if (!slides.length) return <p className="text-sm text-muted-foreground">This presentation has no published slides.</p>;
    return <section aria-label="Slide presentation" className="bg-muted/30 p-6 sm:p-10">
        <div className="min-h-72"><LessonMarkdown content={slides[index]} /></div>
        <footer className="mt-6 flex items-center justify-between border-t pt-4">
            <Button variant="outline" size="icon" aria-label="Previous slide" disabled={index === 0} onClick={() => setIndex((value) => Math.max(0, value - 1))}><ChevronLeft /></Button>
            <span className="text-sm text-muted-foreground">{index + 1} / {slides.length}</span>
            <Button variant="outline" size="icon" aria-label="Next slide" disabled={index === slides.length - 1} onClick={() => setIndex((value) => Math.min(slides.length - 1, value + 1))}><ChevronRight /></Button>
        </footer>
    </section>;
}

function VideoRenderer({ courseId, enrollmentId, itemId, content }: { courseId: string; enrollmentId?: string; itemId: string; content: unknown }) {
    const src = videoSource(content);
    const { url: resolvedSrc, loading } = useResolvedAssetUrl(src);
    const lastHeartbeat = useRef(0);
    const send = (type: 'Opened' | 'Progressed' | 'Paused' | 'Completed', video: HTMLVideoElement) => {
        if (!enrollmentId) return;
        void recordLessonEvent({ courseId, enrollmentId, contentId: itemId, type, positionSeconds: Math.round(video.currentTime), durationSeconds: Number.isFinite(video.duration) ? Math.round(video.duration) : undefined, progressPercentage: Number.isFinite(video.duration) && video.duration > 0 ? Math.round((video.currentTime / video.duration) * 100) : undefined, idempotencyKey: crypto.randomUUID() });
    };
    if (!src) return <p className="text-sm text-muted-foreground">This video lesson has no published media.</p>;
    if (loading) return <div className="aspect-video w-full animate-pulse bg-muted" aria-label="Loading video" />;
    if (!resolvedSrc) return <p className="text-sm text-destructive">This lesson media is unavailable.</p>;
    return <video aria-label="Video lesson" controls preload="metadata" src={resolvedSrc} className="aspect-video w-full bg-black" onPlay={(event) => send('Opened', event.currentTarget)} onPause={(event) => send('Paused', event.currentTarget)} onEnded={(event) => send('Completed', event.currentTarget)} onTimeUpdate={(event) => { const second = Math.floor(event.currentTarget.currentTime); if (second - lastHeartbeat.current >= 15) { lastHeartbeat.current = second; send('Progressed', event.currentTarget); } }} />;
}

function HtmlRenderer({ content }: { content: unknown }) {
    const html = textContent(content);
    if (!html) return <p className="text-sm text-muted-foreground">This HTML lesson has no published content.</p>;
    return <iframe
        title="HTML lesson"
        sandbox=""
        referrerPolicy="no-referrer"
        srcDoc={html}
        className="min-h-[32rem] w-full border-0 bg-white"
    />;
}

function ExternalLinkRenderer({ content }: { content: unknown }) {
    const href = externalLinkSource(content);
    if (!href) return <p className="text-sm text-muted-foreground">This lesson resource link is unavailable.</p>;
    return <Button asChild>
        <a href={href} target="_blank" rel="noopener noreferrer">
            Open lesson resource
            <ExternalLink className="ml-2 h-4 w-4" />
        </a>
    </Button>;
}

export function LearnerLessonRenderer({ courseId, enrollmentId, itemId, format, content }: { courseId: string; enrollmentId?: string; itemId: string; format?: LearningCoursesLessonContentFormat; content: unknown }) {
    const assetRepository = getLearningAssetRepository();
    return <AssetsProvider repository={assetRepository} scope={{ type: 'ProgramContent', id: itemId }}>
        <LearnerLessonContent courseId={courseId} enrollmentId={enrollmentId} itemId={itemId} format={format} content={content} />
    </AssetsProvider>;
}

function LearnerLessonContent({ courseId, enrollmentId, itemId, format, content }: { courseId: string; enrollmentId?: string; itemId: string; format?: LearningCoursesLessonContentFormat; content: unknown }) {
    switch (format ?? 'Markdown') {
        case 'Lexical': return <Suspense fallback={<div className="min-h-32 animate-pulse rounded-md bg-muted" />}><LexicalLessonRenderer content={content} itemId={itemId} /></Suspense>;
        case 'RevealJs': return <RevealRenderer content={content} />;
        case 'Video': return <VideoRenderer courseId={courseId} enrollmentId={enrollmentId} itemId={itemId} content={content} />;
        case 'Html': return <HtmlRenderer content={content} />;
        case 'ExternalLink': return <ExternalLinkRenderer content={content} />;
        case 'Markdown':
        default: {
            const markdown = textContent(content);
            return markdown ? <LessonMarkdown content={markdown} /> : <p className="text-sm text-muted-foreground">This lesson has no published content.</p>;
        }
    }
}
