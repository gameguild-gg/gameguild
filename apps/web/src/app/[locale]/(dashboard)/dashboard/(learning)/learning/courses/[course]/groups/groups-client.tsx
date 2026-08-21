'use client';

import React, { useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import {
  addGroupMember,
  createCourseGroup,
  createGroupSet,
  removeGroupMember,
} from '@/lib/learning/actions';
import type { CourseGroupSetView } from '@/lib/learning/queries/assessments';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Loader2, Plus, UserPlus, Users, X } from 'lucide-react';

interface GroupsClientProps {
  courseId: string;
  sets: CourseGroupSetView[];
}

interface NewGroupDraft {
  name: string;
  capacity: string;
}

const emptyDraft: NewGroupDraft = { name: '', capacity: '' };

export function GroupsClient({ courseId, sets }: GroupsClientProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [newSetName, setNewSetName] = useState('');
  const [newGroups, setNewGroups] = useState<Record<string, NewGroupDraft>>({});
  const [memberInputs, setMemberInputs] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);

  function draftFor(setId: string): NewGroupDraft {
    return newGroups[setId] ?? emptyDraft;
  }

  function setDraft(setId: string, draft: NewGroupDraft) {
    setNewGroups((current) => ({ ...current, [setId]: draft }));
  }

  function handleCreateSet() {
    setError(null);
    startTransition(async () => {
      const result = await createGroupSet(courseId, newSetName);
      if (!result.success) {
        setError(result.error);
        return;
      }
      setNewSetName('');
      router.refresh();
    });
  }

  function handleCreateGroup(setId: string) {
    const draft = draftFor(setId);
    const capacity = Number(draft.capacity);
    setError(null);
    startTransition(async () => {
      const result = await createCourseGroup({
        courseId,
        setId,
        name: draft.name,
        capacity,
      });
      if (!result.success) {
        setError(result.error);
        return;
      }
      setDraft(setId, emptyDraft);
      router.refresh();
    });
  }

  function handleAddMember(groupId: string) {
    const reference = memberInputs[groupId] ?? '';
    setError(null);
    startTransition(async () => {
      const result = await addGroupMember({
        courseId,
        groupId,
        userReference: reference,
      });
      if (!result.success) {
        setError(result.error);
        return;
      }
      setMemberInputs((current) => ({ ...current, [groupId]: '' }));
      router.refresh();
    });
  }

  function handleRemoveMember(groupId: string, userId: string) {
    setError(null);
    startTransition(async () => {
      const result = await removeGroupMember({ courseId, groupId, userId });
      if (!result.success) {
        setError(result.error);
        return;
      }
      router.refresh();
    });
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Groups</h1>
          <p className="text-sm text-muted-foreground">
            Group sets for group assignments: students self-sign up, or adjust membership manually.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Input
            placeholder="New group set name"
            value={newSetName}
            onChange={(event) => setNewSetName(event.target.value)}
            className="w-56"
          />
          <Button
            size="sm"
            onClick={handleCreateSet}
            disabled={isPending || !newSetName.trim()}
          >
            {isPending ? (
              <Loader2 className="mr-2 size-4 animate-spin" />
            ) : (
              <Plus className="mr-2 size-4" />
            )}
            Create set
          </Button>
        </div>
      </div>

      {error && (
        <div className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </div>
      )}

      {sets.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-12 text-center">
            <Users className="size-8 text-muted-foreground" />
            <p className="font-medium">No group sets yet</p>
            <p className="text-sm text-muted-foreground">
              Create a group set to start organizing students into teams.
            </p>
          </CardContent>
        </Card>
      ) : (
        sets.map((set) => {
          const draft = draftFor(set.id);
          const capacityInvalid =
            draft.capacity !== '' && Number(draft.capacity) < 2;
          const canCreate =
            draft.name.trim() !== '' &&
            draft.capacity !== '' &&
            Number.isInteger(Number(draft.capacity)) &&
            Number(draft.capacity) >= 2;

          return (
            <Card key={set.id}>
              <CardHeader>
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <CardTitle>{set.name}</CardTitle>
                    <CardDescription>
                      {set.groups.length}{' '}
                      {set.groups.length === 1 ? 'group' : 'groups'}
                    </CardDescription>
                  </div>
                  <div className="flex flex-wrap items-end gap-2">
                    <div className="space-y-1">
                      <Label htmlFor={`group-name-${set.id}`}>Group name</Label>
                      <Input
                        id={`group-name-${set.id}`}
                        placeholder="Group name"
                        value={draft.name}
                        onChange={(event) =>
                          setDraft(set.id, { ...draft, name: event.target.value })
                        }
                        className="w-44"
                      />
                    </div>
                    <div className="space-y-1">
                      <Label htmlFor={`group-capacity-${set.id}`}>Capacity</Label>
                      <Input
                        id={`group-capacity-${set.id}`}
                        type="number"
                        min={2}
                        value={draft.capacity}
                        onChange={(event) =>
                          setDraft(set.id, {
                            ...draft,
                            capacity: event.target.value,
                          })
                        }
                        className="w-24"
                      />
                    </div>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handleCreateGroup(set.id)}
                      disabled={isPending || !canCreate}
                    >
                      <Plus className="mr-2 size-4" />
                      Add group
                    </Button>
                  </div>
                </div>
              </CardHeader>
              {capacityInvalid && (
                <p className="px-6 pb-2 text-sm text-destructive">
                  Capacity must be at least 2.
                </p>
              )}
              <CardContent>
                {set.groups.length === 0 ? (
                  <p className="text-sm text-muted-foreground">
                    No groups in this set yet.
                  </p>
                ) : (
                  <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                    {set.groups.map((group) => (
                      <div
                        key={group.id}
                        className="rounded-lg border p-4"
                      >
                        <div className="mb-3 flex items-center justify-between">
                          <span className="font-medium">{group.name}</span>
                          <Badge variant="secondary">
                            {group.memberCount}/{group.capacity}
                          </Badge>
                        </div>
                        <div className="mb-3 flex flex-wrap gap-2">
                          {group.members.length === 0 ? (
                            <span className="text-sm text-muted-foreground">
                              No members
                            </span>
                          ) : (
                            group.members.map((member) => (
                              <span
                                key={member.userId}
                                className="inline-flex items-center gap-1 rounded-full bg-muted px-2 py-1 text-sm"
                              >
                                {member.displayName}
                                <button
                                  type="button"
                                  aria-label={`Remove member ${member.displayName}`}
                                  onClick={() =>
                                    handleRemoveMember(group.id, member.userId)
                                  }
                                  disabled={isPending}
                                  className="rounded-full p-0.5 text-muted-foreground hover:text-destructive"
                                >
                                  <X className="size-3" />
                                </button>
                              </span>
                            ))
                          )}
                        </div>
                        <div className="flex items-center gap-2">
                          <Input
                            placeholder="Add member by email or user ID"
                            value={memberInputs[group.id] ?? ''}
                            onChange={(event) =>
                              setMemberInputs((current) => ({
                                ...current,
                                [group.id]: event.target.value,
                              }))
                            }
                          />
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleAddMember(group.id)}
                            disabled={
                              isPending ||
                              !(memberInputs[group.id] ?? '').trim()
                            }
                          >
                            <UserPlus className="mr-2 size-4" />
                            Add member
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          );
        })
      )}
    </div>
  );
}
