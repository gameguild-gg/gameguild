'use client';

import {
  joinTestingSessionWaitlist,
  leaveTestingSessionWaitlist,
  registerForTestingSession,
  unregisterFromTestingSession,
  type TestingLabActionResult,
} from '@/lib/testing-lab/actions';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Button } from '@game-guild/ui/components/button';
import { AlertCircle, CheckCircle2, Loader2 } from 'lucide-react';
import { useState, useTransition } from 'react';

type RegistrationState = 'idle' | 'registered' | 'waitlisted';

export function TestingLabSessionRegistration({
  sessionId,
  canRegister,
  availableSpots,
  isAuthenticated,
}: {
  sessionId: string;
  canRegister: boolean;
  availableSpots: number;
  isAuthenticated: boolean;
}) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingLabActionResult<unknown> | null>(null);
  const [state, setState] = useState<RegistrationState>('idle');

  function submit(next: RegistrationState) {
    const formData = new FormData();
    formData.set('sessionId', sessionId);
    formData.set('registrationType', 'Tester');
    startTransition(async () => {
      const operation =
        next === 'registered'
          ? registerForTestingSession
          : next === 'waitlisted'
            ? joinTestingSessionWaitlist
            : state === 'registered'
              ? unregisterFromTestingSession
              : leaveTestingSessionWaitlist;
      const response = await operation(formData);
      setResult(response);
      if (response.success) setState(next);
    });
  }

  if (!isAuthenticated) {
    return (
      <Button asChild className="w-full">
        <a href="/sign-in?callbackUrl=%2Ftesting-lab">Sign in to join this session</a>
      </Button>
    );
  }

  const joinWaitlist = availableSpots <= 0;
  return (
    <div className="space-y-3">
      {state === 'idle' ? (
        <Button type="button" className="w-full" disabled={pending || !canRegister} onClick={() => submit(joinWaitlist ? 'waitlisted' : 'registered')}>
          {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
          {!canRegister ? 'Registration closed' : joinWaitlist ? 'Join waitlist' : 'Reserve a tester seat'}
        </Button>
      ) : (
        <Button type="button" className="w-full" variant="outline" disabled={pending} onClick={() => submit('idle')}>
          {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
          {state === 'registered' ? 'Cancel registration' : 'Leave waitlist'}
        </Button>
      )}
      {result ? (
        <Alert variant={result.success ? 'default' : 'destructive'} className="border-white/10 bg-white/5 text-current" aria-live="polite">
          {result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}
          <AlertDescription>{result.success ? result.message : result.error}</AlertDescription>
        </Alert>
      ) : null}
    </div>
  );
}
