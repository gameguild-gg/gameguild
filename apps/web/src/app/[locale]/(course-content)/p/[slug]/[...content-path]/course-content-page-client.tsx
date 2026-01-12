'use client';

import MarkdownRenderer, { RendererType } from '@/components/markdown-renderer/markdown-renderer';
import { Card, CardContent } from '@/components/ui/card';
import { ProgramContentDto } from '@/lib/api/generated/types.gen';

interface CourseContentPageClientProps {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    programData: any;
    content: ProgramContentDto;
    contentPath: string[];
    basePath: string;
    children?: React.ReactNode;
}

/**
 * Detects if content has frontmatter specifying renderer type
 * Frontmatter format: ---\nrenderer: reveal\n---
 */
function detectRendererType(content: string): { renderer: RendererType; cleanContent: string } {
    const frontmatterRegex = /^---\s*\n([\s\S]*?)\n---\s*\n/;
    const match = content.match(frontmatterRegex);

    if (match && match[1] != null) {
        const frontmatter = match[1];
        const rendererMatch = frontmatter.match(/renderer:\s*(reveal|markdown)/i);
        if (rendererMatch && rendererMatch[1]?.toLowerCase() === 'reveal') {
            // Remove frontmatter from content for reveal
            const cleanContent = content.replace(frontmatterRegex, '');
            return { renderer: 'reveal', cleanContent };
        }
    }

    return { renderer: 'markdown', cleanContent: content };
}

export function CourseContentPageClient({
    programData: _programData, // eslint-disable-line @typescript-eslint/no-unused-vars
    content,
    contentPath: _contentPath, // eslint-disable-line @typescript-eslint/no-unused-vars
    basePath: _basePath, // eslint-disable-line @typescript-eslint/no-unused-vars
    children
}: CourseContentPageClientProps) {

    const bodyContent = typeof content.body === 'string' ? content.body : '';
    const { renderer, cleanContent } = detectRendererType(bodyContent);
    const isReveal = renderer === 'reveal';

    return (
        <div className="flex-1 flex flex-col min-h-0">
            <div className={isReveal ? "w-full" : "mx-auto max-w-4xl w-full"}>
                {/* Content */}
                <Card className={`transition-all duration-300 py-0 ${isReveal ? 'border-0 shadow-none' : ''}`}>
                    <CardContent className={isReveal ? "px-0 py-0" : "px-6 py-6"}>
                        {/* Content Body */}
                        {content.body !== undefined && content.body !== null && (
                            <div className={isReveal ? "" : "prose max-w-none"}>
                                {typeof content.body === 'string' ? (
                                    <MarkdownRenderer content={cleanContent} renderer={renderer} />
                                ) : (
                                    <pre className="whitespace-pre-wrap">
                                        {String(content.body)}
                                    </pre>
                                )}
                            </div>
                        )}

                        {/* Content Metadata */}
                        <div className="flex items-center gap-4 text-sm text-muted-foreground border-t pt-4">
                            {content.estimatedMinutes && (
                                <span>Estimated time: {content.estimatedMinutes} minutes</span>
                            )}
                            {content.maxPoints && (
                                <span>Points: {content.maxPoints}</span>
                            )}
                            {content.isRequired && (
                                <span className="text-orange-600">Required</span>
                            )}
                        </div>

                        {/* Children Content */}
                        {children && (
                            <div className="space-y-4">
                                {children}
                            </div>
                        )}
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}