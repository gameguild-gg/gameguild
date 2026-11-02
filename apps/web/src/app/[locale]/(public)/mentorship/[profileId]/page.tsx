import { Link } from '@/i18n/navigation';
import { notFound } from 'next/navigation';
import { ArrowLeft, CalendarClock, Globe2, Sparkles } from 'lucide-react';
import React from 'react';
import { MentorBookingForm } from '@/components/mentorship/mentor-booking-form';
import { auth } from '@/auth';
import { getMentor, listMentorAvailability } from '@/lib/mentorship/actions';

export const dynamic = 'force-dynamic';

const roleLabels: Record<number, string> = {
  0: 'Mentor',
  1: 'Advisor',
  2: 'Matchmaker',
};

/**
 * Public mentor profile (issue #102): bio, skills, pricing, and the booking form
 * for the advisee to schedule an introductory meeting.
 */
export default async function MentorProfilePage({
    params,
}: {
    params: Promise<{ profileId: string }>;
}): Promise<React.JSX.Element> {
    const { profileId } = await params;
    const mentor = await getMentor(profileId);
    if (!mentor) {
        notFound();
    }

    const [slots, session] = await Promise.all([
        listMentorAvailability(profileId),
        auth().catch(() => null),
    ]);
    const signedIn = Boolean(session && typeof session !== 'function' && session.user?.id);

    return (
        <main className="mx-auto w-full max-w-6xl px-4 py-16 sm:px-6 lg:px-8">
            <Link
                href="/mentorship"
                className="inline-flex items-center text-sm font-medium text-muted-foreground transition hover:text-foreground"
            >
                <ArrowLeft className="mr-2 size-4" aria-hidden="true" />
                Back to the mentorship directory
            </Link>

            <div className="mt-8 grid gap-10 lg:grid-cols-[1.2fr_0.8fr]">
                <section className="space-y-6">
                    <div className="flex flex-wrap gap-2">
                        <span className="rounded-full bg-sky-500/10 px-3 py-1 text-xs font-semibold text-sky-600 dark:text-sky-300">
                            {roleLabels[mentor.role] ?? 'Mentor'}
                        </span>
                        {mentor.offersIntroductoryCall ? (
                            <span className="inline-flex items-center gap-1 rounded-full bg-emerald-500/10 px-3 py-1 text-xs font-semibold text-emerald-600 dark:text-emerald-300">
                                <Sparkles className="size-3" aria-hidden="true" />
                                Free {mentor.introductoryCallMinutes}-minute intro call
                            </span>
                        ) : null}
                        {mentor.bookingMode === 1 ? (
                            <span className="inline-flex items-center gap-1 rounded-full bg-muted px-3 py-1 text-xs font-semibold text-muted-foreground">
                                <CalendarClock className="size-3" aria-hidden="true" />
                                Schedule managed by a matchmaker
                            </span>
                        ) : null}
                        <span className="inline-flex items-center gap-1 rounded-full bg-muted px-3 py-1 text-xs font-semibold text-muted-foreground">
                            <Globe2 className="size-3" aria-hidden="true" />
                            {mentor.timezoneId}
                        </span>
                    </div>

                    <h1 className="text-4xl font-semibold tracking-tight text-foreground">{mentor.displayName}</h1>
                    {mentor.headline ? (
                        <p className="text-lg font-medium text-muted-foreground">{mentor.headline}</p>
                    ) : null}
                    {mentor.bio ? <p className="whitespace-pre-line text-base leading-7 text-muted-foreground">{mentor.bio}</p> : null}

                    <div className="space-y-2">
                        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Skills</h2>
                        {mentor.skills.length > 0 ? (
                            <ul className="flex flex-wrap gap-1.5">
                                {mentor.skills.map((skill) => (
                                    <li key={skill.id} className="rounded-full border px-2.5 py-0.5 text-sm text-foreground">
                                        {skill.skill}
                                    </li>
                                ))}
                            </ul>
                        ) : (
                            <p className="text-sm text-muted-foreground">No skills listed yet.</p>
                        )}
                    </div>

                    <div className="space-y-2">
                        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Program</h2>
                        <p className="text-sm leading-6 text-muted-foreground">
                            {mentor.pricingModel === 0 ? 'Free sessions' : 'Paid sessions'}
                            {mentor.pricingModel === 0 && mentor.freeContributionReason
                                ? ` — ${mentor.freeContributionReason}`
                                : ''}
                            .
                        </p>
                    </div>
                </section>

                <aside>
                    <MentorBookingForm mentorName={mentor.displayName} slots={slots} signedIn={signedIn} />
                </aside>
            </div>
        </main>
    );
}
