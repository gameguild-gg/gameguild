import {
  Product,
  ProductProgram,
  Program,
  ProgramContent,
  ProgramContentType,
} from "@/lib/api/generated";

import ai4games2Week01Lecture from "./01-fsm/01-lecture.md";
import ai4games2Week01Readings from "./01-fsm/02-readings.md";
import ai4games2Week01Quiz from "./01-fsm/03-quiz.md";
import ai4games2Week01Setup from "./01-fsm/04-setup.md";
import ai4games2Week01Assignment from "./01-fsm/05-assignment.md";
import ai4games2Week02Slides from "./02-bt/00-reveal.md";
import ai4games2Week02Lecture from "./02-bt/01-lecture.md";
import ai4games2Week02Readings from "./02-bt/02-readings.md";
import ai4games2Week02Quiz from "./02-bt/03-quiz.md";
import ai4games2Week02Assignment from "./02-bt/04-assignment.md";
import ai4games2Week03Lecture from "./03-utility/01-lecture.md";
import ai4games2Week03Readings from "./03-utility/02-readings.md";
import ai4games2Week03Quiz from "./03-utility/03-quiz.md";
import ai4games2Week03Assignment from "./03-utility/04-assignment.md";
import ai4games2Week04Slides from "./04-minmax/00-reveal.md";
import ai4games2Week04Lecture from "./04-minmax/01-lecture.md";
import ai4games2Week04Readings from "./04-minmax/02-readings.md";
import ai4games2Week04Quiz from "./04-minmax/03-quiz.md";
import ai4games2Week04Assignment from "./04-minmax/04-assignment.md";
import ai4games2Week05Slides from "./05-mcts/00-reveal.md";
import ai4games2Week05Lecture from "./05-mcts/01-lecture.md";
import ai4games2Week05Readings from "./05-mcts/02-readings.md";
import ai4games2Week05Quiz from "./05-mcts/03-quiz.md";
import ai4games2Week05Assignment from "./05-mcts/04-assignment.md";
import ai4games2Week06Slides from "./06-chess/00-reveal.md";
import ai4games2Week06Lecture from "./06-chess/01-lecture.md";
import ai4games2Week06Readings from "./06-chess/02-readings.md";
import ai4games2Week06Quiz from "./06-chess/03-quiz.md";
import ai4games2Week07Slides from "./07-chess/00-reveal.md";
import ai4games2Week07Lecture from "./07-chess/01-lecture.md";
import ai4games2Week07Readings from "./07-chess/02-readings.md";
import ai4games2Week08Midterm from "./08-competition/midterm.md";
import ai4games2Week09FinalProject from "./09-break/final-project.md";
import ai4games2Week10Slides from "./10-wfc/00-reveal.md";
import ai4games2Week10Lecture from "./10-wfc/01-lecture.md";
import ai4games2Week10Readings from "./10-wfc/02-readings.md";
import ai4games2Week10Assignment from "./10-wfc/03-assignment.md";
import ai4games2Week10Quiz from "./10-wfc/04-quiz.md";
import ai4games2Week11Slides from "./11-goap/00-reveal.md";
import ai4games2Week11Lecture from "./11-goap/01-lecture.md";
import ai4games2Week11Readings from "./11-goap/02-readings.md";
import ai4games2Week11Assignment from "./11-goap/03-assignment.md";
import ai4games2Week11Quiz from "./11-goap/04-quiz.md";
import ai4games2Week12Slides from "./12-multiagent/00-reveal.md";
import ai4games2Week12Lecture from "./12-multiagent/01-lecture.md";
import ai4games2Week12Readings from "./12-multiagent/02-readings.md";
import ai4games2Week12Quiz from "./12-multiagent/03-quiz.md";
import ai4games2Week12Assignment from "./12-multiagent/assignment.md";
import ai4games2Week13Slides from "./13-influence/00-reveal.md";
import ai4games2Week13Lecture from "./13-influence/01-lecture.md";
import ai4games2Week13Readings from "./13-influence/02-readings.md";
import ai4games2Week13Assignment from "./13-influence/assignment.md";
import ai4games2ExtraOldLecture from "./old/extras/llms.md";
import ai4games2Week01OldLecture from "./old/week01/lecture.md";
import ai4games2Week01OldReadings from "./old/week01/readings.md";
import ai4games2Week02OldLecture from "./old/week02/lecture.md";
import ai4games2Week02OldPcg from "./old/week02/pcg.md";
import ai4games2Week03OldAstar from "./old/week03/a-star.md";
import ai4games2Week03OldLecture from "./old/week03/lecture.md";
import ai4games2Week04OldAssignment from "./old/week04/assignment.md";
import ai4games2Week04OldLecture from "./old/week04/lecture.md";
import ai4games2Week05OldLecture from "./old/week05/lecture.md";
import ai4games2Week05OldLecture2 from "./old/week05/lecture2.md";
import ai4games2Week06OldLecture from "./old/week06/lecture.md";
import ai4games2Week07OldLecture from "./old/week07/lecture.md";
import ai4games2Week08OldLecture from "./old/week08/lecture.md";
import ai4games2Week09OldLecture from "./old/week09/lecture.md";
import ai4games2Week10OldLecture from "./old/week10/lecture.md";
import ai4games2Week11OldAssignment from "./old/week11/assignment.md";
import ai4games2Week11OldBoard from "./old/week11/board.md";
import ai4games2Week12OldLecture from "./old/week12/lecture.md";
import ai4games2Week13OldLecture from "./old/week13/lecture.md";
import ai4games2Syllabus from "./syllabus.md";

