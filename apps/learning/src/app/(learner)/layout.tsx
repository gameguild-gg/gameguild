import { auth } from '@/auth';
import { StudentShell } from '@/components/student-shell';
import { redirect } from 'next/navigation';
import type { ReactNode } from 'react';

export default async function LearnerLayout({ children }: { children: ReactNode }) {
    const session = await auth();
    if (!session?.user) redirect('/sign-in');
    const name = session.user.name?.trim() || session.user.email?.split('@')[0] || 'Learner';
    return <StudentShell user={{ id: session.user.id, name, email: session.user.email || '', image: session.user.image }}>{children}</StudentShell>;
}