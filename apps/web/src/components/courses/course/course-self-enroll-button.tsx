'use client';

import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { enrollInFreeCourse } from '@/lib/courses/actions/enrollment.actions';
import { Loader2 } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';

interface CourseSelfEnrollButtonProps {
    readonly courseSlug: string;
    readonly className?: string;
    readonly buttonClassName?: string;
}

export function CourseSelfEnrollButton({ courseSlug, className, buttonClassName }: CourseSelfEnrollButtonProps) {
    const router = useRouter();
    const [isPending, startTransition] = useTransition();
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    const handleEnroll = () => {
        startTransition(async () => {
            setError(null);
            setSuccess(null);

            const result = await enrollInFreeCourse(courseSlug);

            if (!result.success) {
                setError(result.message);
                return;
            }

            setSuccess(result.message);
            router.refresh();
        });
    };

    return (
        <div className={cn('space-y-2', className)}>
            <Button onClick={handleEnroll} disabled={isPending} className={cn('bg-blue-600 text-white hover:bg-blue-500', buttonClassName)}>
                {isPending ? (
                    <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Enrolling...
                    </>
                ) : (
                    'Enroll now'
                )}
            </Button>

            {success ? <p className="text-xs text-emerald-300">{success}</p> : null}
            {error ? <p className="text-xs text-amber-300">{error}</p> : null}
        </div>
    );
}
