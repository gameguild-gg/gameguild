'use client';

import MarkdownRenderer from '@/components/markdown-renderer/markdown-renderer';
import { Card, CardContent } from '@/components/ui/card';
import { ProgramContentDto } from '@/lib/api/generated/types.gen';
import { ChevronRight } from 'lucide-react';
import Link from 'next/link';

interface CourseContentPageClientProps {
    programData: any;
    content: ProgramContentDto;
    contentPath: string[];
    basePath: string;
    children?: React.ReactNode;
}

export function CourseContentPageClient({
    programData,
    content,
    contentPath,
    basePath,
    children
}: CourseContentPageClientProps) {

    return (
        <div className="flex-1 flex flex-col min-h-0">
            <div className="mx-auto max-w-4xl w-full">
                {/* Content */}
                <Card className="transition-all duration-300 py-0">
                    <CardContent className="px-6 py-6">
                        {/* Content Body */}
                        {content.body && (
                            <div className="prose max-w-none">
                                {typeof content.body === 'string' ? (
                                    <MarkdownRenderer content={content.body} />
                                ) : (
                                    <pre className="whitespace-pre-wrap">
                                        {String(content.body || '')}
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