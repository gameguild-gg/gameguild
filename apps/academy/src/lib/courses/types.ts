export interface Chapter {
  id: string;
  title: string;
  image: string;
  coverImage: string;
  description: string;
  duration: string;
  progress?: number;
}

export interface Course {
  name: string;
  slug: string;
  chapters: Chapter[];
}
