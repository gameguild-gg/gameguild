'use client';

interface SessionDetailProps {
  sessionId: string;
}

export function SessionDetail({ sessionId }: SessionDetailProps) {
  return (
    <div className="p-4">
      <h1 className="text-2xl font-bold mb-4">Session Detail</h1>
      <p>Session ID: {sessionId}</p>
      <p>This is a placeholder component for the session detail page.</p>
    </div>
  );
}

export default SessionDetail;
