'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { getSession } from 'next-auth/react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Bell, ChevronDown, ChevronUp, Globe, Menu, Search, X } from 'lucide-react';

/**
 * Main Header
 */
export default function Header() {
  const [user, setUser] = useState<any>(null);
  const [isSearchOpen, setIsSearchOpen] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isMoreOpen, setIsMoreOpen] = useState(false);
  const [isComplexOpen, setIsComplexOpen] = useState(false);

  const menuItems = ['Blog', 'Courses']; // ToDo: Jams, Events, Jobs, Tests, Projects
  const moreItems = ['Roadmap', 'Contributors', 'Issues', 'Code of Conduct', 'Security', 'Privacy Policy', 'Terms of Service', 'Contact', 'About', 'License'];

  const complexItems = [
    { name: 'Learn', href: `/learn` },
    {
      name: 'Code',
      href: '/learn/coding-environment?id=0&type=sandbox&userId=&role=',
    },
  ];

  const languages = ['English', 'Spanish', 'Portuguese', 'French', 'German'];

  const router = useRouter();

  useEffect(() => {
    // Only fetch the session once when the component mounts
    // Next.js will handle session updates through its built-in mechanisms
    // No need for polling which can cause performance issues
    getUserData();
  }, []); // Only run once on mount

  const getUserData = async () => {
    try {
      const session = await getSession();
      if (session && session.user) {
        setUser(session.user);
      } else {
        setUser(null);
      }
    } catch (error) {
      console.error('Error getting session:', error);
      setUser(null);
    }
  };

  return (
    <header className="bg-neutral-900 h-[70px] flex items-center justify-between px-4 text-white">
      <div className="flex items-center">
        <Link href="/" className="mr-4">
          <img src="/assets/images/logo-text.png" className="h-[40px] my-auto mx-[10px]" />
        </Link>
        <nav className="hidden md:flex space-x-2 h-[50px] p-0">
          {menuItems.map((item) => (
            <button key={item} onClick={() => router.push(`/${item.toLowerCase()}`)} className="px-2 my-0 hover:text-gray-400 transition-colors h-full">
              {item}
            </button>
          ))}
          <DropdownMenu open={isMoreOpen} onOpenChange={setIsMoreOpen}>
            <DropdownMenuTrigger className="flex px-2 items-center hover:text-gray-400 transition-colors">Institutional {isMoreOpen ? <ChevronUp className="ml-1 h-4 w-4" /> : <ChevronDown className="ml-1 h-4 w-4" />}</DropdownMenuTrigger>
            <DropdownMenuContent className="bg-neutral-900 text-white border-0">
              {moreItems.map((item) => (
                <Link key={item} href={`/` + item.toLowerCase().replace(/\s/g, '')}>
                  <DropdownMenuItem>{item}</DropdownMenuItem>
                </Link>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
          <DropdownMenu open={isComplexOpen} onOpenChange={setIsComplexOpen}>
            <DropdownMenuTrigger className="flex px-2 items-center hover:text-gray-400 transition-colors">Learn {isMoreOpen ? <ChevronUp className="ml-1 h-4 w-4" /> : <ChevronDown className="ml-1 h-4 w-4" />}</DropdownMenuTrigger>
            <DropdownMenuContent className="bg-neutral-900 text-white border-0">
              {complexItems.map((item) => (
                <DropdownMenuItem key={item.name}>
                  <Link href={item.href} className=" hover:text-gray-400 transition-colors">
                    {item.name}
                  </Link>
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        </nav>
      </div>
      <div className="hidden md:flex items-center space-x-4">
        {isSearchOpen ? (
          <div className="relative text-white w-80">
            <Input type="text" placeholder="Search..." className="pl-10 pr-10 py-2 rounded-full bg-neutral-800 text-white placeholder:text-gray-200" />
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-200" />
            <button onClick={() => setIsSearchOpen(false)} className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-200 hover:text-white">
              <X className="h-4 w-4" />
            </button>
          </div>
        ) : (
          <button onClick={() => setIsSearchOpen(true)} className="hover:text-gray-200 transition-colors">
            <Search />
          </button>
        )}
        <DropdownMenu>
          <DropdownMenuTrigger className="hover:text-gray-400 transition-colors">
            <Globe />
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            {languages.map((lang) => (
              <DropdownMenuItem key={lang}>{lang}</DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
        {user ? (
          <div className="flex items-center space-x-2">
            <DropdownMenu>
              <DropdownMenuTrigger className="hover:text-gray-400 transition-colors">
                <Bell />
              </DropdownMenuTrigger>
              <DropdownMenuContent>
                <DropdownMenuLabel>Empty</DropdownMenuLabel>
              </DropdownMenuContent>
            </DropdownMenu>
            <DropdownMenu>
              <DropdownMenuTrigger>
                {/*<img src={user.avatar} alt={user.name[0]} className="w-8 h-8 rounded-full" />*/}
                <Avatar className="mx-2">
                  <AvatarImage src={user.image} alt={user.name} />
                  <AvatarFallback className="bg-neutral-50 text-neutral-950">{user.name[0]}</AvatarFallback>
                </Avatar>
              </DropdownMenuTrigger>
              <DropdownMenuContent>
                <DropdownMenuItem onClick={() => router.push('/profile/edit')}>Profile</DropdownMenuItem>
                <DropdownMenuItem>Settings</DropdownMenuItem>
                <DropdownMenuItem
                  onClick={
                    // redirect to /disconnect
                    async () => {
                      router.push('/disconnect');
                    }
                  }
                >
                  Logout
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        ) : (
          <Button onClick={() => router.push('/connect')} variant="outline" className="bg-white text-black font-semibold text-base rounded-md hover:bg-gray-200">
            Connect
          </Button>
        )}
      </div>
      <div className="md:hidden">
        <button onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)} className="text-white items-center">
          {isMobileMenuOpen ? <X size={42} /> : <Menu size={42} />}
        </button>
      </div>
      {isMobileMenuOpen && (
        <div className="absolute top-[70px] left-0 right-0 bg-neutral-900 p-4 md:hidden z-30">
          <div className="flex flex-col space-y-4">
            <Input type="text" placeholder="Search..." className="pl-10 pr-10 py-2 rounded-full bg-neutral-800 text-white" />
            <DropdownMenu>
              <DropdownMenuTrigger className="flex items-center justify-between w-full">
                <span>Language</span>
                <Globe />
              </DropdownMenuTrigger>
              <DropdownMenuContent>
                {languages.map((lang) => (
                  <DropdownMenuItem key={lang}>{lang}</DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
            {user ? (
              <>
                <DropdownMenu>
                  <DropdownMenuTrigger className="flex items-center justify-between w-full">
                    <span>Notifications</span>
                    <Bell />
                  </DropdownMenuTrigger>
                  <DropdownMenuContent>
                    <DropdownMenuItem>Notification 1</DropdownMenuItem>
                    <DropdownMenuItem>Notification 2</DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
                <DropdownMenu>
                  <DropdownMenuTrigger className="flex items-center space-x-2">
                    <img src={user.avatar} alt={user.name} className="w-8 h-8 rounded-full" />
                    <span>{user.name}</span>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent>
                    <DropdownMenuItem>Profile</DropdownMenuItem>
                    <DropdownMenuItem>Settings</DropdownMenuItem>
                    <DropdownMenuItem>Logout</DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </>
            ) : (
              <Button onClick={() => router.push('/connect')} variant="outline" className="bg-white text-black font-semibold rounded-md hover:bg-gray-200 w-full">
                Connect
              </Button>
            )}
            {menuItems.map((item) => (
              <Link key={item} href={`/${item.toLowerCase()}`} className="hover:text-gray-400 transition-colors">
                {item}
              </Link>
            ))}
            <DropdownMenu>
              <DropdownMenuTrigger className="flex items-center justify-between w-full">
                <span>More</span>
                <ChevronDown className="h-4 w-4" />
              </DropdownMenuTrigger>
              <DropdownMenuContent className="bg-neutral-900 text-white">
                {moreItems.map((item) => (
                  <DropdownMenuItem key={item}>{item}</DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
      )}
    </header>
  );
}
