'use client';

import { recordLessonEvent } from '@/lib/learner/lesson-interaction-actions';
import type { LearningCoursesLessonContentFormat } from '@game-guild/client';
import { MarkdownRenderer } from '@game-guild/content-rendering';
import { Button } from '@game-guild/ui/components/button';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { lazy, Suspense, useRef, useState } from 'react';

type ContentRecord = Record<string, unknown>;

const LexicalLessonRenderer = lazy(async () => {
    const mod = await import('./lexical-lesson-renderer');
    return { default: mod.LexicalLessonRenderer };
});

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
        case 'Lexical': return <Suspense fallback={<div className="min-h-32 animate-pulse rounded-md bg-muted" />}><LexicalLessonRenderer content={content} itemId={itemId} /></Suspense>;
        case 'RevealJs': return <RevealRenderer content={content} />;
        case 'Video': return <VideoRenderer courseId={courseId} enrollmentId={enrollmentId} itemId={itemId} content={content} />;
        case 'Markdown':
        default: {
            const markdown = textContent(content);
            return markdown ? <MarkdownRenderer content={markdown} /> : <p className="text-sm text-muted-foreground">This lesson has no published content.</p>;
        }
    }
}
