'use client';

import { useState } from 'react';
import type { Program } from '@/lib/api/generated/types.gen';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

type ProgramEditFormProps = {
  program: Program;
  onSubmit: (formData: FormData) => Promise<void>;
  pending?: boolean;
};

export default function ProgramEditForm({ program, onSubmit, pending }: ProgramEditFormProps) {
  const [isOpenEnrollment, setIsOpenEnrollment] = useState<boolean>(!!program.isEnrollmentOpen);

  return (
    <form action={onSubmit} className="space-y-6">
      <input type="hidden" name="id" value={(program.id as unknown as string) || ''} />

      <Card>
        <CardHeader>
          <CardTitle>Basic Info</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4">
          <div className="space-y-2">
            <Label htmlFor="title">Title</Label>
            <Input id="title" name="title" defaultValue={program.title || ''} required />
          </div>

          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Textarea id="description" name="description" defaultValue={program.description || ''} rows={4} />
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end gap-2">
        <Button type="submit" disabled={!!pending}>
          {pending ? 'Saving…' : 'Save Changes'}
        </Button>
      </div>
    </form>
  );
}
