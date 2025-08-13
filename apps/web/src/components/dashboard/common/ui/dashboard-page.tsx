import React, { PropsWithChildren } from 'react';

export const DashboardPage = ({ children }: PropsWithChildren): React.JSX.Element => {
  return (
    <div className=" flex flex-col flex-1  gap-8">
      {/* Main Content */}
      {children}
    </div>
  );
};

export const DashboardPageHeader = ({ children }: PropsWithChildren): React.JSX.Element => {
  return (
    <div className="flex flex-col items-center  sticky top-0 z-50 border-b backdrop-blur-sm px-4 py-4 md:px-8">
      <div className="flex flex-col flex-1 container">{children}</div>
    </div>
  );
};

export const DashboardPageTitle = ({ children }: PropsWithChildren): React.JSX.Element => {
  return <h1 className="text-3xl font-bold text-gray-900 dark:text-white">{children}</h1>;
};

export const DashboardPageDescription = ({ children }: PropsWithChildren): React.JSX.Element => {
  return <p className="text-gray-600 dark:text-gray-400">{children}</p>;
};

export const DashboardPageContent = ({ children }: PropsWithChildren): React.JSX.Element => {
  return (
    <div className=" flex flex-col flex-1 items-center px-4 md:px-8">
      <div className="flex flex-col flex-1 container">
        {/* Main Content */}
        {children}
      </div>
    </div>
  );
};
