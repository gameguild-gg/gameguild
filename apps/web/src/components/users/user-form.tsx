'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import React from 'react';

type UserFormProps = {
  onSubmit: (formData: FormData) => void | Promise<void>;
};

export function UserForm({ onSubmit }: UserFormProps) {
  const [role, setRole] = React.useState('member');

  return (
    <form action={onSubmit} className="space-y-6 max-w-xl">
      <div className="grid gap-2">
        <Label htmlFor="name">Name</Label>
        <Input id="name" name="name" placeholder="Jane Doe" required />
      </div>

      <div className="grid gap-2">
        <Label htmlFor="email">Email</Label>
        <Input id="email" name="email" type="email" placeholder="jane@example.com" required />
      </div>

      <div className="grid gap-2">
        <Label htmlFor="role">Role</Label>
        <Select value={role} onValueChange={setRole}>
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Select a role" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="member">Member</SelectItem>
            <SelectItem value="admin">Admin</SelectItem>
            <SelectItem value="moderator">Moderator</SelectItem>
          </SelectContent>
        </Select>
        {/* Hidden input ensures the selected role is included in FormData */}
        <input type="hidden" name="role" value={role} />
      </div>

      <div className="pt-2">
        <Button type="submit">Create User</Button>
      </div>
    </form>
  );
}

export default UserForm;
