"use client";

import {
  assignTestingLabRole,
  grantTestingLabResourcePermission,
  inspectTestingLabUserAccess,
  revokeTestingLabResourcePermission,
  revokeTestingLabRole,
  type TestingLabActionResult,
} from "@/lib/testing-lab/actions";
import type {
  TestingLabTestingLabRoleTemplate,
  TestingLabUserTestingLabPermissions,
} from "@game-guild/client";
import { Alert, AlertDescription } from "@game-guild/ui/components/alert";
import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@game-guild/ui/components/sheet";
import {
  AlertCircle,
  CheckCircle2,
  Clock3,
  Loader2,
  RefreshCw,
  ShieldCheck,
  ShieldMinus,
  ShieldPlus,
  Trash2,
  UserCog,
} from "lucide-react";
import { useMemo, useState, useTransition } from "react";

interface MemberOption {
  id: string;
  label: string;
}

interface ResourceOption {
  id: string;
  label: string;
  type: "TestingRequest" | "TestingSession" | "TestingLocation";
}

type Operation = (
  formData: FormData,
) => Promise<TestingLabActionResult<unknown>>;
type AccessInspection = (
  formData: FormData,
) => Promise<TestingLabActionResult<TestingLabUserTestingLabPermissions>>;

const resourceActions: Record<ResourceOption["type"], readonly string[]> = {
  TestingRequest: ["read", "edit", "delete", "approve"],
  TestingSession: ["read", "edit", "delete"],
  TestingLocation: ["read", "edit", "delete"],
};

export function getTestingLabResourceActions(
  resourceType: ResourceOption["type"],
): readonly string[] {
  return resourceActions[resourceType];
}

function createFormData(values: Record<string, string>) {
  const formData = new FormData();
  Object.entries(values).forEach(([key, value]) => formData.set(key, value));
  return formData;
}

export async function executeTestingLabAccessMutation(
  operation: Operation,
  inspect: AccessInspection,
  values: Record<string, string>,
): Promise<{
  result: TestingLabActionResult<unknown>;
  effectiveAccess: TestingLabUserTestingLabPermissions | null;
  refreshError: string | null;
}> {
  const result = await operation(createFormData(values));
  if (!result.success)
    return { result, effectiveAccess: null, refreshError: null };

  const refreshed = await inspect(
    createFormData({ userId: values.userId ?? "" }),
  );
  if (!refreshed.success) {
    return { result, effectiveAccess: null, refreshError: refreshed.error };
  }

  return { result, effectiveAccess: refreshed.data, refreshError: null };
}

function ResultAlert({
  result,
  refreshError,
}: {
  result: TestingLabActionResult<unknown> | null;
  refreshError?: string | null;
}) {
  if (!result && !refreshError) return null;
  const success = Boolean(result?.success) && !refreshError;
  const message = refreshError
    ? "The change was saved, but effective access could not be refreshed: " +
      refreshError
    : result?.success
      ? result.message
      : result?.error;

  return (
    <Alert variant={success ? "default" : "destructive"} aria-live="polite">
      {success ? (
        <CheckCircle2 className="size-4" />
      ) : (
        <AlertCircle className="size-4" />
      )}
      <AlertDescription>{message}</AlertDescription>
    </Alert>
  );
}

function permissionLabel(permission: string) {
  return permission
    .replace(/^can/, "")
    .replace(/([A-Z])/g, " $1")
    .trim();
}

