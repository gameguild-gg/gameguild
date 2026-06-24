'use client';

import React, { useMemo, useState, useTransition } from 'react';
import { manualEnrollStudent } from '@/lib/learning/actions';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@game-guild/ui/components/dialog';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { Progress } from '@game-guild/ui/components/progress';
import { Avatar, AvatarFallback } from '@game-guild/ui/components/avatar';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Checkbox } from '@game-guild/ui/components/checkbox';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@game-guild/ui/components/dropdown-menu';
import { Download, Eye, Loader2, Mail, MoreHorizontal, Search, TrendingUp, UserMinus, UserPlus, Users } from 'lucide-react';

interface Student {
  id: string;
  name: string;
  email: string;
  completionPercent: number;
  isActive: boolean;
  enrolledAt: string;
  lastActivity: string;
}

function progressColor(value: number) {
  if (value >= 80) return 'text-green-600';
  if (value >= 40) return 'text-yellow-600';
  return 'text-red-600';
}

function getStatusLabel(student: Student) {
  if (student.completionPercent >= 100) return 'completed';
  if (student.isActive) return 'active';
  return 'inactive';
}

function StatusBadge({ student }: { student: Student }) {
  const status = getStatusLabel(student);
  const config = {
    completed: { variant: 'default' as const, label: 'Completed' },
    active: { variant: 'secondary' as const, label: 'Active' },
    inactive: { variant: 'outline' as const, label: 'Inactive' },
  };
  const c = config[status];
  return <Badge variant={c.variant}>{c.label}</Badge>;
}

