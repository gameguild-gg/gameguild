'use client';

import MarkdownRenderer, { RendererType } from '@/components/markdown-renderer/markdown-renderer';
import { Card, CardContent } from '@/components/ui/card';
import { ProgramContentDto, ProgramContentType } from '@/lib/api/generated/types.gen';

interface CourseContentPageClientProps {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    programData: any;
    content: ProgramContentDto;
    contentPath: string[];
    basePath: string;
    children?: React.ReactNode;
}

/**
 * Determines the renderer type based on content type
 */
function getRendererType(contentType: ProgramContentType | undefined): RendererType {
    if (contentType === ProgramContentType.REVEAL) {
        return 'reveal';
    }
    if (contentType === ProgramContentType.MARP) {
        return 'marp';
    }
    if (contentType === 11) {
        return 'remark';
    }
    return 'markdown';
}

export function CourseContentPageClient({
    programData: _programData, // eslint-disable-line @typescript-eslint/no-unused-vars
    content,
    contentPath: _contentPath, // eslint-disable-line @typescript-eslint/no-unused-vars
    basePath: _basePath, // eslint-disable-line @typescript-eslint/no-unused-vars
    children
}: CourseContentPageClientProps) {

    const bodyContent = typeof content.body === 'string' ? content.body : '';
    const renderer = getRendererType(content.type);
    const isReveal = renderer === 'reveal';
    const isMarp = renderer === 'marp';
    const isRemark = renderer === 'remark';
    const isPresentationMode = isReveal || isMarp || isRemark;

    return (
        <div className={isPresentationMode ? "flex-1 flex flex-col h-full" : "flex-1 flex flex-col min-h-0"}>
            <div className={isPresentationMode ? "w-full h-full flex-1 flex flex-col" : "mx-auto max-w-4xl w-full"}>
                {/* Content */}
                <Card className={`transition-all duration-300 py-0 ${isPresentationMode ? 'border-0 shadow-none h-full flex-1 flex flex-col' : ''}`}>
                    <CardContent className={isPresentationMode ? "px-0 py-0 h-full flex-1 flex flex-col" : "px-6 py-6"}>
                        {/* Content Body */}
                        {content.body !== undefined && content.body !== null && (
                            <div className={isPresentationMode ? "h-full flex-1 flex flex-col" : "prose max-w-none"}>
                                {typeof content.body === 'string' ? (
                                    <MarkdownRenderer content={bodyContent} renderer={renderer} />
                                ) : (
                                    <pre className="whitespace-pre-wrap">
                                        {String(content.body)}
                                    </pre>
                                )}
                            </div>
                        )}

                        {/* Content Metadata - Hidden in presentation mode */}
                        {!isPresentationMode && (
                            <div className="flex items-center gap-4 text-sm text-muted-foreground border-t pt-4">
                                {content.estimatedMinutes != null && content.estimatedMinutes > 0 && (
                                    <span>Estimated time: {content.estimatedMinutes} minutes</span>
                                )}
                                {content.maxPoints != null && content.maxPoints > 0 && (
                                    <span>Points: {content.maxPoints}</span>
                                )}
                                {content.isRequired === true && (
                                    <span className="text-orange-600">Required</span>
                                )}
                            </div>
                        )}

                        {/* Children Content */}
                        {children != null && (
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