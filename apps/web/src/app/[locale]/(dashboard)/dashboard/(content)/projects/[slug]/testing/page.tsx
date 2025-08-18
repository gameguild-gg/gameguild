"use client";

import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';

export default function TestingPage(): React.JSX.Element {
  const sessions: Array<{ id: string; name: string; date?: string; status?: string }> = [];

  return (
    <Card className="dark-card">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle>Testing Sessions</CardTitle>
            <CardDescription>Plan and review your testing sessions.</CardDescription>
          </div>
          <Button size="sm" variant="secondary" disabled>
            New session
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {sessions.length === 0 ? (
          <div className="text-center text-muted-foreground py-10">No sessions yet.</div>
        ) : (
          <div className="rounded-md border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Date</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {sessions.map((s) => (
                  <TableRow key={s.id}>
                    <TableCell>{s.name}</TableCell>
                    <TableCell>{s.date || '—'}</TableCell>
                    <TableCell>{s.status || '—'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
