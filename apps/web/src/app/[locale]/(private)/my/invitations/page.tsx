import { auth } from '@/auth';
import { getPendingMemberInvitations } from '@/lib/community';
import { acceptCurrentUserInvite } from '@/lib/community/actions/member-access';
import { acceptTeamInvitationForm } from '@/lib/workspace-actions';
import { getWorkspaceMyTeamInvitations } from '@/lib/workspaces';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Building2, Check, MailCheck } from 'lucide-react';
import React from 'react';

interface Props {
  searchParams?: Promise<{ message?: string; error?: string }>;
}

export default async function InvitationsPage({ searchParams }: Props): Promise<React.JSX.Element> {
  const [session, query, teamInvitations] = await Promise.all([auth(), searchParams, getWorkspaceMyTeamInvitations()]);
  const result = await getPendingMemberInvitations(session?.user.id ?? '');
  const warning = query?.error ?? result.error;

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-start gap-3">
        <div className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-primary text-primary-foreground">
          <MailCheck className="size-5" />
        </div>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Invitations</h1>
          <p className="text-muted-foreground">Review workspace access offered to your GameGuild account.</p>
        </div>
      </div>

      {query?.message ? (
        <Alert>
          <Check className="size-4" />
          <AlertTitle>Invitation updated</AlertTitle>
          <AlertDescription>{query.message}</AlertDescription>
        </Alert>
      ) : null}

      {warning ? (
        <Alert variant="destructive">
          <MailCheck className="size-4" />
          <AlertTitle>Invitations could not be loaded</AlertTitle>
          <AlertDescription>{warning}</AlertDescription>
        </Alert>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>Pending workspace invitations</CardTitle>
          <CardDescription>Access remains inactive until you accept it.</CardDescription>
        </CardHeader>
        <CardContent>
          {result.invitations.length === 0 ? (
            <div className="flex min-h-56 flex-col items-center justify-center gap-3 text-center">
              <MailCheck className="size-10 text-muted-foreground" />
              <div>
                <p className="font-medium">No pending invitations</p>
                <p className="text-sm text-muted-foreground">New workspace invitations will appear here.</p>
              </div>
            </div>
          ) : (
            <div className="divide-y rounded-lg border">
              {result.invitations.map((invitation) => (
                <div key={`${invitation.tenantId}-${session?.user.id}`} className="flex flex-col gap-4 p-4 md:flex-row md:items-center md:justify-between">
                  <div className="flex items-start gap-3">
                    <div className="flex size-9 shrink-0 items-center justify-center rounded-md bg-muted">
                      <Building2 className="size-4" />
                    </div>
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <p className="font-medium">{invitation.tenantName || invitation.tenantSlug || 'GameGuild workspace'}</p>
                        <Badge variant="secondary">{invitation.role || 'Member'}</Badge>
                      </div>
                      <p className="mt-1 text-sm text-muted-foreground">
                        Invited by {invitation.invitedByEmail || 'a workspace administrator'}
                      </p>
                    </div>
                  </div>
                  <form action={acceptCurrentUserInvite}>
                    <input type="hidden" name="userId" value={session?.user.id ?? ''} />
                    <input type="hidden" name="tenantId" value={invitation.tenantId} />
                    <Button type="submit">
                      <Check className="mr-2 size-4" />
                      Accept invitation
                    </Button>
                  </form>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Pending Team invitations</CardTitle>
          <CardDescription>Accepting joins the Team; it does not grant event-management capabilities.</CardDescription>
        </CardHeader>
        <CardContent>
          {teamInvitations.length === 0 ? (
            <div className="py-10 text-center text-sm text-muted-foreground">No pending Team invitations.</div>
          ) : (
            <div className="divide-y rounded-lg border">
              {teamInvitations.map((invitation) => (
                <div key={invitation.id} className="flex flex-col gap-4 p-4 md:flex-row md:items-center md:justify-between">
                  <div>
                    <div className="flex flex-wrap items-center gap-2"><p className="font-medium">{invitation.teamName}</p><Badge variant="secondary">{String(invitation.authority)}</Badge></div>
                    <p className="mt-1 text-sm text-muted-foreground">Expires {new Date(invitation.expiresAt).toLocaleString()}</p>
                  </div>
                  <form action={acceptTeamInvitationForm}>
                    <input type="hidden" name="invitationId" value={invitation.id} />
                    <Button type="submit"><Check className="mr-2 size-4" />Accept Team invitation</Button>
                  </form>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
