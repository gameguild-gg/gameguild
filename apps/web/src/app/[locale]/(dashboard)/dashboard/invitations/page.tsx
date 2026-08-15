import { redirect } from 'next/navigation';

/** Legacy personal route; Next config redirects before the dashboard shell. */
export default function LegacyInvitationsPage(): never { redirect('/invitations'); }
