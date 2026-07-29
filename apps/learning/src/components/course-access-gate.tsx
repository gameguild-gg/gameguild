'use client';

import type { CourseAccessState } from '@/lib/courses';
import { enrollInCourse } from '@/lib/learner-actions';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader } from '@game-guild/ui/components/card';
import { AlertCircle, ArrowRight, CheckCircle2, LockKeyhole } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';

type GateAccess = Exclude<CourseAccessState, { kind: 'ready' } | { kind: 'not-found' }>;

function formatPrice(price: number | null, currency: string) {
    if (price === null) return 'See pricing';
    return new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(price);
}

export function CourseAccessGate({ access }: { access: GateAccess }) {
    const router = useRouter();
    const [isPending, startTransition] = useTransition();
    const [message, setMessage] = useState<string | null>(null);
    const course = access.course;
    const storefrontUrl = `${process.env.NEXT_PUBLIC_WEB_URL || 'http://localhost:3000'}/courses/${course?.slug ?? ''}`;

    const enroll = () => {
        if (!course) return;
        setMessage(null);
        startTransition(async () => {
            const result = await enrollInCourse(course.id);
            if (!result.success) {
                setMessage(result.error);
                return;
            }
            setMessage('Enrollment confirmed');
            router.refresh();
        });
    };

    const title = access.kind === 'enrollment-required'
        ? 'Join this course'
        : access.kind === 'payment-required'
          ? 'Purchase required'
          : access.kind === 'enrollment-closed'
            ? 'Enrollment is closed'
            : 'Classroom unavailable';

    return (
        <main className="mx-auto flex min-h-[70vh] max-w-3xl items-center px-4 py-12">
            <Card className="w-full border-slate-800 bg-slate-900/90 text-slate-100 shadow-2xl shadow-slate-950/40">
                <CardHeader className="gap-3">
                    <div className="flex size-11 items-center justify-center rounded-lg bg-sky-500/10 text-sky-300">
                        {access.kind === 'enrollment-required' ? <CheckCircle2 className="size-5" /> : <LockKeyhole className="size-5" />}
                    </div>
                    <h1 className="text-2xl font-semibold">{title}</h1>
                    <p className="text-sm text-slate-300">
                        {course?.title ?? (access.kind === 'unavailable' ? access.message : 'This course')}
                    </p>
                </CardHeader>
                <CardContent className="space-y-4">
                    {access.kind === 'payment-required' ? (
                        <p className="text-3xl font-semibold">{formatPrice(access.price, access.currency)}</p>
                    ) : null}
                    {access.kind === 'enrollment-closed' ? (
                        <p className="text-sm text-slate-400">This cohort is not accepting new learners. Check the catalog for the next offering.</p>
                    ) : null}
                    {access.kind === 'unavailable' ? (
                        <div className="flex gap-2 text-sm text-amber-300"><AlertCircle className="size-4 shrink-0" />{access.message}</div>
                    ) : null}
                    {message ? <p role="status" className={message === 'Enrollment confirmed' ? 'text-emerald-300' : 'text-red-300'}>{message}</p> : null}
                    {access.kind === 'enrollment-required' ? (
                        <Button onClick={enroll} disabled={isPending}>{isPending ? 'Enrolling...' : 'Enroll for free'}</Button>
                    ) : null}
                    {access.kind === 'payment-required' ? (
                        <Button asChild><Link href={storefrontUrl}>Continue to checkout<ArrowRight className="ml-2 size-4" /></Link></Button>
                    ) : null}
                    <Button asChild variant="outline"><Link href="/catalog">Browse catalog</Link></Button>
                </CardContent>
            </Card>
        </main>
    );
}