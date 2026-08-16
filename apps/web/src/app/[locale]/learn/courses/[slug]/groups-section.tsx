'use client';

import { useState } from 'react';
import type { CourseGroupSetView } from '@/lib/learning/queries/assessments';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { joinGroup, leaveGroup } from '@/lib/learning/actions';
import { useRouter } from 'next/navigation';

interface LearnCourseGroupsProps {
  courseId: string;
  currentUserId: string;
  sets: CourseGroupSetView[];
}

/**
 * Student group self-signup on the learn course page. The API owns the rules
 * (capacity, one-group-per-set, lock at deadline); this surface disables what
 * it can see and surfaces server rejections as messages. A "locked" rejection
 * latches per set: both buttons disable with a "Locked at deadline" note.
 */
export function LearnCourseGroups({ courseId, currentUserId, sets }: LearnCourseGroupsProps): React.JSX.Element | null {
  const router = useRouter();
  const [lockedSets, setLockedSets] = useState<Set<string>>(new Set());
  const [messages, setMessages] = useState<Record<string, string | null>>({});
  const [pendingGroup, setPendingGroup] = useState<string | null>(null);

  if (sets.length === 0) return null;

  function applyResult(setId: string, result: { success: boolean; error?: string }) {
    if (result.success) {
      setMessages((prev) => ({ ...prev, [setId]: null }));
      router.refresh();
      return;
    }
    const error = result.error ?? 'Something went wrong.';
    if (error.toLowerCase().includes('lock')) {
      setLockedSets((prev) => new Set(prev).add(setId));
      setMessages((prev) => ({ ...prev, [setId]: null }));
      return;
    }
    setMessages((prev) => ({ ...prev, [setId]: error }));
  }

  async function handleJoin(setId: string, groupId: string) {
    setPendingGroup(groupId);
    const result = await joinGroup(courseId, groupId);
    applyResult(setId, result);
    setPendingGroup(null);
  }

  async function handleLeave(setId: string, groupId: string) {
    setPendingGroup(groupId);
    const result = await leaveGroup(courseId, groupId);
    applyResult(setId, result);
    setPendingGroup(null);
  }

  return (
    <section data-testid="learn-course-groups" className="space-y-4">
      <h2 className="text-xl font-semibold tracking-tight">Groups</h2>
      {sets.map((set) => {
        const locked = lockedSets.has(set.id);
        const memberGroup = set.groups.find((group) => group.members.some((member) => member.userId === currentUserId));
        return (
          <Card key={set.id} data-testid={`group-set-${set.id}`}>
            <CardHeader>
              <div className="flex items-center justify-between gap-2">
                <CardTitle className="text-base">{set.name}</CardTitle>
                {locked && (
                  <Badge variant="outline" title="Locked at deadline">
                    Locked at deadline
                  </Badge>
                )}
              </div>
              <div className="mt-3 space-y-2">
                {set.groups.map((group) => {
                  const isMember = memberGroup?.id === group.id;
                  const isFull = group.memberCount >= group.capacity;
                  return (
                    <div key={group.id} data-testid={`group-${group.id}`} className="flex flex-wrap items-center justify-between gap-2 rounded-md border p-3">
                      <div className="min-w-0">
                        <p className="text-sm font-medium">
                          {group.name}{' '}
                          <span className="text-muted-foreground">
                            {group.memberCount}/{group.capacity}
                          </span>
                        </p>
                        {group.members.length > 0 && (
                          <p className="mt-0.5 flex flex-wrap gap-1 text-xs text-muted-foreground">
                            {group.members.map((member) => (
                              <span key={member.userId} data-testid={`group-member-${member.userId}`}>
                                {member.displayName}
                              </span>
                            ))}
                          </p>
                        )}
                      </div>
                      {isMember ? (
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleLeave(set.id, group.id)}
                          disabled={locked || pendingGroup === group.id}
                          title={locked ? 'Locked at deadline' : undefined}
                        >
                          Leave {group.name}
                        </Button>
                      ) : !memberGroup ? (
                        <Button
                          size="sm"
                          onClick={() => handleJoin(set.id, group.id)}
                          disabled={locked || isFull || pendingGroup === group.id}
                          title={locked ? 'Locked at deadline' : isFull ? 'This group is full' : undefined}
                        >
                          Join {group.name}
                        </Button>
                      ) : null}
                    </div>
                  );
                })}
              </div>
              {messages[set.id] && (
                <p role="alert" className="mt-2 text-sm text-destructive">
                  {messages[set.id]}
                </p>
              )}
            </CardHeader>
          </Card>
        );
      })}
    </section>
  );
}
