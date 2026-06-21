export interface PublicProject {
  slug: string;
  title: string;
  creator: string;
  creatorRole: string;
  summary: string;
  description: string;
  status: 'Open playtest' | 'In review' | 'Showcase ready';
  tags: string[];
  coursePath: string;
  accent: string;
  previewImage: string;
  buildType: string;
  feedbackGoal: string;
  metrics: Array<{ label: string; value: string }>;
  media: Array<{ label: string; detail: string }>;
}

export interface PublicMemberSpotlight {
  name: string;
  handle: string;
  role: string;
  focus: string;
  contribution: string;
}

export interface PublicPlaytest {
  title: string;
  date: string;
  format: string;
  seats: string;
  href: string;
}

export interface PublicActivity {
  actor: string;
  action: string;
  target: string;
  href: string;
}

export const publicProjects: PublicProject[] = [
  {
    slug: 'skybound-courier',
    title: 'Skybound Courier',
    creator: 'Maya Torres',
    creatorRole: 'Gameplay programmer',
    summary: 'A traversal prototype about delivering packages through floating islands with glider physics.',
    description:
      'Skybound Courier is a compact movement prototype focused on readable air control, route planning, and level-flow feedback. The current build is looking for movement feel, checkpoint clarity, and onboarding notes.',
    status: 'Open playtest',
    tags: ['Traversal', 'Prototype', 'Unity', 'Portfolio'],
    coursePath: 'Game Programming Foundations',
    accent: 'from-sky-400/30 via-cyan-300/10 to-slate-950',
    previewImage: 'https://images.unsplash.com/photo-1511512578047-dfb367046420?w=1400&h=900&fit=crop',
    buildType: 'WebGL prototype',
    feedbackGoal: 'Validate movement readability and route pacing before adding progression systems.',
    metrics: [
      { label: 'Build', value: '0.7' },
      { label: 'Target', value: '15 min' },
      { label: 'Need', value: '12 testers' },
    ],
    media: [
      { label: 'Gameplay loop', detail: 'Glide, land, deliver, upgrade route timing.' },
      { label: 'Current risk', detail: 'Players may miss thermal lift affordances.' },
    ],
  },
  {
    slug: 'echoes-of-iron',
    title: 'Echoes of Iron',
    creator: 'Ren Okafor',
    creatorRole: 'Technical designer',
    summary: 'A tactical arena focused on influence-map enemy decisions and readable pressure zones.',
    description:
      'Echoes of Iron turns the AI systems path into a visible combat prototype. The team is testing whether players understand pressure, retreat, and flank behavior without tutorial text.',
    status: 'In review',
    tags: ['AI', 'Tactics', 'Unreal', 'Systems'],
    coursePath: 'Game AI & Systems Programming',
    accent: 'from-violet-400/30 via-fuchsia-300/10 to-slate-950',
    previewImage: 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop',
    buildType: 'Windows build',
    feedbackGoal: 'Measure whether enemy behavior reads as intentional under pressure.',
    metrics: [
      { label: 'Agents', value: '6' },
      { label: 'Maps', value: '3' },
      { label: 'Reports', value: '18' },
    ],
    media: [
      { label: 'AI showcase', detail: 'Influence maps, flank scoring, and retreat thresholds.' },
      { label: 'Current risk', detail: 'Difficulty spikes during the second arena wave.' },
    ],
  },
  {
    slug: 'lantern-market',
    title: 'Lantern Market',
    creator: 'Iris Chen',
    creatorRole: 'Producer and UI designer',
    summary: 'A cozy shop-management slice with economy tuning, narrative events, and portfolio case-study notes.',
    description:
      'Lantern Market is being prepared as a portfolio-ready case study. The next pass focuses on shop UI clarity, event pacing, and whether the public project page explains the team decisions well.',
    status: 'Showcase ready',
    tags: ['Economy', 'UI', 'Production', 'Case study'],
    coursePath: 'Portfolio & Professional Presentation',
    accent: 'from-amber-300/30 via-orange-300/10 to-slate-950',
    previewImage: 'https://images.unsplash.com/photo-1556075798-4825dfaaf498?w=1400&h=900&fit=crop',
    buildType: 'Video walkthrough',
    feedbackGoal: 'Polish the story around design tradeoffs and production constraints.',
    metrics: [
      { label: 'Milestones', value: '5' },
      { label: 'Clips', value: '4' },
      { label: 'Reviews', value: '9' },
    ],
    media: [
      { label: 'Case study', detail: 'Economy loop, UI decisions, and scoped production plan.' },
      { label: 'Current risk', detail: 'Screenshots need stronger before-and-after framing.' },
    ],
  },
];

