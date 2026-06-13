import type { Program, ProgramContent } from '@/lib/api/generated';

export type ProgramArea = 'ai-systems' | 'programming' | 'production' | 'portfolio' | 'data';

export interface PublicProgramPackage {
  slug: string;
  title: string;
  shortTitle: string;
  area: ProgramArea;
  summary: string;
  longDescription: string;
  audience: string;
  level: string;
  duration: string;
  format: string;
  image: string;
  accent: string;
  courseSlugs: string[];
  outcomes: string[];
  tools: string[];
  portfolioResult: string;
}

export interface CourseProjectShowcase {
  title: string;
  summary: string;
  image: string;
  skills: string[];
  deliverable: string;
  moduleLabel: string;
}

export interface CourseJourneyStep {
  label: string;
  title: string;
  body: string;
  checkpoint: string;
  projectTitle: string;
  minutes?: number | null;
  type?: number | null;
}

export interface CourseShowcase {
  slug: string;
  programSlug: string;
  headline: string;
  studioPrompt: string;
  projectResult: string;
  instructorModel: string;
  portfolioProof: string;
  outcomes: string[];
  prerequisites: string[];
  projects?: CourseProjectShowcase[];
  journey?: CourseJourneyStep[];
  faq: Array<{ question: string; answer: string }>;
}

