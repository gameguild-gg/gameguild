/**
 * Stub for common report button component.
 */

'use client';

import { Button } from '@/components/ui/button';
import { Flag } from 'lucide-react';

export interface ReportButtonProps {
    contentType: string;
    contentId: string;
    variant?: 'default' | 'ghost' | 'outline';
    size?: 'default' | 'sm' | 'lg' | 'icon';
    className?: string;
}

export function ReportButton({
    contentType,
    contentId,
    variant = 'ghost',
    size = 'icon',
    className
}: ReportButtonProps) {
    const handleReport = () => {
        console.log('[STUB] Report button clicked:', { contentType, contentId });
    };

    return (
        <Button
            variant={variant}
            size={size}
            className={className}
            onClick={handleReport}
            title="Report content"
        >
            <Flag className="h-4 w-4" />
        </Button>
    );
}

export default ReportButton;