export const ai4games2Program: Program = {
  id: "ai4games2-program",
  title: "Advanced Game AI",
  description:
    "Learn advanced artificial intelligence techniques specifically designed for game development, including pathfinding, decision-making, and procedural content generation.",
  slug: "ai4games2",
  thumbnail:
    "https://i.imgur.com/cooKXbw.jpeg",
  videoShowcaseUrl: null,
  estimatedHours: 60,
  enrollmentStatus: 0,
  maxEnrollments: null,
  enrollmentDeadline: null,
  category: 1,
  difficulty: 1,
  visibility: 0,
  status: 1,
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
  programContents: [],
  programUsers: [],
  programRatings: [],
  programWishlists: [],
};

export const ai4games2Product: Product = {
  id: "ai4games2-product",
  title: "Advanced Game AI Course",
  name: "Advanced Game AI",
  description: "Master advanced AI techniques for game development",
  shortDescription:
    "Learn advanced pathfinding, decision-making, and procedural content generation",
  imageUrl:
    "https://i.imgur.com/cooKXbw.jpeg",
  type: 0,
  isBundle: false,
  creatorId: "1",
  bundleItems: null,
  referralCommissionPercentage: 0,
  maxAffiliateDiscount: 0,
  affiliateCommissionPercentage: 0,
  visibility: 0,
  status: 1,
  slug: "ai4games2",
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
  productPrograms: [],
  productPricings: [],
  subscriptionPlans: [],
  userProducts: [],
  promoCodes: [],
};

