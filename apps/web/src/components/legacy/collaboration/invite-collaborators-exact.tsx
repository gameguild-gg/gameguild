'use client';

import { useState } from 'react';
import { Building, ChevronDown, Search, UserPlus, Users } from 'lucide-react';
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

export function InviteCollaborators({ open, onOpenChange, onInvite, teamMembers = [] }: InviteCollaboratorsProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRole, setSelectedRole] = useState<string>('Editor');

  const defaultTeamMembers: TeamMember[] = [
    {
      id: '1',
      name: 'Lena Hartfield',
      email: 'lena@hartfield.com',
      avatar: undefined, // Will show initials
      role: 'Admin',
    },
    {
      id: '2',
      name: 'Elliot Grayson',
      email: 'elliot@grayson.com',
      avatar: undefined, // Will show initials
      role: 'Editor',
    },
  ];

  const displayMembers = teamMembers.length > 0 ? teamMembers : defaultTeamMembers;

  const filteredMembers = displayMembers.filter((member) => member.name.toLowerCase().includes(searchQuery.toLowerCase()) || member.email.toLowerCase().includes(searchQuery.toLowerCase()));

  const handleInvite = () => {
    if (searchQuery && onInvite) {
      onInvite(searchQuery, selectedRole);
      setSearchQuery('');
    }
  };

  const handleRoleChange = (memberId: string, newRole: string) => {
    console.log(`Updating ${memberId} role to ${newRole}`);
  };

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map((word) => word.charAt(0))
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  const getAvatarBg = (name: string) => {
    // Generate consistent colors for avatars based on name
    const colors = ['bg-blue-500', 'bg-green-500', 'bg-purple-500', 'bg-orange-500', 'bg-pink-500', 'bg-indigo-500'];
    const index = name.charCodeAt(0) % colors.length;
    return colors[index];
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[480px] bg-[#1a1a1a] border-[#2a2a2a] text-white p-0 rounded-2xl overflow-hidden">
        {/* Header */}
        <DialogHeader className="px-6 pt-6 pb-4">
          <div className="flex items-center gap-3">
            <div className="bg-[#2a2a2a] p-2.5 rounded-lg border border-[#3a3a3a]">
              <UserPlus className="h-5 w-5 text-gray-400" />
            </div>
            <div>
              <DialogTitle className="text-lg font-medium text-white">Invite collaborators</DialogTitle>
              <p className="text-sm text-gray-400 mt-0.5">Invite users to work on projects together</p>
            </div>
          </div>
        </DialogHeader>

        {/* Content */}
        <div className="px-6 pb-6">
          <Tabs defaultValue="team-members" className="w-full">
            {/* Tab Navigation */}
            <TabsList className="grid w-full grid-cols-2 bg-[#2a2a2a] p-1 rounded-lg h-auto">
              <TabsTrigger value="team-members" className="flex items-center gap-2 text-sm font-medium px-4 py-2.5 rounded-md data-[state=active]:bg-[#3a3a3a] data-[state=active]:text-white text-gray-400 transition-all">
                <Users className="h-4 w-4" />
                Team members
              </TabsTrigger>
              <TabsTrigger value="organisations" className="flex items-center gap-2 text-sm font-medium px-4 py-2.5 rounded-md data-[state=active]:bg-[#3a3a3a] data-[state=active]:text-white text-gray-500 transition-all">
                <Building className="h-4 w-4" />
                Organisations
              </TabsTrigger>
            </TabsList>

            {/* Team Members Tab */}
            <TabsContent value="team-members" className="space-y-4 mt-4">
              {/* Search Input */}
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-500" />
                <Input
                  placeholder="Search by Name, Email or Username..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-10 bg-[#2a2a2a] border-[#3a3a3a] text-white placeholder:text-gray-500 h-11 rounded-lg focus:border-[#4a4a4a] focus:ring-0 focus-visible:ring-0"
                />
              </div>

              {/* Team Members Section */}
              <div className="space-y-3">
                <h3 className="text-sm font-medium text-gray-400">Team members</h3>

                <div className="space-y-3">
                  {filteredMembers.map((member) => (
                    <div key={member.id} className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <Avatar className="h-10 w-10">
                          <AvatarImage src={member.avatar} alt={member.name} />
                          <AvatarFallback className={`${getAvatarBg(member.name)} text-white text-sm font-medium border-0`}>{getInitials(member.name)}</AvatarFallback>
                        </Avatar>
                        <div>
                          <p className="text-sm font-medium text-white">{member.name}</p>
                          <p className="text-xs text-gray-400">{member.email}</p>
                        </div>
                      </div>

                      {/* Role Selector */}
                      <Select defaultValue={member.role} onValueChange={(value) => handleRoleChange(member.id, value)}>
                        <SelectTrigger className="w-auto min-w-[80px] h-8 bg-[#2a2a2a] border-[#3a3a3a] text-white text-sm rounded-md hover:bg-[#3a3a3a] focus:ring-0 focus-visible:ring-0">
                          <SelectValue />
                          <ChevronDown className="h-4 w-4 ml-1 text-gray-400" />
                        </SelectTrigger>
                        <SelectContent className="bg-[#2a2a2a] border-[#3a3a3a] rounded-lg shadow-lg">
                          <SelectItem value="Admin" className="text-white hover:bg-[#3a3a3a] text-sm focus:bg-[#3a3a3a]">
                            Admin
                          </SelectItem>
                          <SelectItem value="Editor" className="text-white hover:bg-[#3a3a3a] text-sm focus:bg-[#3a3a3a]">
                            Editor
                          </SelectItem>
                          <SelectItem value="Viewer" className="text-white hover:bg-[#3a3a3a] text-sm focus:bg-[#3a3a3a]">
                            Viewer
                          </SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  ))}
                </div>
              </div>

              {/* Invite New User Section */}
              {searchQuery && !filteredMembers.some((m) => m.email === searchQuery) && (
                <div className="flex items-center justify-between p-3 bg-[#2a2a2a] rounded-lg border border-[#3a3a3a]">
                  <div className="flex items-center gap-3">
                    <div className="h-10 w-10 bg-[#3a3a3a] rounded-full flex items-center justify-center">
                      <UserPlus className="h-5 w-5 text-gray-400" />
                    </div>
                    <div>
                      <p className="text-sm font-medium text-white">Invite "{searchQuery}"</p>
                      <p className="text-xs text-gray-400">Send invitation email</p>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <Select value={selectedRole} onValueChange={setSelectedRole}>
                      <SelectTrigger className="w-auto min-w-[80px] h-8 bg-[#1a1a1a] border-[#3a3a3a] text-white text-sm rounded-md">
                        <SelectValue />
                        <ChevronDown className="h-4 w-4 ml-1 text-gray-400" />
                      </SelectTrigger>
                      <SelectContent className="bg-[#2a2a2a] border-[#3a3a3a] rounded-lg shadow-lg">
                        <SelectItem value="Admin" className="text-white hover:bg-[#3a3a3a] text-sm">
                          Admin
                        </SelectItem>
                        <SelectItem value="Editor" className="text-white hover:bg-[#3a3a3a] text-sm">
                          Editor
                        </SelectItem>
                        <SelectItem value="Viewer" className="text-white hover:bg-[#3a3a3a] text-sm">
                          Viewer
                        </SelectItem>
                      </SelectContent>
                    </Select>
                    <Button size="sm" onClick={handleInvite} className="bg-blue-600 hover:bg-blue-700 text-white text-sm px-3 h-8">
                      Invite
                    </Button>
                  </div>
                </div>
              )}
            </TabsContent>

            {/* Organisations Tab */}
            <TabsContent value="organisations" className="mt-4">
              <div className="text-center py-12 text-gray-500">
                <Building className="h-12 w-12 mx-auto mb-3 opacity-40" />
                <p className="text-sm">No organisations available</p>
                <p className="text-xs mt-1 opacity-75">You can invite organisations to collaborate on projects</p>
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </DialogContent>
    </Dialog>
  );
}
