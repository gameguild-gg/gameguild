'use client';

import React, { useState } from 'react';
import { useRouter } from '@/i18n/navigation';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { AlertTriangle, Archive, Loader2, ShieldCheck, Trash2, UserRoundCog } from 'lucide-react';
import { archiveCourse, deleteCourse, transferCourseOwnership } from '@/lib/learning/actions';
import type { CourseDetails } from '@/lib/learning/types';

/**
 * Course danger zone (issue #106 phase 1). The course record is fetched by
 * the server page and arrives as a prop; the destructive confirmations and
 * lifecycle actions stay on the client.
 */
export function CourseDangerZoneForm({
  course,
  surface,
}: {
  course: CourseDetails;
  surface: 'workspace' | 'console';
}): React.JSX.Element {
  const router = useRouter();
  const [pendingAction, setPendingAction] = useState<'archive' | 'delete' | 'transfer' | null>(null);
  const isPending = pendingAction !== null;
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Delete confirmation
  const [deleteConfirm, setDeleteConfirm] = useState('');
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [newOwnerReference, setNewOwnerReference] = useState('');
  const [ownershipConfirm, setOwnershipConfirm] = useState('');

  async function handleArchive() {
    setError(null);
    setSuccess(null);
    setPendingAction('archive');
    try {
      const result = await archiveCourse(course.id);
      if (result.success) {
        setSuccess('Archived successfully.');
        router.refresh();
      } else {
        setError(result.error);
      }
    } catch (archiveError) {
      setError(archiveError instanceof Error ? archiveError.message : 'Course could not be archived.');
    } finally {
      setPendingAction(null);
    }
  }

  async function handleDelete() {
    if (deleteConfirm !== course.title) return;
    setError(null);
    setSuccess(null);
    setPendingAction('delete');
    try {
      const result = await deleteCourse(course.id);
      if (result.success) {
        router.push(`/${surface}/learning/courses`);
      } else {
        setError(result.error);
      }
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : 'Course could not be deleted.');
    } finally {
      setPendingAction(null);
    }
  }

  async function handleTransferOwnership() {
    if (ownershipConfirm !== course.title) return;
    setError(null);
    setSuccess(null);
    setPendingAction('transfer');
    try {
      const result = await transferCourseOwnership(course.id, newOwnerReference);
      if (result.success) {
        setNewOwnerReference('');
        setOwnershipConfirm('');
        setSuccess('Course ownership was transferred.');
        router.refresh();
      } else {
        setError(result.error);
      }
    } catch (transferError) {
      setError(transferError instanceof Error ? transferError.message : 'Course ownership could not be transferred.');
    } finally {
      setPendingAction(null);
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h2 className="text-lg font-semibold">Settings</h2>
        <p className="text-sm text-muted-foreground">Manage course ownership and restricted lifecycle controls.</p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">
          {error}
        </div>
      )}
      {success && (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-300">
          {success}
        </div>
      )}

      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <UserRoundCog className="size-5 text-primary" />
            <CardTitle className="text-base">Transfer course ownership</CardTitle>
          </div>
          <CardDescription>
            Transfer the course owner to another instructor or administrator. The new owner can manage listing, content, students, billing-sensitive settings, and lifecycle controls.
          </CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4">
          <div className="rounded-md border bg-muted/30 p-3 text-sm">
            <span className="text-muted-foreground">Current owner ID:</span>{' '}
            <span className="font-mono">{course.creatorId ?? 'Not assigned'}</span>
          </div>
          <div className="grid gap-2">
            <Label htmlFor="new-owner">New owner email, username, or user ID</Label>
            <Input
              id="new-owner"
              value={newOwnerReference}
              onChange={(event) => setNewOwnerReference(event.target.value)}
              placeholder="instructor@gameguild.gg"
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="ownership-confirm">
              Type <strong>{course.title}</strong> to confirm transfer
            </Label>
            <Input
              id="ownership-confirm"
              value={ownershipConfirm}
              onChange={(event) => setOwnershipConfirm(event.target.value)}
              placeholder={course.title}
            />
          </div>
          <div>
            <Button
              type="button"
              variant="outline"
              onClick={() => void handleTransferOwnership()}
              disabled={isPending || !newOwnerReference.trim() || ownershipConfirm !== course.title}
            >
              {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <ShieldCheck className="mr-2 size-4" />}
              Transfer ownership
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Archive */}
      {course.status !== 'archived' && (
        <Card className="border-amber-200 dark:border-amber-900">
          <CardHeader>
            <div className="flex items-center gap-2">
              <Archive className="size-5 text-amber-600" />
              <CardTitle className="text-base">Archive this course</CardTitle>
            </div>
            <CardDescription>
              Archiving hides the course from students and stops new enrollments. Existing student data is preserved.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Button variant="outline" onClick={() => void handleArchive()} disabled={isPending}>
              {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Archive className="mr-2 size-4" />}
              Archive Course
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Delete */}
      <Card className="border-red-200 dark:border-red-900">
        <CardHeader>
          <div className="flex items-center gap-2">
            <AlertTriangle className="size-5 text-destructive" />
            <CardTitle className="text-base text-destructive">Delete this course</CardTitle>
          </div>
          <CardDescription>
            Once deleted, this course and all its content, assessments, student enrollments, and analytics data will be permanently removed. This action cannot be undone.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {!showDeleteConfirm ? (
            <Button variant="destructive" onClick={() => setShowDeleteConfirm(true)}>
              <Trash2 className="mr-2 size-4" />
              Delete Course
            </Button>
          ) : (
            <>
              <div className="flex flex-col gap-2">
                <Label htmlFor="delete-confirm">
                  Type <strong>{course.title}</strong> to confirm deletion
                </Label>
                <Input
                  id="delete-confirm"
                  value={deleteConfirm}
                  onChange={(e) => setDeleteConfirm(e.target.value)}
                  placeholder={course.title}
                  autoFocus
                />
              </div>
              <div className="flex gap-2">
                <Button
                  variant="destructive"
                  onClick={() => void handleDelete()}
                  disabled={isPending || deleteConfirm !== course.title}
                >
                  {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Trash2 className="mr-2 size-4" />}
                  Permanently Delete
                </Button>
                <Button variant="outline" onClick={() => { setShowDeleteConfirm(false); setDeleteConfirm(''); }}>
                  Cancel
                </Button>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