export const PUBLIC_PROGRAM_PACKAGES: PublicProgramPackage[] = [
  {
    slug: 'game-ai-systems',
    title: 'Game AI & Systems Programming',
    shortTitle: 'AI & Systems',
    area: 'ai-systems',
    summary: 'Build believable agents, tactical decision systems, procedural tools, and runtime AI features for playable games.',
    longDescription:
      'A production-minded AI path for students who want to move beyond isolated algorithms and ship game-ready behavior. The package starts with AI fundamentals, then pushes into pathfinding, tactical reasoning, simulation, procedural content, and network-aware systems.',
    audience: 'Gameplay programmers, technical designers, AI engineers, and advanced students building portfolio-ready systems.',
    level: 'Intermediate to advanced',
    duration: '73 estimated hours',
    format: 'Project-based lessons, technical breakdowns, and capstone implementation work',
    image: 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop',
    accent: 'cyan',
    courseSlugs: ['ai4games', 'ai4games2', 'networking'],
    outcomes: [
      'Implement navigation, steering, tactical positioning, and agent decision loops.',
      'Design multiplayer-safe systems and data flows for real-time games.',
      'Produce a technical portfolio piece that demonstrates AI behavior in context.',
    ],
    tools: ['C++', 'Unity', 'C#', 'Pathfinding', 'Behavior Trees', 'Networking'],
    portfolioResult: 'A playable AI systems prototype with documented design decisions and implementation notes.',
  },
  {
    slug: 'game-programming-foundations',
    title: 'Game Programming Foundations',
    shortTitle: 'Programming',
    area: 'programming',
    summary: 'A structured foundation for students who need programming confidence before entering advanced game development work.',
    longDescription:
      'This package builds durable programming habits through Python, data structures, algorithms, databases, and introductory game programming. It is designed as the practical bridge between beginner coding and production game development courses.',
    audience: 'Beginners, career switchers, technical artists, designers, and students preparing for programming-heavy game courses.',
    level: 'Beginner to intermediate',
    duration: '142 estimated hours',
    format: 'Guided fundamentals, coding labs, technical assignments, and applied projects',
    image: 'https://images.unsplash.com/photo-1515879218367-8466d910aaa4?w=1400&h=900&fit=crop',
    accent: 'blue',
    courseSlugs: ['python', 'dsa', 'databases', 'intro2gpro'],
    outcomes: [
      'Write maintainable code using clear control flow, data structures, and debugging habits.',
      'Understand databases and data modeling for game and platform features.',
      'Build small gameplay systems that prepare students for engine-level work.',
    ],
    tools: ['Python', 'SQL', 'Algorithms', 'Data Structures', 'Game Loops'],
    portfolioResult: 'A small playable game/programming project plus supporting technical exercises.',
  },
  {
    slug: 'launch-and-publishing',
    title: 'Launch & Publishing Pipeline',
    shortTitle: 'Launch',
    area: 'production',
    summary: 'Take a game from finished build to public release with store readiness, marketing assets, and launch operations.',
    longDescription:
      'A publishing package for builders who need practical release literacy. Students learn platform requirements, store pages, distribution workflows, launch checklists, and production communication needed to ship beyond a private build.',
    audience: 'Indie teams, producers, technical founders, and students preparing to publish portfolio or commercial games.',
    level: 'Intermediate',
    duration: '25 estimated hours',
    format: 'Launch checklists, platform guides, release planning, and publishing operations',
    image: 'https://images.unsplash.com/photo-1556075798-4825dfaaf498?w=1400&h=900&fit=crop',
    accent: 'violet',
    courseSlugs: ['game-publishing'],
    outcomes: [
      'Prepare store assets and submission flows for major desktop, mobile, console, and web platforms.',
      'Understand launch requirements, release communication, and storefront positioning.',
      'Build a reusable publishing checklist for current and future projects.',
    ],
    tools: ['Steam', 'Itch.io', 'Google Play', 'App Store', 'Console Stores', 'Release Planning'],
    portfolioResult: 'A publish-ready release plan with platform-specific submission assets and launch checklist.',
  },
  {
    slug: 'portfolio-and-career',
    title: 'Portfolio & Professional Presentation',
    shortTitle: 'Portfolio',
    area: 'portfolio',
    summary: 'Turn scattered work into a focused portfolio narrative that helps reviewers understand your craft and growth.',
    longDescription:
      'A portfolio package centered on selection, framing, critique, and presentation. It helps students turn class projects and prototypes into public-facing evidence of ability, taste, iteration, and communication.',
    audience: 'Students preparing internship applications, career switchers, and builders who need a stronger public proof layer.',
    level: 'Beginner to intermediate',
    duration: '20 estimated hours',
    format: 'Portfolio reviews, project framing, documentation, and presentation polish',
    image: 'https://images.unsplash.com/photo-1497366754035-f200968a6e72?w=1400&h=900&fit=crop',
    accent: 'amber',
    courseSlugs: ['portfolio'],
    outcomes: [
      'Select and sequence projects for a clear professional story.',
      'Write project case studies that show constraints, decisions, and outcomes.',
      'Prepare a portfolio that supports applications, mentorship, and community feedback.',
    ],
    tools: ['Portfolio Review', 'Case Studies', 'Presentation', 'Critique', 'Career Framing'],
    portfolioResult: 'A polished portfolio page with at least one structured project case study.',
  },
  {
    slug: 'data-for-games',
    title: 'Data for Games & Product Insight',
    shortTitle: 'Data',
    area: 'data',
    summary: 'Use data analysis to understand player behavior, production questions, and game/product decisions.',
    longDescription:
      'A practical data path for students and teams that want to ask better questions about games. The package covers analysis foundations, data interpretation, and communication of findings for design and product decisions.',
    audience: 'Designers, producers, analysts, technical students, and indie teams that need data-informed decision making.',
    level: 'Beginner to intermediate',
    duration: '36 estimated hours',
    format: 'Analytical exercises, notebooks, decision memos, and applied data projects',
    image: 'https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=1400&h=900&fit=crop',
    accent: 'emerald',
    courseSlugs: ['dataanalysis'],
    outcomes: [
      'Clean, analyze, and communicate data in a way teams can act on.',
      'Translate player or product questions into measurable analysis.',
      'Create clear visual and written evidence for design and production decisions.',
    ],
    tools: ['Python', 'Pandas', 'Analytics', 'Data Visualization', 'Decision Memos'],
    portfolioResult: 'A data analysis case study tied to a game design or product decision.',
  },
];

