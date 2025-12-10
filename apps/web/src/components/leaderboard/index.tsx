/**
 * Stub exports for leaderboard module.
 * This component is disabled in production.
 */

'use client';


export interface LeaderboardEntry {
    rank: number;
    userId: string;
    username: string;
    score: number;
    avatar?: string;
}

export interface LeaderboardProps {
    entries?: LeaderboardEntry[];
    title?: string;
    className?: string;
}

export function Leaderboard({ entries = [], title, className }: LeaderboardProps) {
    return (
        <div className={className}>
            {title && <h3 className="text-lg font-semibold mb-4">{title}</h3>}
            <div className="text-slate-500 text-sm">
                <p>Leaderboard disabled</p>
                <p>{entries.length} entries</p>
            </div>
        </div>
    );
}

export function LeaderboardWidget(props: LeaderboardProps) {
    return <Leaderboard {...props} />;
}

export default Leaderboard;
