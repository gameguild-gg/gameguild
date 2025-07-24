import React, { PropsWithChildren } from 'react';

export const DashboardPageHeader = async (): Promise<React.JSX.Element> => {
  return (
    <div className="mb-6">
      <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Tenant Management</h1>
      <p className="text-gray-600 dark:text-gray-400 mt-2">Manage tenants, organizations, and access control across the platform.</p>
    </div>
  );
};

export const DashboardPageLayout = async ({ children }: PropsWithChildren): Promise<React.JSX.Element> => {
  return (
    <div className=" flex flex-col flex-1 items-center">
      <div className="container flex flex-col mx-auto p-6">
        <div className="flex flex-1 relative">
          <main className="flex-1 flex flex-col">
            {/* Main Content */}
            {children}
          </main>
        </div>
      </div>
    </div>
  );
};