export const PUBLIC_COURSE_SNAPSHOT: Program[] = [
  {
    id: 'ai4games2-program-1',
    title: 'Advanced Game AI',
    description:
      'Master advanced AI techniques for game development, including finite state machines, behavior trees, utility AI, minimax search, Monte Carlo methods, and production AI patterns.',
    slug: 'ai4games2',
    thumbnail: 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop',
    estimatedHours: 60,
    category: 'GameDevelopment',
    difficulty: 'Intermediate',
    isEnrollmentOpen: true,
    visibility: 'Public',
    status: 'Published',
    programContents: null,
  },
  {
    id: 'networking-program-1',
    title: 'Network Programming',
    description:
      'Learn to design, implement, and optimize real-time networked applications and games using sockets, serialization, synchronization, and performance tuning techniques.',
    slug: 'networking',
    thumbnail: 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop',
    estimatedHours: 60,
    category: 'GameDevelopment',
    difficulty: 'Intermediate',
    isEnrollmentOpen: true,
    visibility: 'Public',
    status: 'Published',
    programContents: null,
  },
  {
    id: 'databases-program-1',
    title: 'Databases',
    description:
      'This course introduces students to database design, SQL, normalization, relational database theory, and modern NoSQL paradigms for game and platform applications.',
    slug: 'databases',
    thumbnail: 'https://images.unsplash.com/photo-1515879218367-8466d910aaa4?w=1400&h=900&fit=crop',
    estimatedHours: 48,
    category: 'Programming',
    difficulty: 'Intermediate',
    isEnrollmentOpen: true,
    visibility: 'Public',
    status: 'Published',
    programContents: null,
  },
  {
    id: 'python-program-1',
    title: 'Python Programming',
    description: 'Students learn the history and basics of computing, number systems, Boolean logic, algorithm design, and Python programming fundamentals.',
    slug: 'python',
    thumbnail: 'https://images.unsplash.com/photo-1515879218367-8466d910aaa4?w=1400&h=900&fit=crop',
    estimatedHours: 40,
    category: 'Programming',
    difficulty: 'Beginner',
    isEnrollmentOpen: true,
    visibility: 'Public',
    status: 'Published',
    programContents: null,
  },
  {
    id: 'ai4games-program-1',
    title: 'AI for Games',
    description:
      'Learn artificial intelligence techniques for game development, including behavioral agents, pathfinding algorithms, procedural content generation, and noise functions.',
    slug: 'ai4games',
    thumbnail: 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop',
    estimatedHours: 48,
    category: 'GameDevelopment',
    difficulty: 'Intermediate',
    isEnrollmentOpen: true,
    visibility: 'Public',
    status: 'Published',
    programContents: null,
  },
  {
    id: 'portfolio-program-1',
    title: 'Portfolio Development',
    description: 'Build a professional portfolio with stronger project framing, presentation, deployment, analytics, and review-ready case studies.',
    slug: 'portfolio',
    thumbnail: 'https://images.unsplash.com/photo-1497366754035-f200968a6e72?w=1400&h=900&fit=crop',
    estimatedHours: 30,
    category: 'Design',
    difficulty: 'Beginner',
    isEnrollmentOpen: true,
    visibility: 'Public',
    status: 'Published',
    programContents: null,
  },
  {
    id: 'dsa-program-1',
    title: 'Data Structures and Algorithms',
    description:
      'Compare data structures and algorithms for searching, sorting, graph traversal, complexity analysis, and efficient technical problem solving.',
    slug: 'dsa',
    thumbnail: 'https://images.unsplash.com/photo-1515879218367-8466d910aaa4?w=1400&h=900&fit=crop',
    estimatedHours: 60,
    category: 'Programming',
    difficulty: 'Advanced',
    isEnrollmentOpen: true,
    visibility: 'Public',
    status: 'Published',
    programContents: null,
  },
  {
    id: 'intro2gpro-program-1',
    title: 'Introduction to Game Programming',
    description: 'Explore game programming roles, tools, production workflows, technical expectations, and the core habits of successful game programmers.',
    slug: 'intro2gpro',
    thumbnail: 'https://images.unsplash.com/photo-1511512578047-dfb367046420?w=1400&h=900&fit=crop',
    estimatedHours: 45,
    category: 'GameDevelopment',
    difficulty: 'Beginner',
    isEnrollmentOpen: true,
    visibility: 'Public',
    status: 'Published',
    programContents: null,
  },
  {
    id: 'game-publishing-program-1',
    title: 'Game Publishing Mastery',
    description: 'Learn platform-specific publishing workflows for desktop, mobile, console, web, VR stores, self-hosting, and launch operations.',
    slug: 'game-publishing',
    thumbnail: 'https://images.unsplash.com/photo-1556075798-4825dfaaf498?w=1400&h=900&fit=crop',
    estimatedHours: 25,
    category: 'Business',
    difficulty: 'Intermediate',
    isEnrollmentOpen: true,
    visibility: 'Public',
    status: 'Published',
    programContents: null,
  },
  {
    id: 'dataanalysis-program-1',
    title: 'Data Analysis',
    description: 'Learn the fundamentals of data analysis using Python, pandas, visualization workflows, exploratory analysis, and basic statistical methods.',
    slug: 'dataanalysis',
    thumbnail: 'https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=1400&h=900&fit=crop',
    estimatedHours: 40,
    category: 'Programming',
    difficulty: 'Beginner',
    isEnrollmentOpen: true,
    visibility: 'Public',
    status: 'Published',
    programContents: null,
  },
];