export const publicMembers: PublicMemberSpotlight[] = [
  {
    name: 'Maya Torres',
    handle: '@mayat',
    role: 'Gameplay programmer',
    focus: 'Movement systems',
    contribution: 'Hosts weekly traversal critique for prototype builders.',
  },
  {
    name: 'Ren Okafor',
    handle: '@ren-ai',
    role: 'Technical designer',
    focus: 'Game AI',
    contribution: 'Shares tactical AI breakdowns and playtest notes.',
  },
  {
    name: 'Iris Chen',
    handle: '@irisbuilds',
    role: 'Producer',
    focus: 'Portfolio framing',
    contribution: 'Reviews student case studies before public launch.',
  },
];

export const publicPlaytests: PublicPlaytest[] = [
  {
    title: 'Movement feel review',
    date: 'Tuesday, 7 PM UTC',
    format: 'Live Discord session',
    seats: '8 open seats',
    href: '/testing-lab',
  },
  {
    title: 'AI readability pass',
    date: 'Thursday, 6 PM UTC',
    format: 'Async build review',
    seats: '5 open seats',
    href: '/projects/echoes-of-iron',
  },
  {
    title: 'Portfolio case-study critique',
    date: 'Saturday, 3 PM UTC',
    format: 'Peer review board',
    seats: '12 open seats',
    href: '/community',
  },
];

export const publicActivities: PublicActivity[] = [
  {
    actor: 'Maya Torres',
    action: 'opened a playtest request for',
    target: 'Skybound Courier',
    href: '/projects/skybound-courier',
  },
  {
    actor: 'Ren Okafor',
    action: 'published tactical AI notes from',
    target: 'Echoes of Iron',
    href: '/projects/echoes-of-iron',
  },
  {
    actor: 'Iris Chen',
    action: 'updated the portfolio story for',
    target: 'Lantern Market',
    href: '/projects/lantern-market',
  },
];

export const communityGroups = [
  {
    name: 'Prototype critique',
    description: 'Weekly feedback for playable slices, rough mechanics, and early UX signals.',
    members: '248 members',
  },
  {
    name: 'AI and systems',
    description: 'Technical reviews for agent behavior, simulation, pathfinding, and tooling.',
    members: '136 members',
  },
  {
    name: 'Launch desk',
    description: 'Store pages, release checklists, trailer reviews, and publishing operations.',
    members: '91 members',
  },
] as const;

export const communityOpportunities = [
  {
    title: 'Playtest mentor',
    type: 'Volunteer',
    commitment: '2 hours/week',
    description: 'Guide testers toward useful feedback and summarize findings for project teams.',
  },
  {
    title: 'Course project reviewer',
    type: 'Community role',
    commitment: 'Async',
    description: 'Review student milestones and help turn course work into portfolio proof.',
  },
  {
    title: 'Launch checklist contributor',
    type: 'Open source',
    commitment: 'Issue-based',
    description: 'Improve publishing templates, platform notes, and release-readiness checklists.',
  },
] as const;

export function getPublicProject(slug: string) {
  return publicProjects.find((project) => project.slug === slug);
}
