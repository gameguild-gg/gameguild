import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { auth } from '@/auth';
import React from 'react';
import { MentorProfileManager } from '@/components/mentorship/mentor-profile-manager';
import { MentorshipSessionsPanel } from '@/components/mentorship/mentorship-sessions-panel';
import {
    getMentorDashboardAction,
    listMyMentorshipSessionsAction,
} from '@/lib/mentorship/actions';

export const dynamic = 'force-dynamic';

function formatSessionDate(value: string): string {
    return new Intl.DateTimeFormat(undefined, {
        weekday: 'short',
        month: 'short',
        day: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
    }).format(new Date(value));
}

/**
 * Mentorship workspace (issue #102): the mentor dashboard — profile, availability,
 * upcoming sessions, and counts — plus the member's own sessions as advisee.
 */
export default async function WorkspaceMentorshipPage(): Promise<React.JSX.Element> {
    const session = await auth();
    if (!session || typeof session === 'function' || !session.user?.id) {
        return (
            <main className="mx-auto w-full max-w-5xl px-4 py-16 sm:px-6 lg:px-8">
                <h1 className="text-2xl font-semibold tracking-tight">Mentorship</h1>
                <p className="mt-2 text-sm text-muted-foreground">Sign in to manage your mentorship sessions.</p>
            </main>
        );
    }

    const [dashboard, sessions] = await Promise.all([
        getMentorDashboardAction(),
        listMyMentorshipSessionsAction(),
    ]);

    return (
        <main className="mx-auto w-full max-w-5xl space-y-8 px-4 py-10 sm:px-6 lg:px-8">
            <div className="space-y-2">
                <h1 className="text-3xl font-semibold tracking-tight">Mentorship</h1>
                <p className="text-sm leading-6 text-muted-foreground">
                    Mentor or advise people in the community (issue #102): publish availability, manage sessions, and
                    keep your profile current.
                </p>
            </div>

            <div className="grid gap-4 sm:grid-cols-4">
                <Card>
                    <CardHeader className="pb-2">
                        <CardDescription>Upcoming sessions</CardDescription>
                        <CardTitle className="text-3xl">{dashboard?.upcomingSessionCount ?? 0}</CardTitle>
                    </CardHeader>
                </Card>
                <Card>
                    <CardHeader className="pb-2">
                        <CardDescription>Open slots</CardDescription>
                        <CardTitle className="text-3xl">{dashboard?.openSlotCount ?? 0}</CardTitle>
                    </CardHeader>
                </Card>
                <Card>
                    <CardHeader className="pb-2">
                        <CardDescription>Completed</CardDescription>
                        <CardTitle className="text-3xl">{dashboard?.completedSessionCount ?? 0}</CardTitle>
                    </CardHeader>
                </Card>
                <Card>
                    <CardHeader className="pb-2">
                        <CardDescription>Total sessions</CardDescription>
                        <CardTitle className="text-3xl">{dashboard?.totalSessionCount ?? 0}</CardTitle>
                    </CardHeader>
                </Card>
            </div>

            {dashboard?.upcomingSessions?.length ? (
                <Card>
                    <CardHeader>
                        <CardTitle className="text-xl">Next up as mentor</CardTitle>
                        <CardDescription>Your upcoming booked sessions.</CardDescription>
                    </CardHeader>
                    <CardContent>
                        <ul className="space-y-2 text-sm">
                            {dashboard.upcomingSessions.map((session) => (
                                <li key={session.id} className="flex items-center justify-between gap-4">
                                    <span className="font-medium text-foreground">{session.topic}</span>
                                    <span className="text-muted-foreground">{formatSessionDate(session.startsAt)}</span>
                                </li>
                            ))}
                        </ul>
                    </CardContent>
                </Card>
            ) : null}

            <MentorProfileManager profile={dashboard?.profile ?? null} />

            <MentorshipSessionsPanel sessions={sessions} currentUserId={session.user.id} />
        </main>
    );
}