export function StudentTable({ courseId, students, total }: { courseId: string; students: Student[]; total: number }) {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [isPending, startTransition] = useTransition();
  const [manualEnrollOpen, setManualEnrollOpen] = useState(false);
  const [manualUserId, setManualUserId] = useState('');
  const [manualCohortId, setManualCohortId] = useState('');
  const [manualEnrollError, setManualEnrollError] = useState<string | null>(null);

  const submitManualEnrollment = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setManualEnrollError(null);

    startTransition(async () => {
      const result = await manualEnrollStudent({
        courseId,
        userId: manualUserId,
        cohortId: manualCohortId || null,
      });

      if (result.success) {
        setManualEnrollOpen(false);
        setManualUserId('');
        setManualCohortId('');
        return;
      }

      setManualEnrollError(result.error);
    });
  };

  const filtered = useMemo(() => {
    let result = students;

    if (search) {
      const q = search.toLowerCase();
      result = result.filter(
        (s) => s.name.toLowerCase().includes(q) || s.email.toLowerCase().includes(q),
      );
    }

    if (statusFilter !== 'all') {
      result = result.filter((s) => getStatusLabel(s) === statusFilter);
    }

    return result;
  }, [students, search, statusFilter]);

  const toggleAll = () => {
    if (selectedIds.size === filtered.length) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(filtered.map((s) => s.id)));
    }
  };

  const toggleOne = (id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <CardTitle>Enrolled Students</CardTitle>
            <CardDescription>
              {total > 0 ? `${total} students enrolled` : 'No students enrolled yet'}
            </CardDescription>
          </div>
          <div className="flex flex-wrap gap-2">
            <Dialog open={manualEnrollOpen} onOpenChange={setManualEnrollOpen}>
              <DialogTrigger asChild>
                <Button size="sm">
                  <UserPlus className="mr-2 size-4" />
                  Enroll student
                </Button>
              </DialogTrigger>
              <DialogContent>
                <form onSubmit={submitManualEnrollment} className="space-y-5">
                  <DialogHeader>
                    <DialogTitle>Enroll student manually</DialogTitle>
                    <DialogDescription>
                    Add an existing GameGuild user to this course by email, username, or canonical user ID.
                    </DialogDescription>
                  </DialogHeader>
                  <div className="space-y-4">
                    <div className="space-y-2">
                    <Label htmlFor="manual-user-id">Student</Label>
                      <Input
                        id="manual-user-id"
                        value={manualUserId}
                        onChange={(event) => setManualUserId(event.target.value)}
                      placeholder="student@example.com, username, or user ID"
                        required
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="manual-cohort-id">Cohort ID</Label>
                      <Input
                        id="manual-cohort-id"
                        value={manualCohortId}
                        onChange={(event) => setManualCohortId(event.target.value)}
                        placeholder="Optional"
                      />
                      <p className="text-xs text-muted-foreground">Leave blank to enroll the student directly in the course.</p>
                    </div>
                    {manualEnrollError && <p className="text-sm text-destructive">{manualEnrollError}</p>}
                  </div>
                  <DialogFooter>
                    <Button type="button" variant="outline" onClick={() => setManualEnrollOpen(false)}>
                      Cancel
                    </Button>
                    <Button type="submit" disabled={isPending || !manualUserId.trim()}>
                      {isPending && <Loader2 className="mr-2 size-4 animate-spin" />}
                      Enroll student
                    </Button>
                  </DialogFooter>
                </form>
              </DialogContent>
            </Dialog>
            <Button variant="outline" size="sm">
              <Download className="mr-2 size-4" />
              Export
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {/* Filters */}
        <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Search by name or email..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9"
            />
          </div>
          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-full sm:w-[160px]">
              <SelectValue placeholder="Filter" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Students</SelectItem>
              <SelectItem value="active">Active</SelectItem>
              <SelectItem value="completed">Completed</SelectItem>
              <SelectItem value="inactive">Inactive</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Bulk Actions */}
        {selectedIds.size > 0 && (
          <div className="mb-4 flex items-center justify-between rounded-lg bg-muted/50 p-3">
            <span className="text-sm">{selectedIds.size} student(s) selected</span>
            <div className="flex gap-2">
              <Button variant="outline" size="sm">
                <Mail className="mr-2 size-4" />
                Send Message
              </Button>
              <Button variant="outline" size="sm" className="text-destructive">
                <UserMinus className="mr-2 size-4" />
                Remove
              </Button>
            </div>
          </div>
        )}

        {filtered.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <Users className="mb-4 size-12 text-muted-foreground" />
            <h3 className="text-lg font-semibold">No students found</h3>
            <p className="text-sm text-muted-foreground">
              {search || statusFilter !== 'all'
                ? 'Try adjusting your search or filter criteria.'
                : 'Students will appear here once they enroll in this course.'}
            </p>
          </div>
        ) : (
          <>
            <div className="rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-12">
                      <Checkbox
                        checked={selectedIds.size === filtered.length && filtered.length > 0}
                        onCheckedChange={toggleAll}
                      />
                    </TableHead>
                    <TableHead>Student</TableHead>
                    <TableHead>Progress</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="hidden md:table-cell">Enrolled</TableHead>
                    <TableHead className="hidden lg:table-cell">Last Active</TableHead>
                    <TableHead className="w-12" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filtered.map((student) => {
                    const initials = student.name
                      .split(' ')
                      .map((n) => n[0])
                      .join('')
                      .toUpperCase()
                      .slice(0, 2);
                    return (
                      <TableRow key={student.id}>
                        <TableCell>
                          <Checkbox
                            checked={selectedIds.has(student.id)}
                            onCheckedChange={() => toggleOne(student.id)}
                          />
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-3">
                            <Avatar className="size-8">
                              <AvatarFallback className="text-xs">{initials}</AvatarFallback>
                            </Avatar>
                            <div className="flex flex-col">
                              <span className="font-medium">{student.name}</span>
                              <span className="text-xs text-muted-foreground">{student.email}</span>
                            </div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Progress value={student.completionPercent} className="w-20" />
                            <span className={`text-sm font-medium ${progressColor(student.completionPercent)}`}>
                              {student.completionPercent}%
                            </span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <StatusBadge student={student} />
                        </TableCell>
                        <TableCell className="hidden text-sm text-muted-foreground md:table-cell">
                          {new Date(student.enrolledAt).toLocaleDateString()}
                        </TableCell>
                        <TableCell className="hidden text-sm text-muted-foreground lg:table-cell">
                          {new Date(student.lastActivity).toLocaleDateString()}
                        </TableCell>
                        <TableCell>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button variant="ghost" size="icon" className="size-8">
                                <MoreHorizontal className="size-4" />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end">
                              <DropdownMenuItem>
                                <Eye className="mr-2 size-4" />
                                View Profile
                              </DropdownMenuItem>
                              <DropdownMenuItem>
                                <TrendingUp className="mr-2 size-4" />
                                View Progress
                              </DropdownMenuItem>
                              <DropdownMenuSeparator />
                              <DropdownMenuItem className="text-destructive">
                                <UserMinus className="mr-2 size-4" />
                                Remove from Course
                              </DropdownMenuItem>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>

            <div className="mt-4 flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                Showing {filtered.length} of {students.length} students
              </p>
            </div>
          </>
        )}
      </CardContent>
    </Card>
  );
}
