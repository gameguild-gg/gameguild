import { auth } from '@/auth';
import { StudentShell } from '@/components/student-shell';
import type { ReactNode } from 'react';

export default async function CoursesLayout({ children }: { children: ReactNode }) {
    const session = await auth();
    if (!session?.user) return children;
    const name = session.user.name?.trim() || session.user.email?.split('@')[0] || 'Learner';
    return <StudentShell user={{ id: session.user.id, name, email: session.user.email || '', image: session.user.image }}>{children}</StudentShell>;
}