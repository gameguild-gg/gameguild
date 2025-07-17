'use client';

import { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@game-guild/ui/components/tabs';
import { CreateGroup } from '@/components/groups';

export function GroupsExample() {
  const [showWithGroups, setShowWithGroups] = useState(true);

  const handleCreateGroup = () => {
    console.log('Creating new group...');
  };

  const sampleGroups = [
    {
      id: '1',
      name: 'Design Team',
      description: 'Creative design collaboration',
      members: [
        { id: '1', name: 'Sarah Chen', initials: 'SC', color: 'bg-orange-500' },
        { id: '2', name: 'Alex Rivera', initials: 'AR', color: 'bg-yellow-500' },
      ],
    },
    {
      id: '2',
      name: 'Development Squad',
      description: 'Frontend and backend development',
      members: [
        { id: '3', name: 'John Smith', initials: 'JS', color: 'bg-green-500' },
        { id: '4', name: 'Maria Garcia', initials: 'MG', color: 'bg-gray-600' },
      ],
    },
  ];

  return (
    <div className="p-8 space-y-6 max-w-6xl mx-auto">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold">Create Group Components</h1>
        <p className="text-muted-foreground">Different variations of the group creation interface with theme support.</p>
      </div>

      <Tabs defaultValue="with-groups" className="w-full">
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="with-groups">With Groups</TabsTrigger>
          <TabsTrigger value="empty-state">Empty State</TabsTrigger>
          <TabsTrigger value="features">Features View</TabsTrigger>
        </TabsList>

        <TabsContent value="with-groups" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Create Group - With Existing Groups</CardTitle>
              <CardDescription>Shows the component when there are existing groups</CardDescription>
            </CardHeader>
            <CardContent>
              <CreateGroup groups={sampleGroups} onCreateGroup={handleCreateGroup} />
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="empty-state" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Create Group - Empty State</CardTitle>
              <CardDescription>Shows the component when no groups exist yet</CardDescription>
            </CardHeader>
            <CardContent>
              <CreateGroup groups={[]} onCreateGroup={handleCreateGroup} />
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="features" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Create Group - Features View</CardTitle>
              <CardDescription>Shows the enhanced version with feature cards</CardDescription>
            </CardHeader>
            <CardContent>
              <CreateGroup groups={[]} onCreateGroup={handleCreateGroup} showFeatures={true} />
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Features Information */}
      <Card>
        <CardHeader>
          <CardTitle>Component Features</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
            <div>
              <h4 className="font-medium mb-2">✨ Visual Features</h4>
              <ul className="space-y-1 text-muted-foreground">
                <li>• Group cards with member avatars</li>
                <li>• Placeholder content simulation</li>
                <li>• Colorful avatar fallbacks</li>
                <li>• Member overflow indicators</li>
                <li>• Feature showcase cards</li>
              </ul>
            </div>
            <div>
              <h4 className="font-medium mb-2">🎨 Theme Support</h4>
              <ul className="space-y-1 text-muted-foreground">
                <li>• Light/dark mode compatible</li>
                <li>• Semantic color tokens</li>
                <li>• Consistent design system</li>
                <li>• Auto-adapting borders</li>
              </ul>
            </div>
            <div>
              <h4 className="font-medium mb-2">⚡ Functionality</h4>
              <ul className="space-y-1 text-muted-foreground">
                <li>• Empty state handling</li>
                <li>• Group creation callback</li>
                <li>• Multiple display modes</li>
                <li>• Feature cards showcase</li>
              </ul>
            </div>
            <div>
              <h4 className="font-medium mb-2">🔧 Technical</h4>
              <ul className="space-y-1 text-muted-foreground">
                <li>• TypeScript support</li>
                <li>• Responsive design</li>
                <li>• Customizable props</li>
                <li>• shadcn/ui integration</li>
              </ul>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Usage Example */}
      <Card>
        <CardHeader>
          <CardTitle>Usage</CardTitle>
        </CardHeader>
        <CardContent>
          <pre className="bg-muted p-4 rounded-lg text-sm overflow-x-auto">
            {`import { CreateGroup } from '@/components/groups';

// Basic usage
<CreateGroup
  groups={userGroups}
  onCreateGroup={() => {
    // Handle group creation
  }}
/>

// With features showcase
<CreateGroup
  groups={[]}
  onCreateGroup={handleCreate}
  showFeatures={true}
/>`}
          </pre>
        </CardContent>
      </Card>
    </div>
  );
}
