'use client';

import { useState } from 'react';
import { Search, Users, Building, UserPlus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';

interface TeamMember {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  role: 'Admin' | 'Editor' | 'Viewer';
}

interface InviteCollaboratorsProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onInvite?: (email: string, role: string) => void;
  teamMembers?: TeamMember[];
}

export function InviteCollaborators({ 
  open, 
  onOpenChange, 
  onInvite,
  teamMembers = []
}: InviteCollaboratorsProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRole, setSelectedRole] = useState<string>('Editor');

  const defaultTeamMembers: TeamMember[] = [
    {
      id: '1',
      name: 'Lena Hartfield',
      email: 'lena@hartfield.com',
      avatar: '/avatars/lena.jpg',
      role: 'Admin'
    },
    {
      id: '2',
      name: 'Elliot Grayson',
      email: 'elliot@grayson.com',
      avatar: '/avatars/elliot.jpg',
      role: 'Editor'
    }
  ];

  const displayMembers = teamMembers.length > 0 ? teamMembers : defaultTeamMembers;
  
  const filteredMembers = displayMembers.filter(member =>
    member.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    member.email.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const handleInvite = () => {
    if (searchQuery && onInvite) {
      onInvite(searchQuery, selectedRole);
      setSearchQuery('');
    }
  };

  const handleRoleChange = (memberId: string, newRole: string) => {
    // This would typically update the member's role via an API call
    console.log(`Updating ${memberId} role to ${newRole}`);
  };

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map(word => word.charAt(0))
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg bg-gray-900 border-gray-800 text-white p-6 rounded-xl">
        <DialogHeader className="space-y-4">
          <div className="flex items-center gap-3">
            <div className="bg-gray-800 p-2.5 rounded-lg border border-gray-700">
              <UserPlus className="h-5 w-5 text-gray-300" />
            </div>
            <div>
              <DialogTitle className="text-lg font-medium text-white">
                Invite collaborators
              </DialogTitle>
              <p className="text-sm text-gray-400 mt-0.5">
                Invite users to work on projects together
              </p>
            </div>
          </div>
        </DialogHeader>

        <div className="space-y-4 mt-4">
          <Tabs defaultValue="team-members" className="w-full">
            <TabsList className="grid w-full grid-cols-2 bg-gray-800 p-0.5 rounded-lg">
              <TabsTrigger 
                value="team-members" 
                className="flex items-center gap-2 text-sm font-medium px-3 py-2 rounded-md data-[state=active]:bg-gray-700 data-[state=active]:text-white text-gray-300"
              >
                <Users className="h-4 w-4" />
                Team members
              </TabsTrigger>
              <TabsTrigger 
                value="organisations"
                className="flex items-center gap-2 text-sm font-medium px-3 py-2 rounded-md data-[state=active]:bg-gray-700 data-[state=active]:text-white text-gray-400"
              >
                <Building className="h-4 w-4" />
                Organisations
              </TabsTrigger>
            </TabsList>

            <TabsContent value="team-members" className="space-y-4 mt-4">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-500" />
                <Input
                  placeholder="Search by Name, Email or Username..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-10 bg-gray-800 border-gray-700 text-white placeholder:text-gray-500 h-10 rounded-lg focus:border-gray-600 focus:ring-1 focus:ring-gray-600"
                />
              </div>

              <div className="space-y-3">
                <h3 className="text-sm font-medium text-gray-400 mb-2">Team members</h3>
                
                {filteredMembers.map((member) => (
                  <div key={member.id} className="flex items-center justify-between py-3">
                    <div className="flex items-center gap-3">
                      <Avatar className="h-9 w-9">
                        <AvatarImage src={member.avatar} alt={member.name} />
                        <AvatarFallback className="bg-gray-700 text-white text-sm font-medium">
                          {getInitials(member.name)}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <p className="text-sm font-medium text-white">{member.name}</p>
                        <p className="text-xs text-gray-400">{member.email}</p>
                      </div>
                    </div>
                    
                    <Select 
                      defaultValue={member.role}
                      onValueChange={(value) => handleRoleChange(member.id, value)}
                    >
                      <SelectTrigger className="w-20 h-8 bg-gray-800 border-gray-700 text-white text-xs rounded-md hover:bg-gray-750">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent className="bg-gray-800 border-gray-700 rounded-md">
                        <SelectItem value="Admin" className="text-white hover:bg-gray-700 text-xs">
                          Admin
                        </SelectItem>
                        <SelectItem value="Editor" className="text-white hover:bg-gray-700 text-xs">
                          Editor
                        </SelectItem>
                        <SelectItem value="Viewer" className="text-white hover:bg-gray-700">
                          Viewer
                        </SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                ))}
              </div>

              {searchQuery && !filteredMembers.some(m => m.email === searchQuery) && (
                <div className="flex items-center justify-between p-3 bg-gray-800 rounded-lg">
                  <div className="flex items-center gap-3">
                    <div className="h-10 w-10 bg-gray-700 rounded-full flex items-center justify-center">
                      <UserPlus className="h-5 w-5 text-gray-400" />
                    </div>
                    <div>
                      <p className="text-sm font-medium text-white">Invite "{searchQuery}"</p>
                      <p className="text-xs text-gray-400">Send invitation email</p>
                    </div>
                  </div>
                  
                  <div className="flex items-center gap-2">
                    <Select value={selectedRole} onValueChange={setSelectedRole}>
                      <SelectTrigger className="w-24 h-8 bg-gray-700 border-gray-600 text-white">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent className="bg-gray-800 border-gray-600">
                        <SelectItem value="Admin" className="text-white hover:bg-gray-700">
                          Admin
                        </SelectItem>
                        <SelectItem value="Editor" className="text-white hover:bg-gray-700">
                          Editor
                        </SelectItem>
                        <SelectItem value="Viewer" className="text-white hover:bg-gray-700">
                          Viewer
                        </SelectItem>
                      </SelectContent>
                    </Select>
                    <Button
                      size="sm"
                      onClick={handleInvite}
                      className="bg-blue-600 hover:bg-blue-700 text-white"
                    >
                      Invite
                    </Button>
                  </div>
                </div>
              )}
            </TabsContent>

            <TabsContent value="organisations" className="mt-4">
              <div className="text-center py-12 text-gray-500">
                <Building className="h-10 w-10 mx-auto mb-3 opacity-40" />
                <p className="text-sm">No organisations available</p>
                <p className="text-xs mt-1 text-gray-600">You can invite organisations to collaborate on projects</p>
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </DialogContent>
    </Dialog>
  );
}
