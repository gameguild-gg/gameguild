'use client';

import { Button } from '@/components/ui/button';
import React from 'react';

interface TestingRequestActionsProps {
  requestId: string;
  status: number;
}

export function TestingRequestActions({ requestId, status }: TestingRequestActionsProps) {
  const handleApprove = () => {
    // TODO: Implement approve action
    console.log('Approve request', requestId);
  };

  const handleReject = () => {
    // TODO: Implement reject action
    console.log('Reject request', requestId);
  };

  if (status !== 1) {
    return null;
  }

  return (
    <>
      <Button variant="default" onClick={handleApprove}>
        Approve
      </Button>
      <Button variant="destructive" onClick={handleReject}>
        Reject
      </Button>
    </>
  );
}