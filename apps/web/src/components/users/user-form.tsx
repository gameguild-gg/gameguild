'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';


type UserFormProps = {
  onSubmit: (formData: FormData) => void | Promise<void>;
};

export function UserForm({ onSubmit }: UserFormProps) {
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

      <div className="pt-2">
        <Button type="submit">Create User</Button>
      </div>
    </form>
  );
}

export default UserForm;
