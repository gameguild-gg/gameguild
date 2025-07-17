import { ComponentShowcase } from '@/components/showcase';

export default function ComponentsPage() {
  return (
    // TODO: Implement NotificationProvider or remove this wrapper
    <ComponentShowcase />
  );
}

export const metadata = {
  title: 'Components Showcase',
  description: 'Showcase of custom UI components including task dashboard, leaderboard, and appearance modal',
};