function formatPermissionExpiry(value: string) {
  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC",
  }).format(new Date(value));
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
  const [memberId, setMemberId] = useState("");
  const [roleName, setRoleName] = useState("");
  const [resourceType, setResourceType] =
    useState<ResourceOption["type"]>("TestingRequest");
  const [resourceId, setResourceId] = useState("");
  const [permissionAction, setPermissionAction] = useState("read");
  const [expiresAt, setExpiresAt] = useState("");
  const [open, setOpen] = useState(false);
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingLabActionResult<unknown> | null>(
    null,
  );
  const [refreshError, setRefreshError] = useState<string | null>(null);
  const [effectiveAccess, setEffectiveAccess] =
    useState<TestingLabUserTestingLabPermissions | null>(null);

  const selectedMember = members.find((member) => member.id === memberId);
  const filteredResources = useMemo(
    () => resources.filter((resource) => resource.type === resourceType),
    [resourceType, resources],
  );
  const resourceById = useMemo(
    () => new Map(resources.map((resource) => [resource.id, resource])),
    [resources],
  );
  const availableActions = getTestingLabResourceActions(resourceType);

  function loadAccess(userId = memberId) {
    if (!userId) return;
    startTransition(async () => {
      const access = await inspectTestingLabUserAccess(
        createFormData({ userId }),
      );
      setResult(access);
      setRefreshError(null);
      if (access.success) setEffectiveAccess(access.data);
    });
  }

  function openMemberAccess() {
    if (!memberId) return;
    setOpen(true);
    loadAccess(memberId);
  }

  function execute(operation: Operation, values: Record<string, string>) {
    startTransition(async () => {
      const outcome = await executeTestingLabAccessMutation(
        operation,
        inspectTestingLabUserAccess,
        values,
      );
      setResult(outcome.result);
      setRefreshError(outcome.refreshError);
      if (outcome.effectiveAccess) setEffectiveAccess(outcome.effectiveAccess);
    });
  }

  const roleDisabled = !memberId || !roleName;
  const resourceDisabled = !memberId || !resourceId || !permissionAction;

  return (
    <div className="space-y-3">
      <Label htmlFor="access-member">Member</Label>
      <div className="grid gap-2 sm:grid-cols-[minmax(0,1fr)_auto]">
        <Select
          value={memberId}
          onValueChange={(value) => {
            setMemberId(value);
            setEffectiveAccess(null);
            setResult(null);
            setRefreshError(null);
          }}
        >
          <SelectTrigger id="access-member">
            <SelectValue placeholder="Choose a community member" />
          </SelectTrigger>
          <SelectContent>
            {members.map((member) => (
              <SelectItem key={member.id} value={member.id}>
                {member.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Button type="button" disabled={!memberId} onClick={openMemberAccess}>
          <UserCog aria-hidden="true" className="mr-2 size-4" />
          Manage access
        </Button>
      </div>

      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent className="w-full gap-0 overflow-y-auto p-0 sm:max-w-2xl">
          <SheetHeader className="sticky top-0 z-10 border-b bg-background pr-12">
            <SheetTitle>
              {selectedMember
                ? "Testing Lab access · " + selectedMember.label
                : "Testing Lab access"}
            </SheetTitle>
            <SheetDescription>
              Roles define baseline access. Resource exceptions add time-limited
              access to one request, session, or location.
            </SheetDescription>
          </SheetHeader>

          <div className="space-y-7 p-4 sm:p-6">
            <ResultAlert result={result} refreshError={refreshError} />

            <section className="space-y-3">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <h3 className="font-semibold">Effective access</h3>
                  <p className="text-sm text-muted-foreground">
                    Merged roles, permissions, and active resource exceptions.
                  </p>
                </div>
                <Button
                  type="button"
                  size="icon"
                  variant="ghost"
                  disabled={pending || !memberId}
                  onClick={() => loadAccess()}
                  title="Refresh access"
                >
                  {pending ? (
                    <Loader2
                      aria-hidden="true"
                      className="size-4 animate-spin"
                    />
                  ) : (
                    <RefreshCw aria-hidden="true" className="size-4" />
                  )}
                  <span className="sr-only">Refresh access</span>
                </Button>
              </div>

              {effectiveAccess ? (
                <div className="space-y-4 rounded-md border p-4">
                  <div>
                    <p className="mb-2 text-xs font-medium uppercase text-muted-foreground">
                      Assigned roles
                    </p>
                    <div className="flex flex-wrap gap-1.5">
                      {(effectiveAccess.assignedRoles ?? []).length > 0 ? (
                        effectiveAccess.assignedRoles?.map((role) => (
                          <Badge key={role}>{role}</Badge>
                        ))
                      ) : (
                        <span className="text-sm text-muted-foreground">
                          No Testing Lab roles assigned.
                        </span>
                      )}
                    </div>
                  </div>
                  <div>
                    <p className="mb-2 text-xs font-medium uppercase text-muted-foreground">
                      Effective permissions
                    </p>
                    <div className="flex flex-wrap gap-1.5">
                      {Object.entries(effectiveAccess.permissions ?? {})
                        .filter(([, enabled]) => enabled)
                        .map(([permission]) => (
                          <Badge key={permission} variant="outline">
                            {permissionLabel(permission)}
                          </Badge>
                        ))}
                    </div>
                  </div>
                </div>
              ) : (
                <div className="rounded-md border border-dashed p-4 text-sm text-muted-foreground">
                  Access is loaded when this sheet opens.
                </div>
              )}
            </section>

            <section className="space-y-3 border-t pt-6">
              <div>
                <h3 className="font-semibold">Role assignment</h3>
                <p className="text-sm text-muted-foreground">
                  Apply or remove a reusable role in the current tenant.
                </p>
              </div>
              <div className="grid gap-2 sm:grid-cols-[minmax(0,1fr)_auto_auto]">
                <Select value={roleName} onValueChange={setRoleName}>
                  <SelectTrigger aria-label="Testing Lab role">
                    <SelectValue placeholder="Choose a role" />
                  </SelectTrigger>
                  <SelectContent>
                    {roles.map((role) => (
                      <SelectItem
                        key={role.id ?? role.name}
                        value={role.name ?? ""}
                      >
                        {role.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Button
                  type="button"
                  disabled={roleDisabled || pending}
                  onClick={() =>
                    execute(assignTestingLabRole, {
                      userId: memberId,
                      roleName,
                    })
                  }
                >
                  <ShieldPlus aria-hidden="true" className="mr-2 size-4" />
                  Assign
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  disabled={roleDisabled || pending}
                  onClick={() =>
                    execute(revokeTestingLabRole, {
                      userId: memberId,
                      roleName,
                    })
                  }
                >
                  <ShieldMinus aria-hidden="true" className="mr-2 size-4" />
                  Revoke
                </Button>
              </div>
            </section>

            <section className="space-y-4 border-t pt-6">
              <div>
                <h3 className="font-semibold">Resource exceptions</h3>
                <p className="text-sm text-muted-foreground">
                  Grant one supported action on one operational resource, with
                  an optional expiry.
                </p>
              </div>

              {(effectiveAccess?.resourcePermissions ?? []).length > 0 ? (
                <div className="divide-y rounded-md border">
                  {effectiveAccess?.resourcePermissions?.map((permission) => {
                    const resource = resourceById.get(
                      permission.resourceId ?? "",
                    );
                    return (
                      <div
                        key={[
                          permission.resourceType,
                          permission.resourceId,
                          permission.action,
                        ].join(":")}
                        className="flex items-center justify-between gap-3 p-3"
                      >
                        <div className="min-w-0">
                          <p className="truncate text-sm font-medium">
                            {resource?.label ?? permission.resourceId}
                          </p>
                          <p className="mt-1 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                            <span>
                              {permission.resourceType?.replace("Testing", "")}
                            </span>
                            <span>·</span>
                            <span>{permission.action}</span>
                            {permission.expiresAt ? (
                              <>
                                <span>·</span>
                                <span className="inline-flex items-center gap-1">
                                  <Clock3
                                    aria-hidden="true"
                                    className="size-3"
                                  />
                                  Expires{" "}
                                  {formatPermissionExpiry(
                                    permission.expiresAt,
                                  )}{" "}
                                  UTC
                                </span>
                              </>
                            ) : null}
                          </p>
                        </div>
                        <Button
                          type="button"
                          size="icon"
                          variant="ghost"
                          disabled={pending}
                          title="Revoke resource permission"
                          onClick={() =>
                            execute(revokeTestingLabResourcePermission, {
                              userId: memberId,
                              resourceType: permission.resourceType ?? "",
                              resourceId: permission.resourceId ?? "",
                              action: permission.action ?? "",
                            })
                          }
                        >
                          <Trash2 aria-hidden="true" className="size-4" />
                          <span className="sr-only">
                            Revoke resource permission
                          </span>
                        </Button>
                      </div>
                    );
                  })}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">
                  No active resource exceptions.
                </p>
              )}

              <div className="grid gap-3 rounded-md bg-muted/25 p-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label>Resource type</Label>
                  <Select
                    value={resourceType}
                    onValueChange={(value) => {
                      const type = value as ResourceOption["type"];
                      setResourceType(type);
                      setResourceId("");
                      setPermissionAction(
                        getTestingLabResourceActions(type)[0] ?? "read",
                      );
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
                  <Select
                    value={permissionAction}
                    onValueChange={setPermissionAction}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {getTestingLabResourceActions(resourceType).map(
                        (action) => (
                          <SelectItem key={action} value={action}>
                            {action}
                          </SelectItem>
                        ),
                      )}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2 sm:col-span-2">
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
                  <Input
                    id="permission-expiry"
                    type="datetime-local"
                    value={expiresAt}
                    onChange={(event) =>
                      setExpiresAt(event.currentTarget.value)
                    }
                  />
                </div>
                <div className="flex items-end">
                  <Button
                    type="button"
                    className="w-full"
                    disabled={resourceDisabled || pending}
                    onClick={() =>
                      execute(grantTestingLabResourcePermission, {
                        userId: memberId,
                        resourceType,
                        resourceId,
                        action: permissionAction,
                        expiresAt,
                      })
                    }
                  >
                    <ShieldCheck aria-hidden="true" className="mr-2 size-4" />
                    Grant exception
                  </Button>
                </div>
              </div>
            </section>
          </div>
        </SheetContent>
      </Sheet>
    </div>
  );
}
