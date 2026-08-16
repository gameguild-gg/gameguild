import { redirect } from 'next/navigation';

export default async function TestingEventPage({
  params,
}: {
  params: Promise<{ eventId: string }>;
}) {
  const { eventId } = await params;
  redirect(`/console/community/testing-lab/events/${eventId}/overview`);
}