const SHOWCASE_BY_SLUG: Record<string, CourseShowcase> = {
  ai4games: {
    slug: 'ai4games',
    programSlug: 'game-ai-systems',
    headline: 'Build decision-making agents and procedural systems that make games feel alive.',
    studioPrompt: 'Students prototype AI behaviors, study pathfinding tradeoffs, and finish with an applied game AI system.',
    projectResult: 'A playable AI prototype with navigation, decision logic, and a documented final project.',
    instructorModel: 'Guided technical instruction with project checkpoints and implementation-focused feedback.',
    portfolioProof: 'Show recruiters and collaborators how you reason about behavior, simulation, and gameplay constraints.',
    outcomes: [
      'Build behavioral agents that react to player state, world context, and gameplay goals.',
      'Implement pathfinding and tactical movement choices that are readable inside a playable prototype.',
      'Use procedural generation techniques to create varied content without losing design control.',
      'Finish a documented AI prototype that explains implementation decisions and tradeoffs.',
    ],
    prerequisites: ['Comfort reading code', 'Basic programming fundamentals', 'Interest in gameplay systems'],
    faq: [
      {
        question: 'Is this only theory?',
        answer: 'No. The course is structured around practical implementation and finishes with a capstone-style AI project.',
      },
      {
        question: 'Which engine is required?',
        answer: 'The concepts transfer across engines. Course examples emphasize implementation patterns over engine lock-in.',
      },
    ],
  },
  ai4games2: {
    slug: 'ai4games2',
    programSlug: 'game-ai-systems',
    headline: 'Push beyond fundamentals into tactical AI, influence maps, and production-minded behavior systems.',
    studioPrompt: 'A deeper AI sequence for students ready to reason about spatial data, tactical choices, and advanced systems.',
    projectResult: 'A tactical AI prototype using influence, scoring, or advanced decision-making techniques.',
    instructorModel: 'Advanced technical walkthroughs with implementation details and portfolio framing.',
    portfolioProof: 'A stronger systems artifact for gameplay programming and technical design portfolios.',
    outcomes: [
      'Design influence-map data that helps agents evaluate danger, pressure, and opportunity.',
      'Build tactical scoring rules that choose actions from readable gameplay constraints.',
      'Combine advanced agent behaviors into a prototype that feels intentional instead of scripted.',
      'Document systems clearly enough for a portfolio review or technical interview.',
    ],
    prerequisites: ['Prior game programming practice', 'Basic AI/pathfinding familiarity', 'Comfort debugging systems'],
    projects: [
      {
        title: 'Influence-map arena',
        summary: 'Build a tactical top-down scenario where agents read pressure, danger, cover, and opportunity from a spatial influence layer.',
        image: 'https://images.unsplash.com/photo-1511512578047-dfb367046420?w=1400&h=900&fit=crop',
        skills: ['Influence maps', 'Spatial reasoning', 'Debug overlays'],
        deliverable: 'A playable arena with visualized influence values and a written note explaining how the map changes agent behavior.',
        moduleLabel: 'Project 01',
      },
      {
        title: 'Decision scoring encounter',
        summary: 'Prototype an encounter where AI chooses movement, attack, retreat, or support behaviors from transparent utility scores.',
        image: 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=1400&h=900&fit=crop',
        skills: ['Utility AI', 'Tactical scoring', 'Behavior debugging'],
        deliverable: 'A score-driven behavior loop with inspector output that makes each action choice reviewable.',
        moduleLabel: 'Project 02',
      },
      {
        title: 'Prototype polish pass',
        summary: 'Package the final AI prototype with readable tuning controls, gameplay framing, and a portfolio-ready implementation breakdown.',
        image: 'https://images.unsplash.com/photo-1518770660439-4636190af475?w=1400&h=900&fit=crop',
        skills: ['Tuning controls', 'Portfolio framing', 'Technical writing'],
        deliverable: 'A short technical case study and recorded walkthrough that show the system, constraints, and tradeoffs.',
        moduleLabel: 'Project 03',
      },
    ],
    journey: [
      {
        label: '01',
        title: 'Spatial reasoning map',
        body: 'Model the tactical space, decide which signals matter, and create a readable influence-map debug view.',
        checkpoint: 'A map overlay that exposes danger, pressure, and opportunity values.',
        projectTitle: 'Influence-map arena',
      },
      {
        label: '02',
        title: 'Action scoring rules',
        body: 'Turn tactical context into weighted scores that explain why an agent moves, attacks, retreats, or waits.',
        checkpoint: 'A scoring table that can be inspected while the encounter runs.',
        projectTitle: 'Decision scoring encounter',
      },
      {
        label: '03',
        title: 'Behavior composition',
        body: 'Combine movement, targeting, and tactical preferences into an encounter that reads as intentional play.',
        checkpoint: 'A playable prototype with at least two distinct agent responses.',
        projectTitle: 'Decision scoring encounter',
      },
      {
        label: '04',
        title: 'Stress test and tune',
        body: 'Change scenario conditions, test failure cases, and tune weights so the AI remains legible under pressure.',
        checkpoint: 'A tuning pass with notes on what changed and why.',
        projectTitle: 'Prototype polish pass',
      },
      {
        label: '05',
        title: 'Portfolio-ready AI breakdown',
        body: 'Package the final build with diagrams, implementation notes, and a concise explanation of design tradeoffs.',
        checkpoint: 'A publishable case-study outline plus final prototype walkthrough.',
        projectTitle: 'Prototype polish pass',
      },
    ],
    faq: [
      {
        question: 'Should I take AI for Games first?',
        answer: 'It is recommended unless you already have implementation experience with game AI fundamentals.',
      },
      {
        question: 'What makes this advanced?',
        answer: 'The work emphasizes layered decision systems, spatial reasoning, and technical tradeoffs.',
      },
    ],
  },
  networking: {
    slug: 'networking',
    programSlug: 'game-ai-systems',
    headline: 'Learn how real-time games exchange state, hide latency, and keep players synchronized.',
    studioPrompt: 'Students study network architecture, serialization, synchronization, and performance constraints.',
    projectResult: 'A small networked interaction or simulation with documented communication flow.',
    instructorModel: 'Technical lessons focused on reasoning about latency, replication, and debugging distributed behavior.',
    portfolioProof: 'A networked systems artifact that shows more than single-player gameplay code.',
    outcomes: [
      'Implement socket and protocol foundations for real-time multiplayer communication.',
      'Synchronize gameplay state while reasoning about authority, replication, and correction.',
      'Serialize data for network transport with attention to size, format, and debugging.',
      'Design around latency so player input and remote state stay understandable.',
    ],
    prerequisites: ['Intermediate programming', 'Basic game loop understanding', 'Debugging discipline'],
    faq: [
      {
        question: 'Is this for multiplayer only?',
        answer: 'The focus is multiplayer/game networking, but the architecture lessons apply to distributed applications too.',
      },
      {
        question: 'Will I build a full online game?',
        answer: 'The course focuses on core networking systems and applied prototypes rather than a full commercial game.',
      },
    ],
  },
  python: {
    slug: 'python',
    programSlug: 'game-programming-foundations',
    headline: 'Start programming with clear fundamentals and enough confidence to build real interactive systems.',
    studioPrompt: 'A practical programming foundation for students preparing for game programming and technical courses.',
    projectResult: 'A collection of programming exercises and a small applied project demonstrating control flow and data use.',
    instructorModel: 'Step-by-step fundamentals with examples, exercises, and structured practice.',
    portfolioProof: 'A visible starting point for technical growth and future gameplay work.',
    outcomes: [
      'Write Python programs with clear control flow, functions, data types, and debugging habits.',
      'Break problems into small steps before translating them into working code.',
      'Practice algorithmic thinking through exercises that build confidence without hiding the logic.',
      'Finish a small applied project that proves you can move from idea to implementation.',
    ],
    prerequisites: ['No prior programming experience required', 'Curiosity and consistent practice'],
    faq: [
      {
        question: 'Is Python useful for games?',
        answer: 'Yes. It is an excellent foundation for programming logic, tools, automation, and later game systems work.',
      },
      {
        question: 'Do I need math before starting?',
        answer: 'Basic comfort with arithmetic is enough. The course builds programming thinking progressively.',
      },
    ],
  },
  dsa: {
    slug: 'dsa',
    programSlug: 'game-programming-foundations',
    headline: 'Understand the data structures and algorithms behind reliable gameplay and tools programming.',
    studioPrompt: 'Students practice the structures and algorithmic patterns that make complex systems tractable.',
    projectResult: 'A set of technical exercises showing data modeling, algorithm choice, and performance reasoning.',
    instructorModel: 'Conceptual breakdowns paired with implementation practice and code review habits.',
    portfolioProof: 'Evidence that you can reason about complexity, not just assemble features.',
    outcomes: [
      'Choose arrays, lists, stacks, queues, maps, and sets based on the problem shape.',
      'Model trees and graphs for traversal, pathfinding, dependency, and gameplay problems.',
      'Compare searching and sorting approaches through correctness and performance tradeoffs.',
      'Explain complexity decisions in a way that improves engineering judgment.',
    ],
    prerequisites: ['Basic programming experience', 'Comfort writing small functions'],
    faq: [
      {
        question: 'Is this interview prep?',
        answer: 'It helps with interviews, but the course is framed around practical engineering reasoning for games and tools.',
      },
      {
        question: 'Which language is used?',
        answer: 'The ideas are language-independent. Implementation follows the course material available in the catalog.',
      },
    ],
  },
  databases: {
    slug: 'databases',
    programSlug: 'game-programming-foundations',
    headline: 'Design data models that support game features, platforms, content, and player-facing systems.',
    studioPrompt: 'Students learn relational design, SQL, normalization, and when alternative data models make sense.',
    projectResult: 'A database-backed design or application exercise with clear schema reasoning.',
    instructorModel: 'Applied database lessons connected to software, services, and game platform needs.',
    portfolioProof: 'A technical artifact showing you can model data and communicate schema tradeoffs.',
    outcomes: [
      'Query and update relational data with SQL patterns used by real applications.',
      'Design schemas that make ownership, relationships, constraints, and change easier to manage.',
      'Normalize data deliberately, then recognize when denormalization is worth the tradeoff.',
      'Compare relational and NoSQL models against concrete product and game-platform needs.',
    ],
    prerequisites: ['Basic programming literacy', 'Interest in backend or tools systems'],
    faq: [
      {
        question: 'Is this only for backend developers?',
        answer: 'No. Designers, producers, and gameplay programmers all benefit from understanding data shape and constraints.',
      },
      {
        question: 'Will this cover NoSQL?',
        answer: 'Yes, relational databases are contrasted with common NoSQL approaches.',
      },
    ],
  },
  intro2gpro: {
    slug: 'intro2gpro',
    programSlug: 'game-programming-foundations',
    headline: 'Move from programming basics into the structure, rhythm, and constraints of game programming.',
    studioPrompt: 'Students learn how gameplay systems are organized and how interactive programs become playable experiences.',
    projectResult: 'A small gameplay prototype or technical feature that demonstrates engine-facing programming habits.',
    instructorModel: 'Practical programming guidance grounded in game loops, systems, and implementation constraints.',
    portfolioProof: 'A first game-programming artifact that can anchor later AI, networking, or systems courses.',
    outcomes: [
      'Understand the game loop and how frame-by-frame updates shape interactive behavior.',
      'Build small gameplay systems that connect input, state, feedback, and rules.',
      'Debug interactive code by observing state changes instead of guessing at behavior.',
      'Finish a focused game-programming artifact that prepares you for advanced systems courses.',
    ],
    prerequisites: ['Basic programming experience', 'Comfort practicing with examples'],
    faq: [
      {
        question: 'Is this beginner-friendly?',
        answer: 'Yes, it is designed as the bridge from programming basics into game programming.',
      },
      {
        question: 'Will I finish a game?',
        answer: 'The goal is a focused playable prototype or system rather than a complete commercial game.',
      },
    ],
  },
  'game-publishing': {
    slug: 'game-publishing',
    programSlug: 'launch-and-publishing',
    headline: 'Prepare your game for public release across desktop, mobile, web, and console storefronts.',
    studioPrompt: 'A launch operations course for teams that need store readiness, release planning, and practical publishing workflows.',
    projectResult: 'A platform-specific launch checklist and release plan for a real or portfolio game.',
    instructorModel: 'Operational guidance, platform breakdowns, and practical publishing checklists.',
    portfolioProof: 'A production artifact that shows you can ship, not only build.',
    outcomes: [
      'Prepare store pages with the assets, descriptions, and requirements each platform expects.',
      'Map submission flows for desktop, mobile, web, and console release paths.',
      'Build a launch plan that covers milestones, risk, communication, and readiness checks.',
      'Create reusable release operations documentation for future projects.',
    ],
    prerequisites: ['A finished or in-progress game project is helpful', 'Basic production planning mindset'],
    faq: [
      {
        question: 'Does this cover Steam?',
        answer: 'Yes. It also includes mobile stores, console pathways, web platforms, and self-hosting workflows.',
      },
      {
        question: 'Is this for solo developers?',
        answer: 'Yes. The course is especially useful for solo developers and small teams preparing a public launch.',
      },
    ],
  },
  portfolio: {
    slug: 'portfolio',
    programSlug: 'portfolio-and-career',
    headline: 'Turn projects into a portfolio story that makes your work easier to review, trust, and remember.',
    studioPrompt: 'Students select work, write case studies, improve presentation, and build a stronger public proof layer.',
    projectResult: 'A polished portfolio page or case study ready to share for review, mentorship, or applications.',
    instructorModel: 'Critique-oriented guidance around framing, sequencing, clarity, and professional presentation.',
    portfolioProof: 'A sharper public identity and stronger evidence for the work you want to do next.',
    outcomes: [
      'Select projects that support a focused professional story instead of showing everything.',
      'Write case studies that explain constraints, decisions, iteration, and final outcomes.',
      'Improve visual presentation so reviewers can scan the work quickly and trust the craft.',
      'Frame your portfolio for the roles, teams, or collaborators you want to reach.',
    ],
    prerequisites: ['At least one project or exercise to present', 'Willingness to revise and critique work'],
    faq: [
      {
        question: 'Can beginners take this?',
        answer: 'Yes, but it works best when you have at least one project, prototype, or exercise to shape into evidence.',
      },
      {
        question: 'Is this only for artists?',
        answer: 'No. Programmers, designers, producers, and technical students all need clear portfolio presentation.',
      },
    ],
  },
  dataanalysis: {
    slug: 'dataanalysis',
    programSlug: 'data-for-games',
    headline: 'Use data to answer game, player, and product questions with evidence your team can act on.',
    studioPrompt: 'Students learn analysis workflows, interpretation, and communication through practical data exercises.',
    projectResult: 'A data analysis case study with clear question, method, visualization, and recommendation.',
    instructorModel: 'Applied analytics guidance focused on decision quality, not just charts.',
    portfolioProof: 'A decision memo or analysis artifact that demonstrates practical product thinking.',
    outcomes: [
      'Clean and structure raw data so analysis starts from reliable evidence.',
      'Explore game and product questions with practical analysis workflows.',
      'Create visualizations that make patterns, gaps, and tradeoffs easier to discuss.',
      'Turn findings into decision memos that teams can act on.',
    ],
    prerequisites: ['Basic programming helps', 'Interest in game/product questions'],
    faq: [
      {
        question: 'Is this a data science degree course?',
        answer: 'No. It is a practical analysis course focused on useful workflows and game/product decisions.',
      },
      {
        question: 'Will I need advanced statistics?',
        answer: 'No. The emphasis is on clear questions, clean analysis, and useful communication.',
      },
    ],
  },
};

