'use client';

import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { enrollInFreeCourse } from '@/lib/courses/actions/enrollment.actions';
import { getLearningAppCourseContentUrl } from '@/lib/learning-app';
import { Loader2 } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState } from 'react';

interface CourseSelfEnrollButtonProps {
    readonly courseSlug: string;
    readonly className?: string;
    readonly buttonClassName?: string;
}

export function CourseSelfEnrollButton({ courseSlug, className, buttonClassName }: CourseSelfEnrollButtonProps) {
    const router = useRouter();
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    const handleEnroll = async () => {
        if (isSubmitting) return;

        setIsSubmitting(true);
        setError(null);
        setSuccess(null);

        try {
            const result = await enrollInFreeCourse(courseSlug);

            if (!result.success) {
                setError(result.message);
                return;
            }

            setSuccess(result.message);
            router.push(result.learningUrl ?? getLearningAppCourseContentUrl(courseSlug));
        } catch (error) {
            setError(error instanceof Error ? error.message : 'Could not complete enrollment.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className={cn('space-y-2', className)}>
            <Button onClick={() => void handleEnroll()} disabled={isSubmitting} className={cn('bg-blue-600 text-white hover:bg-blue-500', buttonClassName)}>
                {isSubmitting ? (
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
