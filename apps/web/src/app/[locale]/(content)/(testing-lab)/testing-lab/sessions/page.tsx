import { TestingSessions } from '@/components/testing-lab/landing/testing-sessions';
import { getAvailableTestSessions } from '@/lib/admin/testing-lab/sessions/sessions.actions';
import React from "react";

export default async function Page(): Promise<React.JSX.Element> {
    const testingSessions = await getAvailableTestSessions();
    return <TestingSessions sessions={testingSessions} />;
}
