import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  const socialMedia = [
    {
      id: 'linkedin',
      name: 'LinkedIn',
      href: 'https://www.linkedin.com/company/gameguild/',
      img: 'https://img.icons8.com/?size=100&id=13930&format=png&color=000000',
      desc: 'Follow Game Guild updates and job postings on LinkedIn.'
    },
    {
      id: 'x',
      name: 'X',
      href: 'https://x.com/GameGuildDev',
      img: 'https://img.icons8.com/?size=100&id=phOKFKYpe00C&format=png&color=000000',
      desc: 'Follow us on X for quick updates and announcements.'
    },
    {
      id: 'instagram',
      name: 'Instagram',
      href: 'https://www.instagram.com/game.guild/',
      img: 'https://img.icons8.com/?size=100&id=zezJrErrmcwx&format=png&color=000000',
      desc: 'Check out our latest photos and stories on Instagram.'
    },
    {
      id: 'itchio',
      name: 'Itch.io',
      href: 'https://gameguild.itch.io/',
      img: 'https://img.icons8.com/?size=100&id=Ijo2Y6vd3VSf&format=png&color=fa5c5c',
      desc: 'Explore and download games made by our community on Itch.io.'
    },
  ];

  const userGroups = [
    {
      id: 'whatsapp',
      name: 'WhatsApp',
      href: 'https://chat.whatsapp.com/CAboWKtosP673f9EkzxKNb',
      img: 'https://img.icons8.com/color/48/000000/whatsapp.png',
      desc: 'Chat with our support and community on WhatsApp.'
    },
    {
      id: 'discord',
      name: 'Discord',
      href: 'https://discord.com/invite/9CdJeQ2XKB?ref=gameguild.gg',
      img: 'https://img.icons8.com/color/48/000000/discord-logo.png',
      desc: 'Join the guild on Discord to discuss projects and events.'
    },
  ];

  const otherContacts = [
    {
      id: 'email',
      name: 'Email',
      href: 'mailto:contact@gameguild.gg',
      img: 'https://img.icons8.com/?size=100&id=tiHbAqWU3ZCQ&format=png&color=000000',
      desc: 'Reach out to us via email for support or inquiries.'
    },
  ];

  return (
    <div className="min-h-screen bg-linear-to-br from-slate-900 via-slate-800 to-slate-900 py-16">
      <div className="container mx-auto max-w-3xl px-4">
        <div className="text-center max-w-2xl mx-auto mb-8">
          <Badge variant="outline" className="mb-4 text-sm font-medium bg-linear-to-r from-purple-500/20 to-blue-500/20 border-purple-500/30 text-purple-300">
            📬 Get in touch
          </Badge>
          <h1 className="text-3xl lg:text-4xl font-bold mb-2 bg-linear-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
            Contact & Links
          </h1>
          <p className="text-slate-400">Choose a channel to connect with the community or our team. Links open in a new tab.</p>
        </div>

        <Card className="bg-slate-800/50 border-slate-700">
          <CardContent className="p-6">
            <div className="grid gap-6">
              <div>
                <h3 className="text-sm font-semibold text-slate-300 mb-3">Social Media</h3>
                <div className="grid gap-4">
                  {socialMedia.map((l) => (
                    <a
                      key={l.id}
                      href={l.href}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="group flex items-center justify-between gap-4 p-3 rounded-lg bg-slate-900/40 border border-slate-700 hover:bg-slate-900/60 hover:scale-[1.01] transform transition-all duration-150 cursor-pointer focus:outline-none focus:ring-2 focus:ring-purple-500"
                    >
                      <div className="flex items-center gap-4">
                        <div className="h-12 w-12 rounded-md bg-blue-400/40 flex items-center justify-center overflow-hidden">
                          {l.img ? (
                            <img
                              src={l.img}
                              alt={`${l.name} icon`}
                              className="h-10 w-10 rounded-md object-cover"
                            />
                          ) : (
                            <div className="text-sm text-slate-300">Icon</div>
                          )}
                        </div>
                        <div>
                          <div className="text-white font-semibold">{l.name}</div>
                          <div className="text-slate-400 text-sm">{l.desc}</div>
                        </div>
                      </div>

                      <div className="shrink-0 ml-4 text-sm text-slate-300 group-hover:text-white">
                        Open
                        <span className="ml-2 text-slate-400 group-hover:text-white">→</span>
                      </div>
                    </a>
                  ))}
                </div>
              </div>

              <div>
                <h3 className="text-sm font-semibold text-slate-300 mb-3">User Groups</h3>
                <div className="grid gap-4">
                  {userGroups.map((l) => (
                    <a
                      key={l.id}
                      href={l.href}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="group flex items-center justify-between gap-4 p-3 rounded-lg bg-slate-900/40 border border-slate-700 hover:bg-slate-900/60 hover:scale-[1.01] transform transition-all duration-150 cursor-pointer focus:outline-none focus:ring-2 focus:ring-purple-500"
                    >
                      <div className="flex items-center gap-4">
                        <div className="h-12 w-12 rounded-md bg-blue-400/40 flex items-center justify-center overflow-hidden">
                          {l.img ? (
                            <img
                              src={l.img}
                              alt={`${l.name} icon`}
                              className="h-10 w-10 rounded-md object-cover"
                            />
                          ) : (
                            <div className="text-sm text-slate-300">Icon</div>
                          )}
                        </div>
                        <div>
                          <div className="text-white font-semibold">{l.name}</div>
                          <div className="text-slate-400 text-sm">{l.desc}</div>
                        </div>
                      </div>

                      <div className="shrink-0 ml-4 text-sm text-slate-300 group-hover:text-white">
                        Open
                        <span className="ml-2 text-slate-400 group-hover:text-white">→</span>
                      </div>
                    </a>
                  ))}
                </div>
              </div>

              <div>
                <h3 className="text-sm font-semibold text-slate-300 mb-3">Contact</h3>
                <div className="grid gap-4">
                  {otherContacts.map((l) => (
                    <a
                      key={l.id}
                      href={l.href}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="group flex items-center justify-between gap-4 p-3 rounded-lg bg-slate-900/40 border border-slate-700 hover:bg-slate-900/60 hover:scale-[1.01] transform transition-all duration-150 cursor-pointer focus:outline-none focus:ring-2 focus:ring-purple-500"
                    >
                      <div className="flex items-center gap-4">
                        <div className="h-12 w-12 rounded-md bg-blue-400/40 flex items-center justify-center overflow-hidden">
                          {l.img ? (
                            <img
                              src={l.img}
                              alt={`${l.name} icon`}
                              className="h-10 w-10 rounded-md object-cover"
                            />
                          ) : (
                            <div className="text-sm text-slate-300">Icon</div>
                          )}
                        </div>
                        <div>
                          <div className="text-white font-semibold">{l.name}</div>
                          <div className="text-slate-400 text-sm">{l.desc}</div>
                        </div>
                      </div>

                      <div className="shrink-0 ml-4 text-sm text-slate-300 group-hover:text-white">
                        Open
                        <span className="ml-2 text-slate-400 group-hover:text-white">→</span>
                      </div>
                    </a>
                  ))}
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

      </div>
    </div>
  );
}
