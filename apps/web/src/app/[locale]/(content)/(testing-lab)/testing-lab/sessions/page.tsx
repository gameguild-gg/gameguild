import { TestingSessions } from '@/components/testing-lab/landing/testing-sessions';
import { getUnifiedTestingSessions } from '@/lib/admin';
import React from "react";

export default async function Page(): Promise<React.JSX.Element> {
    const { testingSessions } = await getUnifiedTestingSessions({ publicOnly: true });
    return <TestingSessions sessions={testingSessions} />;
}
