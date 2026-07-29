'use client';

import {
  assignTestingLabRole,
  grantTestingLabResourcePermission,
  inspectTestingLabUserAccess,
  revokeTestingLabResourcePermission,
  revokeTestingLabRole,
  type TestingLabActionResult,
} from '@/lib/testing-lab/actions';
import type { TestingLabTestingLabRoleTemplate, TestingLabUserTestingLabPermissions } from '@game-guild/client';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { AlertCircle, CheckCircle2, Loader2, Search, ShieldCheck, ShieldMinus, ShieldPlus } from 'lucide-react';
import { useMemo, useState, useTransition } from 'react';

interface MemberOption {
  id: string;
  label: string;
}

interface ResourceOption {
  id: string;
  label: string;
  type: 'TestingRequest' | 'TestingSession' | 'TestingLocation';
}

type Operation = (formData: FormData) => Promise<TestingLabActionResult<unknown>>;

function ResultAlert({ result }: { result: TestingLabActionResult<unknown> | null }) {
  if (!result) return null;
  return (
    <Alert variant={result.success ? 'default' : 'destructive'} aria-live="polite">
      {result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}
      <AlertDescription>{result.success ? result.message : result.error}</AlertDescription>
    </Alert>
  );
}

export function TestingLabAccessManagement({
  members,
  roles,
  resources,
}: {
  members: MemberOption[];
  roles: TestingLabTestingLabRoleTemplate[];
  resources: ResourceOption[];
}) {
  const [memberId, setMemberId] = useState('');
  const [roleName, setRoleName] = useState('');
  const [resourceType, setResourceType] = useState<ResourceOption['type']>('TestingRequest');
  const [resourceId, setResourceId] = useState('');
  const [permissionAction, setPermissionAction] = useState('read');
  const [expiresAt, setExpiresAt] = useState('');
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingLabActionResult<unknown> | null>(null);
  const [effectiveAccess, setEffectiveAccess] = useState<TestingLabUserTestingLabPermissions | null>(null);

  const filteredResources = useMemo(() => resources.filter((resource) => resource.type === resourceType), [resourceType, resources]);

  function execute(operation: Operation, values: Record<string, string>) {
    const formData = new FormData();
    Object.entries(values).forEach(([key, value]) => formData.set(key, value));
    startTransition(async () => {
      const next = await operation(formData);
      setResult(next);
      if (next.success && operation === inspectTestingLabUserAccess) {
        setEffectiveAccess((next.data ?? null) as TestingLabUserTestingLabPermissions | null);
      }
    });
  }

  const memberRequired = memberId.length === 0;
  const roleDisabled = memberRequired || roleName.length === 0;
  const resourceDisabled = memberRequired || resourceId.length === 0 || permissionAction.length === 0;

  return (
    <div className="space-y-5">
      <div className="space-y-2">
        <Label htmlFor="access-member">Member</Label>
        <Select
          value={memberId}
          onValueChange={(value) => {
            setMemberId(value);
            setEffectiveAccess(null);
            setResult(null);
          }}
        >
          <SelectTrigger id="access-member">
            <SelectValue placeholder="Choose a member" />
          </SelectTrigger>
          <SelectContent>
            {members.map((member) => (
              <SelectItem key={member.id} value={member.id}>
                {member.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <section className="space-y-3 rounded-md border p-4">
        <div>
          <h3 className="font-medium">Effective access</h3>
          <p className="text-xs text-muted-foreground">Inspect roles and merged permissions currently effective for this member.</p>
        </div>
        <Button type="button" variant="outline" disabled={memberRequired || pending} onClick={() => execute(inspectTestingLabUserAccess, { userId: memberId })}>
          {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Search className="mr-2 size-4" />}
          Inspect access
        </Button>
        {effectiveAccess ? (
          <div className="space-y-3 rounded-md bg-muted/30 p-3 text-sm">
            <div className="flex flex-wrap gap-1.5">
              {(effectiveAccess.assignedRoles ?? []).length > 0 ? (
                effectiveAccess.assignedRoles?.map((role) => <Badge key={role}>{role}</Badge>)
              ) : (
                <span className="text-muted-foreground">No Testing Lab roles assigned.</span>
              )}
            </div>
            <div className="flex flex-wrap gap-1.5">
              {Object.entries(effectiveAccess.permissions ?? {})
                .filter(([, enabled]) => enabled)
                .map(([permission]) => (
                  <Badge key={permission} variant="outline">
                    {permission
                      .replace(/^can/, '')
                      .replace(/([A-Z])/g, ' $1')
                      .trim()}
                  </Badge>
                ))}
            </div>
          </div>
        ) : null}
      </section>

      <section className="space-y-3 rounded-md border p-4">
        <div>
          <h3 className="font-medium">Role assignment</h3>
          <p className="text-xs text-muted-foreground">Apply or remove a reusable role template in the current tenant.</p>
        </div>
        <Select value={roleName} onValueChange={setRoleName}>
          <SelectTrigger aria-label="Testing Lab role">
            <SelectValue placeholder="Choose a role" />
          </SelectTrigger>
          <SelectContent>
            {roles.map((role) => (
              <SelectItem key={role.id ?? role.name} value={role.name ?? ''}>
                {role.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <div className="flex gap-2">
          <Button type="button" disabled={roleDisabled || pending} onClick={() => execute(assignTestingLabRole, { userId: memberId, roleName })}>
            <ShieldPlus className="mr-2 size-4" /> Assign
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={roleDisabled || pending}
            onClick={() => execute(revokeTestingLabRole, { userId: memberId, roleName })}
          >
            <ShieldMinus className="mr-2 size-4" /> Revoke
          </Button>
        </div>
      </section>

      <section className="space-y-3 rounded-md border p-4">
        <div>
          <h3 className="font-medium">Resource exception</h3>
          <p className="text-xs text-muted-foreground">Grant or revoke one explicit action on a specific request, session, or location.</p>
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="space-y-2">
            <Label>Resource type</Label>
            <Select
              value={resourceType}
              onValueChange={(value) => {
                setResourceType(value as ResourceOption['type']);
                setResourceId('');
              }}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="TestingRequest">Request</SelectItem>
                <SelectItem value="TestingSession">Session</SelectItem>
                <SelectItem value="TestingLocation">Location</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label>Action</Label>
            <Select value={permissionAction} onValueChange={setPermissionAction}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {['read', 'create', 'edit', 'delete', 'approve', 'manage', 'moderate'].map((action) => (
                  <SelectItem key={action} value={action}>
                    {action}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
        <div className="space-y-2">
          <Label>Resource</Label>
          <Select value={resourceId} onValueChange={setResourceId}>
            <SelectTrigger>
              <SelectValue placeholder="Choose a resource" />
            </SelectTrigger>
            <SelectContent>
              {filteredResources.map((resource) => (
                <SelectItem key={resource.id} value={resource.id}>
                  {resource.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="permission-expiry">Optional expiry</Label>
          <Input id="permission-expiry" type="datetime-local" value={expiresAt} onChange={(event) => setExpiresAt(event.currentTarget.value)} />
        </div>
        <div className="flex gap-2">
          <Button
            type="button"
            disabled={resourceDisabled || pending}
            onClick={() => execute(grantTestingLabResourcePermission, { userId: memberId, resourceType, resourceId, action: permissionAction })}
          >
            <ShieldCheck className="mr-2 size-4" /> Grant
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={resourceDisabled || pending}
            onClick={() => execute(revokeTestingLabResourcePermission, { userId: memberId, resourceType, resourceId, action: permissionAction })}
          >
            <ShieldMinus className="mr-2 size-4" /> Revoke
          </Button>
        </div>
      </section>
      <ResultAlert result={result} />
    </div>
  );
}
