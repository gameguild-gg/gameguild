import { TestingLabSettings } from '@/components/testing-lab/testing-lab-settings';
import { Metadata } from 'next';


export const metadata: Metadata = {
  title: 'Testing Lab Settings | Game Guild Dashboard',
  description: 'Configure testing lab locations, settings, and other administrative options.',
};

export default async function TestingLabSettingsPage() {
  return (
    <div className="flex flex-col flex-1 relative">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Testing Lab Settings</h1>
        <p className="text-gray-600 dark:text-gray-400 mt-2">Manage testing locations, configure settings, and control testing lab operations.</p>
      </div>
      <TestingLabSettings />
    </div>
  );
}
