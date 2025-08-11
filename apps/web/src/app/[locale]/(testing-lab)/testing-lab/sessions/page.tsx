import React from "react";
import {TestingSessions} from '@/components/testing-lab/landing/testing-sessions';
import {getAvailableTestSessions} from '@/lib/admin';

export default async function Page(): Promise<React.JSX.Element> {
    const sessions = await getAvailableTestSessions();

    return <TestingSessions sessions={sessions}/>;
}
