import React, { PropsWithChildren } from 'react';

export const DashboardPage = async ({ children }: PropsWithChildren): Promise<React.JSX.Element> => {
  return (
    <div className=" flex flex-col flex-1  gap-8">
      {/* Main Content */}
      {children}
    </div>
  );
};

export const DashboardPageHeader = async ({ children }: PropsWithChildren): Promise<React.JSX.Element> => {
  return <div className="flex flex-col sticky top-0 z-50 border-b backdrop-blur-sm px-4 py-8 md:px-8">{children}</div>;
};

export const DashboardPageTitle = async ({ children }: PropsWithChildren): Promise<React.JSX.Element> => {
  return <h1 className="text-3xl font-bold text-gray-900 dark:text-white">{children}</h1>;
};

export const DashboardPageDescription = async ({ children }: PropsWithChildren): Promise<React.JSX.Element> => {
  return <p className="text-gray-600 dark:text-gray-400">{children}</p>;
};

export const DashboardPageContent = async ({ children }: PropsWithChildren): Promise<React.JSX.Element> => {
  return (
    <div className=" flex flex-col flex-1 px-4 md:px-8">
      <div className="flex flex-col container">
        {/* Main Content */}
        {children}
      </div>
    </div>
  );
};
