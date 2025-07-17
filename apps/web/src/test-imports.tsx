// Test imports to identify which component is undefined
import { ContributorsHeader, TopContributorsSection, GlobalRankingTable, ProjectStats, HorizontalRoadmap, HowToContribute } from '@/components/contributors';

console.log('ContributorsHeader:', ContributorsHeader);
console.log('TopContributorsSection:', TopContributorsSection);
console.log('GlobalRankingTable:', GlobalRankingTable);
console.log('ProjectStats:', ProjectStats);
console.log('HorizontalRoadmap:', HorizontalRoadmap);
console.log('HowToContribute:', HowToContribute);

export default function TestImports() {
  return <div>Testing imports...</div>;
}
