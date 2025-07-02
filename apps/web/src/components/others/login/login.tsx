import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Lock, User } from 'lucide-react';

const Login = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [remember, setRemember] = useState(false);

  const onFinish = (e: React.FormEvent) => {
    e.preventDefault();
    console.log('Received values of form: ', { username, password, remember });
  };

  return (
    <div className="flex justify-center items-center h-screen">
      <form onSubmit={onFinish} className="w-[300px] space-y-4">
        <div className="space-y-2">
          <Label htmlFor="username">Username</Label>
          <div className="relative">
            <User className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
            <Input id="username" type="text" placeholder="Username" value={username} onChange={(e) => setUsername(e.target.value)} className="pl-10" required />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="password">Password</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
            <Input
              id="password"
              type="password"
              placeholder="Password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="pl-10"
              required
            />
          </div>
        </div>

        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Checkbox id="remember" checked={remember} onCheckedChange={(checked) => setRemember(checked === true)} />
            <Label htmlFor="remember" className="text-sm text-gray-600">
              Remember me
            </Label>
          </div>
          <a href="#" className="text-sm text-blue-600 hover:underline">
            Forgot password
          </a>
        </div>

        <Button type="submit" className="w-full">
          Log in
        </Button>

        <div className="text-center">
          Or{' '}
          <a href="#" className="text-blue-600 hover:underline">
            register now!
          </a>
        </div>
      </form>
    </div>
  );
};

export default Login;
