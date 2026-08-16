import { redirect } from 'next/navigation';

export default async function TestingEventPage({
  params,
}: {
  params: Promise<{ eventId: string }>;
}) {
  const { eventId } = await params;
  redirect(`/dashboard/community/testing-lab/events/${eventId}/overview`);
}
