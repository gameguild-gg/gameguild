import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export default function FeedbackPage(): React.JSX.Element {
  return (
    <Card className="dark-card">
      <CardHeader>
        <CardTitle>Feedback</CardTitle>
        <CardDescription>Gather and analyze player feedback.</CardDescription>
      </CardHeader>
      <CardContent>Coming soon.</CardContent>
    </Card>
  );
}
