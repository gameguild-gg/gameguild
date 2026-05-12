'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { usePathname } from 'next/navigation';
import React, { type FormEvent, useState, useTransition } from 'react';
import { submitContactLeadAction } from './actions';

type ContactFormState = {
  name: string;
  email: string;
  company: string;
  topic: string;
  message: string;
};

const initialFormState: ContactFormState = {
  name: '',
  email: '',
  company: '',
  topic: 'sales',
  message: '',
};

export default function Page(): React.JSX.Element {
  const pathname = usePathname();
  const [form, setForm] = useState<ContactFormState>(initialFormState);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [isPending, startTransition] = useTransition();

  function updateField<K extends keyof ContactFormState>(field: K, value: ContactFormState[K]) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFeedback(null);

    startTransition(async () => {
      const result = await submitContactLeadAction({
        ...form,
        pagePath: pathname,
      });

      if (!result.success) {
        setFeedback({ type: 'error', message: result.error });
        return;
      }

      setFeedback({ type: 'success', message: result.message });
      setForm(initialFormState);
    });
  }

  return (
    <main className="mx-auto flex w-full max-w-6xl flex-col gap-8 px-4 py-16 lg:flex-row lg:items-start lg:gap-12">
      <section className="lg:max-w-xl">
        <p className="text-sm font-semibold uppercase tracking-[0.24em] text-sky-500">Contact</p>
        <h1 className="mt-4 text-4xl font-semibold tracking-tight text-foreground sm:text-5xl">Talk to the GameGuild team</h1>
        <p className="mt-5 text-base leading-7 text-muted-foreground sm:text-lg">
          Use this form for partnerships, product questions, platform support, or commercial conversations. Your message now lands in the same marketing-lead flow used for contact and newsletter capture on the platform backend.
        </p>

        <div className="mt-8 grid gap-4 sm:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">What this is for</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm text-muted-foreground">
              <p>Sales and pricing questions</p>
              <p>Support and onboarding requests</p>
              <p>Partnership or collaboration proposals</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Response expectation</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm text-muted-foreground">
              <p>Messages are captured directly in the platform CRM flow.</p>
              <p>Include enough context so the right team can route it quickly.</p>
            </CardContent>
          </Card>
        </div>
      </section>

      <Card className="w-full lg:max-w-2xl">
        <CardHeader>
          <CardTitle>Send a message</CardTitle>
          <CardDescription>Required fields are kept minimal. Topic is used to route the request internally.</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="space-y-5" onSubmit={handleSubmit}>
            <div className="grid gap-5 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="contact-name">Name</Label>
                <Input
                  id="contact-name"
                  name="name"
                  value={form.name}
                  onChange={(event) => updateField('name', event.target.value)}
                  placeholder="Your name"
                  disabled={isPending}
                  required
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="contact-email">Email</Label>
                <Input
                  id="contact-email"
                  name="email"
                  type="email"
                  value={form.email}
                  onChange={(event) => updateField('email', event.target.value)}
                  placeholder="you@example.com"
                  disabled={isPending}
                  required
                />
              </div>
            </div>

            <div className="grid gap-5 sm:grid-cols-[minmax(0,1fr)_200px]">
              <div className="space-y-2">
                <Label htmlFor="contact-company">Company</Label>
                <Input
                  id="contact-company"
                  name="company"
                  value={form.company}
                  onChange={(event) => updateField('company', event.target.value)}
                  placeholder="Optional"
                  disabled={isPending}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="contact-topic">Topic</Label>
                <select
                  id="contact-topic"
                  name="topic"
                  value={form.topic}
                  onChange={(event) => updateField('topic', event.target.value)}
                  className="border-input bg-background h-9 w-full rounded-md border px-3 py-1 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]"
                  disabled={isPending}
                >
                  <option value="sales">Sales</option>
                  <option value="support">Support</option>
                  <option value="partnership">Partnership</option>
                  <option value="other">Other</option>
                </select>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="contact-message">Message</Label>
              <Textarea
                id="contact-message"
                name="message"
                value={form.message}
                onChange={(event) => updateField('message', event.target.value)}
                placeholder="Tell us what you need."
                disabled={isPending}
                required
              />
              <p className="text-xs text-muted-foreground">Minimum 10 characters.</p>
            </div>

            {feedback ? (
              <div
                className={feedback.type === 'success' ? 'rounded-md border border-emerald-500/40 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-700 dark:text-emerald-300' : 'rounded-md border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive'}
                role="status"
              >
                {feedback.message}
              </div>
            ) : null}

            <Button type="submit" disabled={isPending}>
              {isPending ? 'Sending...' : 'Send message'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </main>
  );
}
