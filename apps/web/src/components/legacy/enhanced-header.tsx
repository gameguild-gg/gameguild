'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuSub, DropdownMenuSubContent, DropdownMenuSubTrigger, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { NavigationMenu, NavigationMenuContent, NavigationMenuItem, NavigationMenuList, NavigationMenuTrigger } from '@/components/ui/navigation-menu';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import { Input } from '@/components/ui/input';
import {
  Bell,
  Bookmark,
  BookOpen,
  ChevronDown,
  Code,
  CreditCard,
  Download,
  Gamepad2,
  Globe,
  HelpCircle,
  LogOut,
  Menu,
  MessageSquare,
  Moon,
  Palette,
  Search,
  Settings,
  Shield,
  Star,
  Sun,
  TrendingUp,
  Trophy,
  User,
  Users,
  Zap,
} from 'lucide-react';

// Enhanced header component with user profile and settings
export default function EnhancedHeader() {
  const router = useRouter();
  const [isSearchFocused, setIsSearchFocused] = useState(false);
  const [notifications, setNotifications] = useState(3); // Mock notification count
  const [theme, setTheme] = useState<'light' | 'dark'>('light');

  // Mock user data
  const user = {
    name: 'Alex Johnson',
    email: 'alex.johnson@example.com',
    avatar: '/avatars/user.jpg',
    level: 'Pro Member',
    xp: 2450,
    nextLevelXp: 3000,
    completedCourses: 12,
    certificates: 8,
    currentStreak: 15,
    joinDate: 'March 2023',
  };

  const quickStats = [
    { label: 'Courses', value: user.completedCourses, icon: BookOpen },
    { label: 'Certificates', value: user.certificates, icon: Trophy },
    { label: 'Day Streak', value: user.currentStreak, icon: Zap },
  ];

  // Navigation items with enhanced structure
  const navigationItems = [
    {
      title: 'Learn',
      items: [
        {
          title: 'All Courses',
          href: '/courses',
          description: 'Browse our complete course library',
          icon: BookOpen,
        },
        {
          title: 'Learning Tracks',
          href: '/tracks',
          description: 'Structured learning paths for specific goals',
          icon: TrendingUp,
        },
        {
          title: 'Unity Development',
          href: '/courses/unity',
          description: 'Master Unity game development',
          icon: Gamepad2,
        },
        {
          title: 'Game Art & Design',
          href: '/courses/art',
          description: 'Create stunning game visuals',
          icon: Palette,
        },
        {
          title: 'Programming',
          href: '/courses/programming',
          description: 'Learn coding for games',
          icon: Code,
        },
      ],
    },
    {
      title: 'Community',
      items: [
        {
          title: 'Forums',
          href: '/community/forums',
          description: 'Connect with fellow learners',
          icon: MessageSquare,
        },
        {
          title: 'Study Groups',
          href: '/community/groups',
          description: 'Join collaborative learning groups',
          icon: Users,
        },
        {
          title: 'Showcases',
          href: '/community/showcases',
          description: 'Share your game projects',
          icon: Star,
        },
        {
          title: 'Mentorship',
          href: '/community/mentors',
          description: 'Get guidance from experts',
          icon: User,
        },
      ],
    },
    {
      title: 'Resources',
      items: [
        {
          title: 'Documentation',
          href: '/docs',
          description: 'Comprehensive guides and references',
          icon: BookOpen,
        },
        {
          title: 'Downloads',
          href: '/downloads',
          description: 'Assets, tools, and templates',
          icon: Download,
        },
        {
          title: 'Help Center',
          href: '/help',
          description: 'Get support and find answers',
          icon: HelpCircle,
        },
      ],
    },
  ];

  const toggleTheme = () => {
    setTheme((prev) => (prev === 'light' ? 'dark' : 'light'));
  };

  const handleLogout = () => {
    // Handle logout logic
    router.push('/login');
  };

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container mx-auto px-4">
        <div className="flex h-16 items-center justify-between">
          {/* Logo and Brand */}
          <div className="flex items-center gap-6">
            <Link href="/" className="flex items-center gap-2">
              <div className="bg-primary rounded-lg p-2">
                <Gamepad2 className="h-6 w-6 text-primary-foreground" />
              </div>
              <span className="font-bold text-xl">GameGuild</span>
            </Link>

            {/* Desktop Navigation */}
            <NavigationMenu className="hidden lg:flex">
              <NavigationMenuList>
                {navigationItems.map((section) => (
                  <NavigationMenuItem key={section.title}>
                    <NavigationMenuTrigger className="h-9">{section.title}</NavigationMenuTrigger>
                    <NavigationMenuContent>
                      <div className="grid w-[600px] gap-3 p-6 md:grid-cols-2">
                        {section.items.map((item) => (
                          <Link key={item.href} href={item.href} className="group grid grid-cols-[auto_1fr] gap-3 rounded-md p-3 hover:bg-accent transition-colors">
                            <div className="bg-primary/10 rounded-md p-2 group-hover:bg-primary/20 transition-colors">
                              <item.icon className="h-4 w-4 text-primary" />
                            </div>
                            <div className="space-y-1">
                              <p className="text-sm font-medium leading-none">{item.title}</p>
                              <p className="text-xs text-muted-foreground">{item.description}</p>
                            </div>
                          </Link>
                        ))}
                      </div>
                    </NavigationMenuContent>
                  </NavigationMenuItem>
                ))}
              </NavigationMenuList>
            </NavigationMenu>
          </div>

          {/* Enhanced Search */}
          <div className={`hidden md:flex flex-1 max-w-lg mx-8 transition-all duration-200 ${isSearchFocused ? 'scale-105' : ''}`}>
            <div className="relative w-full">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input type="text" placeholder="Search courses, tracks, or topics..." className="pl-9 pr-4 w-full" onFocus={() => setIsSearchFocused(true)} onBlur={() => setIsSearchFocused(false)} />
            </div>
          </div>

          {/* Right Side Actions */}
          <div className="flex items-center gap-2">
            {/* Search Icon for Mobile */}
            <Button variant="ghost" size="sm" className="md:hidden">
              <Search className="h-4 w-4" />
            </Button>

            {/* Notifications */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="relative">
                  <Bell className="h-4 w-4" />
                  {notifications > 0 && (
                    <Badge variant="destructive" className="absolute -top-1 -right-1 h-5 w-5 rounded-full p-0 text-xs flex items-center justify-center">
                      {notifications}
                    </Badge>
                  )}
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-80">
                <DropdownMenuLabel className="flex items-center justify-between">
                  Notifications
                  <Badge variant="secondary" className="text-xs">
                    {notifications} new
                  </Badge>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />

                {/* Sample notifications */}
                <div className="space-y-1">
                  <DropdownMenuItem className="flex items-start gap-3 p-3">
                    <div className="bg-blue-500/10 rounded-full p-2">
                      <BookOpen className="h-4 w-4 text-blue-600" />
                    </div>
                    <div className="flex-1 space-y-1">
                      <p className="text-sm font-medium">New course available</p>
                      <p className="text-xs text-muted-foreground">"Advanced Unity Scripting" is now live</p>
                      <p className="text-xs text-muted-foreground">2 hours ago</p>
                    </div>
                  </DropdownMenuItem>

                  <DropdownMenuItem className="flex items-start gap-3 p-3">
                    <div className="bg-green-500/10 rounded-full p-2">
                      <Trophy className="h-4 w-4 text-green-600" />
                    </div>
                    <div className="flex-1 space-y-1">
                      <p className="text-sm font-medium">Achievement unlocked!</p>
                      <p className="text-xs text-muted-foreground">You've completed your first learning track</p>
                      <p className="text-xs text-muted-foreground">1 day ago</p>
                    </div>
                  </DropdownMenuItem>

                  <DropdownMenuItem className="flex items-start gap-3 p-3">
                    <div className="bg-purple-500/10 rounded-full p-2">
                      <Users className="h-4 w-4 text-purple-600" />
                    </div>
                    <div className="flex-1 space-y-1">
                      <p className="text-sm font-medium">Study group invitation</p>
                      <p className="text-xs text-muted-foreground">Join "Unity Beginners" study group</p>
                      <p className="text-xs text-muted-foreground">3 days ago</p>
                    </div>
                  </DropdownMenuItem>
                </div>

                <DropdownMenuSeparator />
                <DropdownMenuItem className="justify-center text-sm text-primary">View all notifications</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>

            {/* Enhanced User Profile Menu */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="relative h-9 w-9 rounded-full">
                  <Avatar className="h-9 w-9">
                    <AvatarImage src={user.avatar} alt={user.name} />
                    <AvatarFallback>
                      {user.name
                        .split(' ')
                        .map((n) => n[0])
                        .join('')}
                    </AvatarFallback>
                  </Avatar>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="w-80" align="end" forceMount>
                {/* User Info Header */}
                <div className="flex items-center gap-3 p-4 bg-gradient-to-r from-primary/10 to-blue-500/10 rounded-t-md">
                  <Avatar className="h-12 w-12">
                    <AvatarImage src={user.avatar} alt={user.name} />
                    <AvatarFallback>
                      {user.name
                        .split(' ')
                        .map((n) => n[0])
                        .join('')}
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex-1">
                    <p className="font-medium">{user.name}</p>
                    <p className="text-sm text-muted-foreground">{user.email}</p>
                    <div className="flex items-center gap-2 mt-1">
                      <Badge variant="secondary" className="text-xs">
                        {user.level}
                      </Badge>
                    </div>
                  </div>
                </div>

                {/* Quick Stats */}
                <div className="grid grid-cols-3 gap-2 p-3 bg-muted/30">
                  {quickStats.map((stat) => (
                    <div key={stat.label} className="text-center">
                      <div className="flex items-center justify-center mb-1">
                        <stat.icon className="h-4 w-4 text-primary" />
                      </div>
                      <p className="text-lg font-bold">{stat.value}</p>
                      <p className="text-xs text-muted-foreground">{stat.label}</p>
                    </div>
                  ))}
                </div>

                {/* XP Progress */}
                <div className="p-3 border-b">
                  <div className="flex justify-between text-sm mb-1">
                    <span>XP Progress</span>
                    <span>
                      {user.xp} / {user.nextLevelXp}
                    </span>
                  </div>
                  <div className="w-full bg-secondary rounded-full h-2">
                    <div className="bg-primary h-2 rounded-full transition-all duration-300" style={{ width: `${(user.xp / user.nextLevelXp) * 100}%` }} />
                  </div>
                </div>

                <DropdownMenuSeparator />

                {/* Profile Actions */}
                <DropdownMenuItem asChild>
                  <Link href="/profile" className="cursor-pointer">
                    <User className="mr-2 h-4 w-4" />
                    <span>View Profile</span>
                  </Link>
                </DropdownMenuItem>

                <DropdownMenuItem asChild>
                  <Link href="/my-learning" className="cursor-pointer">
                    <BookOpen className="mr-2 h-4 w-4" />
                    <span>My Learning</span>
                  </Link>
                </DropdownMenuItem>

                <DropdownMenuItem asChild>
                  <Link href="/certificates" className="cursor-pointer">
                    <Trophy className="mr-2 h-4 w-4" />
                    <span>Certificates</span>
                  </Link>
                </DropdownMenuItem>

                <DropdownMenuItem asChild>
                  <Link href="/bookmarks" className="cursor-pointer">
                    <Bookmark className="mr-2 h-4 w-4" />
                    <span>Bookmarks</span>
                  </Link>
                </DropdownMenuItem>

                <DropdownMenuSeparator />

                {/* Settings Submenu */}
                <DropdownMenuSub>
                  <DropdownMenuSubTrigger>
                    <Settings className="mr-2 h-4 w-4" />
                    <span>Settings</span>
                  </DropdownMenuSubTrigger>
                  <DropdownMenuSubContent>
                    <DropdownMenuItem asChild>
                      <Link href="/settings/account">
                        <User className="mr-2 h-4 w-4" />
                        Account Settings
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem asChild>
                      <Link href="/settings/preferences">
                        <Settings className="mr-2 h-4 w-4" />
                        Preferences
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem asChild>
                      <Link href="/settings/privacy">
                        <Shield className="mr-2 h-4 w-4" />
                        Privacy & Security
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={toggleTheme}>
                      {theme === 'light' ? <Moon className="mr-2 h-4 w-4" /> : <Sun className="mr-2 h-4 w-4" />}
                      {theme === 'light' ? 'Dark Mode' : 'Light Mode'}
                    </DropdownMenuItem>
                    <DropdownMenuItem>
                      <Globe className="mr-2 h-4 w-4" />
                      Language
                      <ChevronDown className="ml-auto h-4 w-4" />
                    </DropdownMenuItem>
                  </DropdownMenuSubContent>
                </DropdownMenuSub>

                <DropdownMenuItem asChild>
                  <Link href="/billing" className="cursor-pointer">
                    <CreditCard className="mr-2 h-4 w-4" />
                    <span>Billing</span>
                  </Link>
                </DropdownMenuItem>

                <DropdownMenuItem asChild>
                  <Link href="/help" className="cursor-pointer">
                    <HelpCircle className="mr-2 h-4 w-4" />
                    <span>Help & Support</span>
                  </Link>
                </DropdownMenuItem>

                <DropdownMenuSeparator />

                <DropdownMenuItem onClick={handleLogout} className="text-red-600 focus:text-red-600">
                  <LogOut className="mr-2 h-4 w-4" />
                  <span>Log out</span>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>

            {/* Mobile Menu */}
            <Sheet>
              <SheetTrigger asChild>
                <Button variant="ghost" size="sm" className="lg:hidden">
                  <Menu className="h-4 w-4" />
                </Button>
              </SheetTrigger>
              <SheetContent side="right" className="w-80">
                <div className="flex flex-col h-full">
                  {/* Mobile Search */}
                  <div className="relative mb-6">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input type="text" placeholder="Search..." className="pl-9" />
                  </div>

                  {/* Mobile Navigation */}
                  <nav className="flex-1 space-y-4">
                    {navigationItems.map((section) => (
                      <div key={section.title}>
                        <h3 className="font-semibold text-sm text-muted-foreground uppercase tracking-wider mb-2">{section.title}</h3>
                        <div className="space-y-1">
                          {section.items.map((item) => (
                            <Link key={item.href} href={item.href} className="flex items-center gap-3 p-2 rounded-md hover:bg-accent transition-colors">
                              <item.icon className="h-4 w-4 text-primary" />
                              <div>
                                <p className="text-sm font-medium">{item.title}</p>
                                <p className="text-xs text-muted-foreground">{item.description}</p>
                              </div>
                            </Link>
                          ))}
                        </div>
                      </div>
                    ))}
                  </nav>
                </div>
              </SheetContent>
            </Sheet>
          </div>
        </div>
      </div>
    </header>
  );
}