export function getPublicProgramPackage(slug: string): PublicProgramPackage | null {
  return PUBLIC_PROGRAM_PACKAGES.find((program) => program.slug === slug) ?? null;
}

export function getProgramForCourse(courseSlug: string | null | undefined): PublicProgramPackage | null {
  if (!courseSlug) {
    return null;
  }

  return PUBLIC_PROGRAM_PACKAGES.find((program) => program.courseSlugs.includes(courseSlug)) ?? null;
}

export function getCourseShowcase(courseSlug: string | null | undefined): CourseShowcase | null {
  if (!courseSlug) {
    return null;
  }

  return SHOWCASE_BY_SLUG[courseSlug] ?? null;
}

export function getCoursesForProgram<T extends Pick<Program, 'slug'>>(program: PublicProgramPackage, courses: T[]): T[] {
  return program.courseSlugs.map((slug) => courses.find((course) => course.slug === slug)).filter((course): course is T => Boolean(course));
}

function getContentSortOrder(content: ProgramContent): number {
  const sortOrder = content.sortOrder;

  if (typeof sortOrder === 'number') {
    return sortOrder;
  }

  if (typeof sortOrder === 'string') {
    const parsed = Number(sortOrder);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  return 0;
}

export function listCourseContentPreview(contents: ProgramContent[] | null | undefined, limit = 6): ProgramContent[] {
  return [...(contents ?? [])]
    .filter((content) => !content.parentId)
    .sort((left, right) => getContentSortOrder(left) - getContentSortOrder(right))
    .slice(0, limit);
}