export const ai4games2ProductProgram: ProductProgram = {
  id: "ai4games2-product-program",
  productId: "ai4games2-product",
  product: ai4games2Product,
  programId: "ai4games2-program",
  program: ai4games2Program,
  sortOrder: 1,
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2SyllabusContent: ProgramContent = {
  id: "ai4games2-syllabus",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Course Syllabus",
  slug: "syllabus",
  description: "Advanced AI for Games course overview and objectives",
  type: 0,
  body: ai4games2Syllabus,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 20,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// Parent container for all old content
export const ai4games2OldContent: ProgramContent = {
  id: "ai4games2-old",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Old Content (Archive)",
  slug: "old",
  description: "Archived course content from previous versions",
  type: 0,
  body: "# Archived Content\n\nThis section contains archived content from previous versions of the course.",
  sortOrder: 2,
  isRequired: false,
  estimatedMinutes: 1,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// Old Week 01 Content (from archived version)
export const ai4games2Week01OldLectureContent: ProgramContent = {
  id: "ai4games2-week-01-old-lecture",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 1: Introduction",
  slug: "week-01",
  description: "Archived week 1 lecture content",
  type: 0,
  body: ai4games2Week01OldLecture,
  sortOrder: 1,
  isRequired: false,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week01OldReadingsContent: ProgramContent = {
  id: "ai4games2-week-01-old-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-01-old-lecture",
  title: "Week 1: Readings",
  slug: "readings",
  description: "Archived week 1 readings",
  type: 0,
  body: ai4games2Week01OldReadings,
  sortOrder: 1,
  isRequired: false,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week01OldLectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// New Week 01 Content (Primary, from 01-fsm folder)
export const ai4games2Week01LectureContent: ProgramContent = {
  id: "ai4games2-week-01-lecture",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 1: FSM & Decision Basics",
  slug: "week-01",
  description: "Finite state machines, behavior trees, and decision architectures fundamentals",
  type: 0,
  body: ai4games2Week01Lecture,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week01ReadingsContent: ProgramContent = {
  id: "ai4games2-week-01-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-01-lecture",
  title: "Week 1: Readings",
  slug: "readings",
  description: "Required readings and videos for FSM and Behavior Trees",
  type: 0,
  body: ai4games2Week01Readings,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week01LectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week01QuizContent: ProgramContent = {
  id: "ai4games2-week-01-quiz",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-01-lecture",
  title: "Week 1: Quiz",
  slug: "quiz",
  description: "Test your understanding of FSM concepts, state patterns, and transitions",
  type: 0,
  body: ai4games2Week01Quiz,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week01LectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week01SetupContent: ProgramContent = {
  id: "ai4games2-week-01-setup",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-01-lecture",
  title: "Week 1: Setup",
  slug: "setup",
  description: "Repository setup, development environment, and assignment submission workflow",
  type: 0,
  body: ai4games2Week01Setup,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 30,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week01LectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week01AssignmentContent: ProgramContent = {
  id: "ai4games2-week-01-assignment",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-01-lecture",
  title: "Week 1: Assignment",
  slug: "assignment",
  description: "Implement a simple finite state machine with proper state patterns",
  type: 0,
  body: ai4games2Week01Assignment,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 180,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week01LectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// New Week 02 Content (Behavior Trees)
export const ai4games2Week02LectureContent: ProgramContent = {
  id: "ai4games2-week-02-lecture",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 2: Behavior Trees",
  slug: "week-02",
  description: "Behavior Tree fundamentals: Selector, Sequence, Status, Running state, and guard AI",
  type: 0,
  body: ai4games2Week02Lecture,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week02ReadingsContent: ProgramContent = {
  id: "ai4games2-week-02-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-02-lecture",
  title: "Week 2: Readings",
  slug: "readings",
  description: "Required readings and videos for Behavior Trees",
  type: 0,
  body: ai4games2Week02Readings,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week02LectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week02QuizContent: ProgramContent = {
  id: "ai4games2-week-02-quiz",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-02-lecture",
  title: "Week 2: Quiz",
  slug: "quiz",
  description: "Test your understanding of Behavior Trees (Selectors, Sequences, abort modes, debugging)",
  type: 0,
  body: ai4games2Week02Quiz,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week02LectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week02AssignmentContent: ProgramContent = {
  id: "ai4games2-week-02-assignment",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-02-lecture",
  title: "Week 2: Assignment",
  slug: "assignment",
  description: "Implement a guard AI using Behavior Trees (Selector/Sequence) with Running/resume semantics",
  type: 0,
  body: ai4games2Week02Assignment,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week02LectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week02SlidesContent: ProgramContent = {
  id: "ai4games2-week-02-slides",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-02-lecture",
  title: "Week 2: Slides",
  slug: "slides",
  description: "Behavior Trees presentation slides covering industry examples and production patterns",
  type: ProgramContentType.REVEAL,
  body: ai4games2Week02Slides,
  sortOrder: 4,
  isRequired: false,
  estimatedMinutes: 45,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week02LectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// New Week 03 Content (Utility AI)
export const ai4games2Week03LectureContent: ProgramContent = {
  id: "ai4games2-week-03-lecture",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 3: Utility AI",
  slug: "week-03",
  description: "Utility-based decision making, considerations, response curves, and The Sims",
  type: ProgramContentType.REVEAL,
  body: ai4games2Week03Lecture,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week03ReadingsContent: ProgramContent = {
  id: "ai4games2-week-03-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-03-lecture",
  title: "Week 3: Readings",
  slug: "readings",
  description: "Required readings and videos for Utility AI",
  type: ProgramContentType.PAGE,
  body: ai4games2Week03Readings,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week03LectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week03QuizContent: ProgramContent = {
  id: "ai4games2-week-03-quiz",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-03-lecture",
  title: "Week 3: Quiz",
  slug: "quiz",
  description: "Test your understanding of Utility AI concepts, considerations, and response curves",
  type: ProgramContentType.PAGE,
  body: ai4games2Week03Quiz,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week03LectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week03AssignmentContent: ProgramContent = {
  id: "ai4games2-week-03-assignment",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-03-lecture",
  title: "Week 3: Assignment",
  slug: "assignment",
  description: "Implement a Utility AI system for a survival game character with response curves and considerations",
  type: ProgramContentType.PAGE,
  body: ai4games2Week03Assignment,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 180,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week03LectureContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// Week 04 Content (MinMax & Alpha-Beta Pruning)
export const ai4games2Week04SlidesContent: ProgramContent = {
  id: "ai4games2-week-04-slides",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 4: MinMax & Alpha-Beta Pruning",
  slug: "week-04",
  description: "Adversarial search, minimax algorithm, alpha-beta pruning, and game tree optimization",
  type: ProgramContentType.REVEAL,
  body: ai4games2Week04Slides,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week04LectureContent: ProgramContent = {
  id: "ai4games2-week-04-lecture",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-04-slides",
  title: "Week 4: Lecture Notes",
  slug: "lecture",
  description: "Detailed lecture notes on MinMax search and Alpha-Beta pruning",
  type: ProgramContentType.PAGE,
  body: ai4games2Week04Lecture,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 45,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week04SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week04ReadingsContent: ProgramContent = {
  id: "ai4games2-week-04-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-04-slides",
  title: "Week 4: Readings",
  slug: "readings",
  description: "Required readings and videos for MinMax and Alpha-Beta Pruning",
  type: ProgramContentType.PAGE,
  body: ai4games2Week04Readings,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 135,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week04SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week04QuizContent: ProgramContent = {
  id: "ai4games2-week-04-quiz",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-04-slides",
  title: "Week 4: Quiz",
  slug: "quiz",
  description: "Test your understanding of MinMax algorithm and Alpha-Beta pruning",
  type: ProgramContentType.PAGE,
  body: ai4games2Week04Quiz,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week04SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week04AssignmentContent: ProgramContent = {
  id: "ai4games2-week-04-assignment",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-04-slides",
  title: "Week 4: Assignment",
  slug: "assignment",
  description: "Implement a game AI using MinMax with Alpha-Beta pruning",
  type: ProgramContentType.PAGE,
  body: ai4games2Week04Assignment,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week04SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// Week 05 Content (Monte Carlo Tree Search)
export const ai4games2Week05SlidesContent: ProgramContent = {
  id: "ai4games2-week-05-slides",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 5: Monte Carlo Tree Search",
  slug: "week-05",
  description: "MCTS algorithm, UCB1 selection, rollout simulations, and neural MCTS (AlphaGo/AlphaZero)",
  type: ProgramContentType.REVEAL,
  body: ai4games2Week05Slides,
  sortOrder: 5,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week05LectureContent: ProgramContent = {
  id: "ai4games2-week-05-lecture",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-05-slides",
  title: "Week 5: Lecture Notes",
  slug: "lecture",
  description: "Detailed lecture notes on MCTS, UCB1, rollout policies, and AlphaZero",
  type: ProgramContentType.PAGE,
  body: ai4games2Week05Lecture,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 45,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week05SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week05ReadingsContent: ProgramContent = {
  id: "ai4games2-week-05-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-05-slides",
  title: "Week 5: Readings",
  slug: "readings",
  description: "Required readings and videos for Monte Carlo Tree Search",
  type: ProgramContentType.PAGE,
  body: ai4games2Week05Readings,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 139,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week05SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week05QuizContent: ProgramContent = {
  id: "ai4games2-week-05-quiz",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-05-slides",
  title: "Week 5: Quiz",
  slug: "quiz",
  description: "Test your understanding of MCTS, UCB1, and the four search phases",
  type: ProgramContentType.PAGE,
  body: ai4games2Week05Quiz,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week05SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week05AssignmentContent: ProgramContent = {
  id: "ai4games2-week-05-assignment",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-05-slides",
  title: "Week 5: Assignment",
  slug: "assignment",
  description: "Implement a game AI using Monte Carlo Tree Search with UCB1",
  type: ProgramContentType.PAGE,
  body: ai4games2Week05Assignment,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week05SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// Week 06 Content (Chess Engine Core)
export const ai4games2Week06SlidesContent: ProgramContent = {
  id: "ai4games2-week-06-slides",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 6: Chess Engine Core",
  slug: "week-06",
  description: "Chess engine architecture: board representation, evaluation functions, iterative deepening, aspiration windows, quiescence search, and time management",
  type: ProgramContentType.REVEAL,
  body: ai4games2Week06Slides,
  sortOrder: 6,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week06LectureContent: ProgramContent = {
  id: "ai4games2-week-06-lecture",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-06-slides",
  title: "Week 6: Lecture Notes",
  slug: "lecture",
  description: "Detailed lecture notes on chess engine board representation, evaluation, search, and time management",
  type: ProgramContentType.PAGE,
  body: ai4games2Week06Lecture,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week06SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week06ReadingsContent: ProgramContent = {
  id: "ai4games2-week-06-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-06-slides",
  title: "Week 6: Readings",
  slug: "readings",
  description: "Required readings and videos for Chess Engine Core",
  type: ProgramContentType.PAGE,
  body: ai4games2Week06Readings,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week06SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week06QuizContent: ProgramContent = {
  id: "ai4games2-week-06-quiz",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-06-slides",
  title: "Week 6: Quiz",
  slug: "quiz",
  description: "Test your understanding of chess engine architecture, board representation, evaluation functions, and search techniques",
  type: ProgramContentType.PAGE,
  body: ai4games2Week06Quiz,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week06SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// Week 07 Content (Advanced Chess Techniques)
export const ai4games2Week07SlidesContent: ProgramContent = {
  id: "ai4games2-week-07-slides",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 7: Advanced Chess Techniques",
  slug: "week-07",
  description:
    "Chess engine optimization: null-move pruning, LMR, move ordering, transposition tables, and endgames",
  type: ProgramContentType.REVEAL,
  body: ai4games2Week07Slides,
  sortOrder: 7,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week07LectureContent: ProgramContent = {
  id: "ai4games2-week-07-lecture",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-07-slides",
  title: "Week 7: Lecture Notes",
  slug: "lecture",
  description:
    "Detailed lecture notes on chess engine optimization techniques and competition preparation",
  type: ProgramContentType.PAGE,
  body: ai4games2Week07Lecture,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week07SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week07ReadingsContent: ProgramContent = {
  id: "ai4games2-week-07-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-07-slides",
  title: "Week 7: Readings",
  slug: "readings",
  description: "Required readings and videos for Advanced Chess Techniques",
  type: ProgramContentType.PAGE,
  body: ai4games2Week07Readings,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week07SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week08MidtermContent: ProgramContent = {
  id: "ai4games2-week-08-midterm",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 8: Midterm — Chess Competition",
  slug: "week-08",
  description:
    "Midterm chess engine competition: implement advanced search techniques and compete in a class-wide tournament",
  type: ProgramContentType.PAGE,
  body: ai4games2Week08Midterm,
  sortOrder: 8,
  isRequired: true,
  estimatedMinutes: 600,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week09FinalProjectContent: ProgramContent = {
  id: "ai4games2-week-09-final-project",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 9: Final Project",
  slug: "week-09",
  description:
    "Final project overview: deliverables, topic suggestions, tech demo requirements, and grading",
  type: ProgramContentType.PAGE,
  body: ai4games2Week09FinalProject,
  sortOrder: 9,
  isRequired: true,
  estimatedMinutes: 30,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// Week 10 Content (Wave Function Collapse)
export const ai4games2Week10SlidesContent: ProgramContent = {
  id: "ai4games2-week-10-slides",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 10: Wave Function Collapse",
  slug: "week-10",
  description: "Constraint-based procedural generation: WFC algorithm, tiled and overlapping models, entropy heuristic, propagation, and tileset design",
  type: ProgramContentType.REVEAL,
  body: ai4games2Week10Slides,
  sortOrder: 10,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week10LectureContent: ProgramContent = {
  id: "ai4games2-week-10-lecture",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-10-slides",
  title: "Week 10: Lecture Notes",
  slug: "lecture",
  description: "Detailed lecture notes on WFC, CSP formulation, entropy, propagation, and backtracking",
  type: ProgramContentType.PAGE,
  body: ai4games2Week10Lecture,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 45,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week10SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week10ReadingsContent: ProgramContent = {
  id: "ai4games2-week-10-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-10-slides",
  title: "Week 10: Readings",
  slug: "readings",
  description: "Required readings and videos for Wave Function Collapse",
  type: ProgramContentType.PAGE,
  body: ai4games2Week10Readings,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week10SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week10QuizContent: ProgramContent = {
  id: "ai4games2-week-10-quiz",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-10-slides",
  title: "Week 10: Quiz",
  slug: "quiz",
  description: "Test your understanding of WFC, constraint satisfaction, entropy heuristic, and propagation",
  type: ProgramContentType.PAGE,
  body: ai4games2Week10Quiz,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week10SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week10AssignmentContent: ProgramContent = {
  id: "ai4games2-week-10-assignment",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-10-slides",
  title: "Week 10: Assignment",
  slug: "assignment",
  description: "Final Project — Checkpoint 1: Proposal",
  type: ProgramContentType.PAGE,
  body: ai4games2Week10Assignment,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week10SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// Week 11 Content (Goal-Oriented Action Planning)
export const ai4games2Week11SlidesContent: ProgramContent = {
  id: "ai4games2-week-11-slides",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 11: Goal-Oriented Action Planning",
  slug: "week-11",
  description: "GOAP: STRIPS planning, A* through action space, F.E.A.R. case study, world state, preconditions/effects, replanning",
  type: ProgramContentType.REVEAL,
  body: ai4games2Week11Slides,
  sortOrder: 11,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week11LectureContent: ProgramContent = {
  id: "ai4games2-week-11-lecture",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-11-slides",
  title: "Week 11: Lecture Notes",
  slug: "lecture",
  description: "Detailed lecture notes on GOAP, STRIPS formalism, F.E.A.R. case study, solver design, and architecture comparison",
  type: ProgramContentType.PAGE,
  body: ai4games2Week11Lecture,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week11SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week11ReadingsContent: ProgramContent = {
  id: "ai4games2-week-11-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-11-slides",
  title: "Week 11: Readings",
  slug: "readings",
  description: "Required readings and videos for Goal-Oriented Action Planning",
  type: ProgramContentType.PAGE,
  body: ai4games2Week11Readings,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week11SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week11QuizContent: ProgramContent = {
  id: "ai4games2-week-11-quiz",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-11-slides",
  title: "Week 11: Quiz",
  slug: "quiz",
  description: "Test your understanding of GOAP, STRIPS, F.E.A.R. AI, planning heuristics, and replanning",
  type: ProgramContentType.PAGE,
  body: ai4games2Week11Quiz,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week11SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week11AssignmentContent: ProgramContent = {
  id: "ai4games2-week-11-assignment",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-11-slides",
  title: "Week 11: Assignment",
  slug: "assignment",
  description: "Final Project — Checkpoint 2: Architecture Design",
  type: ProgramContentType.PAGE,
  body: ai4games2Week11Assignment,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week11SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// Week 12 Content (Multi-Agent Coordination)
export const ai4games2Week12SlidesContent: ProgramContent = {
  id: "ai4games2-week-12-slides",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 12: Multi-Agent Coordination",
  slug: "week-12",
  description: "Multi-agent coordination: communication patterns, blackboard architecture, Killzone hierarchical AI, token systems, companion AI",
  type: ProgramContentType.REVEAL,
  body: ai4games2Week12Slides,
  sortOrder: 12,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week12LectureContent: ProgramContent = {
  id: "ai4games2-week-12-lecture",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-12-slides",
  title: "Week 12: Lecture Notes",
  slug: "lecture",
  description: "Detailed lecture notes on multi-agent coordination, Observer/Event Queue/Pub-Sub patterns, blackboard architecture, Killzone case study, token systems, and companion AI",
  type: ProgramContentType.PAGE,
  body: ai4games2Week12Lecture,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week12SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week12ReadingsContent: ProgramContent = {
  id: "ai4games2-week-12-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-12-slides",
  title: "Week 12: Readings",
  slug: "readings",
  description: "Required readings and videos for Multi-Agent Coordination",
  type: ProgramContentType.PAGE,
  body: ai4games2Week12Readings,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week12SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week12QuizContent: ProgramContent = {
  id: "ai4games2-week-12-quiz",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-12-slides",
  title: "Week 12: Quiz",
  slug: "quiz",
  description: "Test your understanding of multi-agent coordination, communication patterns, blackboard systems, token systems, and companion AI",
  type: ProgramContentType.PAGE,
  body: ai4games2Week12Quiz,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week12SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week12AssignmentContent: ProgramContent = {
  id: "ai4games2-week-12-assignment",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-12-slides",
  title: "Week 12: Assignment",
  slug: "assignment",
  description: "Final Project — Checkpoint 3: Proof of Concept",
  type: ProgramContentType.PAGE,
  body: ai4games2Week12Assignment,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week12SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// Week 13 Content (Influence Maps & Tactical Position Evaluation)
export const ai4games2Week13SlidesContent: ProgramContent = {
  id: "ai4games2-week-13-slides",
  programId: "ai4games2-program",
  parentId: undefined,
  title: "Week 13: Influence Maps & Tactical Position Evaluation",
  slug: "week-13",
  description: "Influence maps, value propagation, decay functions, layered maps, tactical position evaluation, cover points, flanking detection, tactical pathfinding",
  type: ProgramContentType.REVEAL,
  body: ai4games2Week13Slides,
  sortOrder: 13,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week13LectureContent: ProgramContent = {
  id: "ai4games2-week-13-lecture",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-13-slides",
  title: "Week 13: Lecture Notes",
  slug: "lecture",
  description: "Detailed lecture notes on influence maps, value propagation, decay functions, layered maps, tactical position evaluation, cover points, flanking detection, and tactical pathfinding",
  type: ProgramContentType.PAGE,
  body: ai4games2Week13Lecture,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week13SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week13ReadingsContent: ProgramContent = {
  id: "ai4games2-week-13-readings",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-13-slides",
  title: "Week 13: Readings",
  slug: "readings",
  description: "Required readings and videos for Influence Maps & Tactical Position Evaluation",
  type: ProgramContentType.PAGE,
  body: ai4games2Week13Readings,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week13SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week13AssignmentContent: ProgramContent = {
  id: "ai4games2-week-13-assignment",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-13-slides",
  title: "Week 13: Assignment",
  slug: "assignment",
  description: "Final Project — Checkpoint 4",
  type: ProgramContentType.PAGE,
  body: ai4games2Week13Assignment,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week13SlidesContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week02OldContent: ProgramContent = {
  id: "ai4games2-week-02",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 2: Procedural Content Generation",
  slug: "week-02",
  description: "PCG concepts and techniques",
  type: 0,
  body: ai4games2Week02OldLecture,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week02OldPcgContent: ProgramContent = {
  id: "ai4games2-week-02-pcg",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-02",
  title: "Week 2: PCG Materials",
  slug: "pcg",
  description: "Supplementary PCG resources",
  type: 0,
  body: ai4games2Week02OldPcg,
  sortOrder: 2,
  isRequired: false,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week02OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week03OldContent: ProgramContent = {
  id: "ai4games2-week-03",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 3: Pathfinding",
  slug: "week-03",
  description: "Pathfinding algorithms and heuristics",
  type: 0,
  body: ai4games2Week03OldLecture,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week03OldAstarContent: ProgramContent = {
  id: "ai4games2-week-03-astar",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-03",
  title: "Week 3: A* Algorithm",
  slug: "astar",
  description: "Deep dive into A*",
  type: 0,
  body: ai4games2Week03OldAstar,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week03OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week04OldContent: ProgramContent = {
  id: "ai4games2-week-04",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 4: Decision Making",
  slug: "week-04",
  description: "State machines and behavior trees",
  type: 0,
  body: ai4games2Week04OldLecture,
  sortOrder: 5,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week04OldAssignmentContent: ProgramContent = {
  id: "ai4games2-week-04-assignment",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-04",
  title: "Week 4: Assignment",
  slug: "assignment",
  description: "Implement a behavior tree",
  type: 0,
  body: ai4games2Week04OldAssignment,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 180,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week04OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week05OldContent: ProgramContent = {
  id: "ai4games2-week-05",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 5: Advanced Topics",
  slug: "week-05",
  description: "Utility AI and decision systems",
  type: 0,
  body: ai4games2Week05OldLecture,
  sortOrder: 6,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week05OldCsharpContent: ProgramContent = {
  id: "ai4games2-week-05-csharp",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-05",
  title: "Week 5: C# Systems",
  slug: "csharp",
  description: "C# implementation details",
  type: 0,
  body: ai4games2Week05OldLecture2,
  sortOrder: 2,
  isRequired: false,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week05OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week06OldContent: ProgramContent = {
  id: "ai4games2-week-06",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 6: Navigation Meshes",
  slug: "week-06",
  description: "NavMesh fundamentals",
  type: 0,
  body: ai4games2Week06OldLecture,
  sortOrder: 7,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week07OldContent: ProgramContent = {
  id: "ai4games2-week-07",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 7: Steering Behaviors",
  slug: "week-07",
  description: "Flocking and movement",
  type: 0,
  body: ai4games2Week07OldLecture,
  sortOrder: 8,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week08OldContent: ProgramContent = {
  id: "ai4games2-week-08",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 8: Tactical AI",
  slug: "week-08",
  description: "Tactics and strategy",
  type: 0,
  body: ai4games2Week08OldLecture,
  sortOrder: 9,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week09OldContent: ProgramContent = {
  id: "ai4games2-week-09",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 9: Machine Learning Basics",
  slug: "week-09",
  description: "Intro to ML for games",
  type: 0,
  body: ai4games2Week09OldLecture,
  sortOrder: 10,
  isRequired: false,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week10OldContent: ProgramContent = {
  id: "ai4games2-week-10",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 10: Advanced Pathfinding",
  slug: "week-10",
  description: "Optimizations and large maps",
  type: 0,
  body: ai4games2Week10OldLecture,
  sortOrder: 11,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week11OldAssignmentContent: ProgramContent = {
  id: "ai4games2-week-11-assignment",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 11: Assignment",
  slug: "week-11",
  description: "Build a tactical AI system",
  type: 0,
  body: ai4games2Week11OldAssignment,
  sortOrder: 12,
  isRequired: true,
  estimatedMinutes: 240,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week11OldBoardContent: ProgramContent = {
  id: "ai4games2-week-11-board",
  programId: "ai4games2-program",
  parentId: "ai4games2-week-11-assignment",
  title: "Week 11: Discussion Board",
  slug: "board",
  description: "Assignment Q&A and discussion",
  type: 0,
  body: ai4games2Week11OldBoard,
  sortOrder: 2,
  isRequired: false,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2Week11OldAssignmentContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week12OldContent: ProgramContent = {
  id: "ai4games2-week-12",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 12: Emergent Behavior",
  slug: "week-12",
  description: "Complex systems and emergence",
  type: 0,
  body: ai4games2Week12OldLecture,
  sortOrder: 13,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2Week13OldContent: ProgramContent = {
  id: "ai4games2-week-13",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Week 13: Modern AI Tools",
  slug: "week-13",
  description: "LLMs and game AI",
  type: 0,
  body: ai4games2Week13OldLecture,
  sortOrder: 14,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

export const ai4games2ExtraOldContent: ProgramContent = {
  id: "ai4games2-extras-llms",
  programId: "ai4games2-program",
  parentId: "ai4games2-old",
  title: "Extra: LLMs for Game AI",
  slug: "extras-llms",
  description: "Exploring LLMs in game AI",
  type: 0,
  body: ai4games2ExtraOldLecture,
  sortOrder: 15,
  isRequired: false,
  estimatedMinutes: 60,
  visibility: 1,
  program: ai4games2Program,
  parent: ai4games2OldContent,
  children: [],
  contentInteractions: [],
  createdAt: "2023-01-01T00:00:00Z",
  updatedAt: "2023-01-01T00:00:00Z",
};

// Set up children arrays for nested content (week sub-items)
ai4games2Week01LectureContent.children = [
  ai4games2Week01ReadingsContent,
  ai4games2Week01QuizContent,
  ai4games2Week01SetupContent,
  ai4games2Week01AssignmentContent,
];
ai4games2Week02LectureContent.children = [
  ai4games2Week02ReadingsContent,
  ai4games2Week02SlidesContent,
  ai4games2Week02QuizContent,
  ai4games2Week02AssignmentContent,
];
ai4games2Week03LectureContent.children = [
  ai4games2Week03ReadingsContent,
  ai4games2Week03QuizContent,
  ai4games2Week03AssignmentContent,
];
ai4games2Week04SlidesContent.children = [
  ai4games2Week04LectureContent,
  ai4games2Week04ReadingsContent,
  ai4games2Week04QuizContent,
  ai4games2Week04AssignmentContent,
];
ai4games2Week05SlidesContent.children = [
  ai4games2Week05LectureContent,
  ai4games2Week05ReadingsContent,
  ai4games2Week05QuizContent,
  ai4games2Week05AssignmentContent,
];
ai4games2Week06SlidesContent.children = [
  ai4games2Week06LectureContent,
  ai4games2Week06ReadingsContent,
  ai4games2Week06QuizContent,
];
ai4games2Week07SlidesContent.children = [
  ai4games2Week07LectureContent,
  ai4games2Week07ReadingsContent,
];
ai4games2Week10SlidesContent.children = [
  ai4games2Week10LectureContent,
  ai4games2Week10ReadingsContent,
  ai4games2Week10QuizContent,
  ai4games2Week10AssignmentContent,
];
ai4games2Week11SlidesContent.children = [
  ai4games2Week11LectureContent,
  ai4games2Week11ReadingsContent,
  ai4games2Week11QuizContent,
  ai4games2Week11AssignmentContent,
];
ai4games2Week12SlidesContent.children = [
  ai4games2Week12LectureContent,
  ai4games2Week12ReadingsContent,
  ai4games2Week12QuizContent,
  ai4games2Week12AssignmentContent,
];
ai4games2Week13SlidesContent.children = [
  ai4games2Week13LectureContent,
  ai4games2Week13ReadingsContent,
  ai4games2Week13AssignmentContent,
];
ai4games2Week02OldContent.children = [ai4games2Week02OldPcgContent];
ai4games2Week03OldContent.children = [ai4games2Week03OldAstarContent];
ai4games2Week04OldContent.children = [ai4games2Week04OldAssignmentContent];
ai4games2Week05OldContent.children = [ai4games2Week05OldCsharpContent];
ai4games2Week11OldAssignmentContent.children = [ai4games2Week11OldBoardContent];

// Set up children for the old content container
ai4games2Week01OldLectureContent.children = [ai4games2Week01OldReadingsContent];
ai4games2OldContent.children = [
  ai4games2Week01OldLectureContent,
  ai4games2Week02OldContent,
  ai4games2Week03OldContent,
  ai4games2Week04OldContent,
  ai4games2Week05OldContent,
  ai4games2Week06OldContent,
  ai4games2Week07OldContent,
  ai4games2Week08OldContent,
  ai4games2Week09OldContent,
  ai4games2Week10OldContent,
  ai4games2Week11OldAssignmentContent,
  ai4games2Week12OldContent,
  ai4games2Week13OldContent,
  ai4games2ExtraOldContent,
];

ai4games2Product.productPrograms = [ai4games2ProductProgram];
ai4games2Program.programContents = [
  ai4games2SyllabusContent,
  ai4games2Week01LectureContent,
  ai4games2Week02LectureContent,
  ai4games2Week03LectureContent,
  ai4games2Week04SlidesContent,
  ai4games2Week05SlidesContent,
  ai4games2Week06SlidesContent,
  ai4games2Week07SlidesContent,
  ai4games2Week08MidtermContent,
  ai4games2Week09FinalProjectContent,
  ai4games2Week10SlidesContent,
  ai4games2Week11SlidesContent,
  ai4games2Week12SlidesContent,
  ai4games2Week13SlidesContent,
  ai4games2OldContent,
];

export default ai4games2Program;