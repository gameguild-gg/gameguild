'use client';

import { Snowflake, Trophy, Users } from 'lucide-react';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Avatar, AvatarFallback, AvatarImage } from '@game-guild/ui/components/avatar';

interface LeaderboardUser {
  id: string;
  rank: number;
  name: string;
  username: string;
  avatar?: string;
  initials: string;
  wins: number;
  matches: number;
  victories: number;
  bestWinTime: string;
  points: number;
  tokens: {
    fire: number;
    diamond: number;
  };
  isTopPlayer?: boolean;
}

interface LeaderboardStats {
  totalRegistered: number;
  totalParticipated: number;
  remainingTime: {
    days: number;
    hours: number;
    minutes: number;
  };
}

export function Leaderboard() {
  const stats: LeaderboardStats = {
    totalRegistered: 1277,
    totalParticipated: 255,
    remainingTime: {
      days: 12,
      hours: 6,
      minutes: 42,
    },
  };

  const topPlayers: LeaderboardUser[] = [
    {
      id: '1',
      rank: 1,
      name: 'Blademir Malina Tori',
      username: '@popy_bob',
      initials: 'BM',
      wins: 443,
      matches: 778,
      victories: 43,
      bestWinTime: '1:05',
      points: 44872,
      tokens: {
        fire: 32421,
        diamond: 17500,
      },
      isTopPlayer: true,
    },
    {
      id: '2',
      rank: 2,
      name: 'Robert Fox',
      username: '@robert_fox',
      initials: 'RF',
      wins: 440,
      matches: 887,
      victories: 43,
      bestWinTime: '1:03',
      points: 42515,
      tokens: {
        fire: 31001,
        diamond: 17421,
      },
    },
    {
      id: '3',
      rank: 3,
      name: 'Molida Glinda',
      username: '@molida_glinda',
      initials: 'MG',
      wins: 412,
      matches: 756,
      victories: 43,
      bestWinTime: '1:15',
      points: 40550,
      tokens: {
        fire: 30987,
        diamond: 17224,
      },
    },
  ];

  const allPlayers: LeaderboardUser[] = [
    {
      id: '1',
      rank: 1,
      name: 'Blademir Malina Tori',
      username: 'ID 1587607',
      initials: 'BM',
      wins: 443,
      matches: 778,
      victories: 43,
      bestWinTime: '1:05',
      points: 44872,
      tokens: { fire: 0, diamond: 0 },
    },
    {
      id: '2',
      rank: 2,
      name: 'Robert Fox',
      username: 'ID 1587634',
      initials: 'RF',
      wins: 440,
      matches: 887,
      victories: 43,
      bestWinTime: '1:03',
      points: 42515,
      tokens: { fire: 0, diamond: 0 },
    },
    {
      id: '3',
      rank: 3,
      name: 'Molida Glinda',
      username: 'ID 1587603',
      initials: 'MG',
      wins: 412,
      matches: 756,
      victories: 43,
      bestWinTime: '1:15',
      points: 40550,
      tokens: { fire: 0, diamond: 0 },
    },
  ];

  const StatCard = ({
    icon,
    value,
    label,
    color = 'text-muted-foreground',
  }: {
    icon: React.ReactNode;
    value: string | number;
    label: string;
    color?: string;
  }) => (
    <Card className="bg-card border-border">
      <CardContent className="p-6">
        <div className="flex items-center gap-3">
          <div className={`p-2 rounded-lg ${color === 'text-orange-500' ? 'bg-orange-500/10' : 'bg-muted/10'}`}>
            <div className={color}>{icon}</div>
          </div>
          <div>
            <div className="text-2xl font-bold text-foreground">{value}</div>
            <div className="text-sm text-muted-foreground">{label}</div>
          </div>
        </div>
      </CardContent>
    </Card>
  );

  const TopPlayerCard = ({ player }: { player: LeaderboardUser }) => (
    <Card
      className={`relative overflow-hidden ${player.isTopPlayer ? 'ring-2 ring-yellow-500 bg-gradient-to-br from-yellow-500/5 to-orange-500/5' : 'bg-card'} border-border`}
    >
      <CardContent className="p-6">
        {player.isTopPlayer && (
          <div className="absolute top-4 right-4">
            <div className="bg-yellow-500 p-2 rounded-full">
              <Trophy className="w-4 h-4 text-white" />
            </div>
          </div>
        )}

        <div className="flex items-start gap-4">
          <div className="relative">
            <Avatar className="w-16 h-16 border-2 border-background">
              <AvatarImage src={player.avatar} alt={player.name} />
              <AvatarFallback className="bg-gradient-to-br from-purple-500 to-pink-500 text-white font-bold">{player.initials}</AvatarFallback>
            </Avatar>
            <div className="absolute -bottom-1 -right-1 bg-background rounded-full p-1">
              <div className="w-6 h-6 bg-green-500 rounded-full flex items-center justify-center text-white font-bold text-xs">{player.rank}</div>
            </div>
          </div>

          <div className="flex-1 min-w-0">
            <h3 className="font-bold text-foreground text-lg leading-tight">{player.name}</h3>
            <p className="text-sm text-muted-foreground mb-4">{player.username}</p>

            <div className="grid grid-cols-3 gap-4 text-center">
              <div>
                <div className="text-xs text-muted-foreground mb-1">WINS</div>
                <div className="text-lg font-bold text-foreground">{player.wins}</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">MATCHES</div>
                <div className="text-lg font-bold text-foreground">{player.matches}</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">POINTS</div>
                <div className="text-lg font-bold text-foreground">{player.points.toLocaleString()}</div>
              </div>
            </div>

            <div className="flex items-center justify-center gap-6 mt-4 pt-4 border-t border-border">
              <div className="flex items-center gap-2">
                <div className="w-4 h-4 bg-orange-500 rounded"></div>
                <span className="text-sm font-medium text-foreground">{player.tokens.fire.toLocaleString()}</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-4 h-4 bg-blue-500 rounded-sm">
                  <Snowflake className="w-3 h-3 text-white m-0.5" />
                </div>
                <span className="text-sm font-medium text-foreground">{player.tokens.diamond.toLocaleString()}</span>
              </div>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );

  return (
    <div className="bg-background text-foreground p-6 space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-foreground mb-8">Leaderboard</h1>

        {/* Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <StatCard icon={<Users className="w-5 h-5" />} value={stats.totalRegistered} label="Total Registered" color="text-green-500" />
          <StatCard icon={<Snowflake className="w-5 h-5" />} value={stats.totalParticipated} label="Total Participated" color="text-blue-500" />
          <Card className="bg-card border-border">
            <CardContent className="p-6">
              <div className="text-center">
                <div className="text-sm text-muted-foreground mb-2 flex items-center justify-center gap-2">
                  Remaining time for completion
                  <div className="w-2 h-2 bg-orange-500 rounded-full"></div>
                </div>
                <div className="text-2xl font-bold text-foreground">
                  {stats.remainingTime.days} : {stats.remainingTime.hours.toString().padStart(2, '0')} :{' '}
                  {stats.remainingTime.minutes.toString().padStart(2, '0')}
                </div>
                <div className="text-xs text-muted-foreground mt-1">DAYS &nbsp;&nbsp;&nbsp;&nbsp;&nbsp; HRS &nbsp;&nbsp;&nbsp;&nbsp;&nbsp; MINS</div>
                <div className="text-xs text-muted-foreground mt-2">Only the first three positions will be awarded prizes</div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Top Players Cards */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {topPlayers.map((player) => (
          <TopPlayerCard key={player.id} player={player} />
        ))}
      </div>

      {/* Global Ranking Table */}
      <div className="space-y-4">
        <h2 className="text-2xl font-bold text-foreground">Global Ranking</h2>

        <Card className="bg-card border-border">
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b border-border">
                  <tr className="text-left">
                    <th className="p-4 text-sm font-medium text-muted-foreground">Rank</th>
                    <th className="p-4 text-sm font-medium text-muted-foreground">User name</th>
                    <th className="p-4 text-sm font-medium text-muted-foreground">Match Wins</th>
                    <th className="p-4 text-sm font-medium text-muted-foreground">Spent time</th>
                    <th className="p-4 text-sm font-medium text-muted-foreground">Victories</th>
                    <th className="p-4 text-sm font-medium text-muted-foreground">Best Win (mins)</th>
                    <th className="p-4 text-sm font-medium text-muted-foreground">Points</th>
                  </tr>
                </thead>
                <tbody>
                  {allPlayers.map((player, index) => (
                    <tr key={player.id} className={`border-b border-border last:border-b-0 hover:bg-muted/20 ${index === 0 ? 'bg-yellow-500/5' : ''}`}>
                      <td className="p-4">
                        <div className="flex items-center gap-2">
                          <div
                            className={`w-6 h-6 rounded-full flex items-center justify-center text-white text-sm font-bold ${
                              index === 0 ? 'bg-yellow-500' : index === 1 ? 'bg-gray-400' : 'bg-orange-600'
                            }`}
                          >
                            {player.rank}
                          </div>
                        </div>
                      </td>
                      <td className="p-4">
                        <div className="flex items-center gap-3">
                          <Avatar className="w-8 h-8">
                            <AvatarImage src={player.avatar} alt={player.name} />
                            <AvatarFallback
                              className={`text-white text-xs font-bold ${index === 0 ? 'bg-red-500' : index === 1 ? 'bg-green-500' : 'bg-red-500'}`}
                            >
                              {player.initials}
                            </AvatarFallback>
                          </Avatar>
                          <div>
                            <div className="font-medium text-foreground">{player.name}</div>
                            <div className="text-xs text-muted-foreground">{player.username}</div>
                          </div>
                        </div>
                      </td>
                      <td className="p-4 text-foreground font-medium">{player.wins}</td>
                      <td className="p-4 text-foreground">{player.matches}</td>
                      <td className="p-4 text-foreground">{player.victories}</td>
                      <td className="p-4 text-foreground">{player.bestWinTime}</td>
                      <td className="p-4 text-foreground font-bold">{player.points.toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
