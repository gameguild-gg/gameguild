import { TestingLabSidebar } from '@/components/testing-lab/testing-lab-sidebar';
import type { Metadata } from 'next';
import React, { PropsWithChildren } from 'react';

export const metadata: Metadata = {
  title: 'Testing Lab | Game Guild',
  description: 'Submit, test, and manage game projects for Capstone teams',
};

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <div className=" flex flex-col flex-1 items-center">
      <div className="container flex flex-col mx-auto p-6">
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Testing Lab</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Manage testing sessions and review feedback</p>
        </div>
        <div className="flex flex-1 relative">
          <TestingLabSidebar />
          <main className="flex-1 flex flex-col">
            {/* Main Content */}
            {children}
          </main>
        </div>
      </div>
    </div>
  );
}
