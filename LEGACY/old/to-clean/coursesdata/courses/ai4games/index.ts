import { Api } from '@/types/course-types';
import syllabusBody from './syllabus.md';
import Chapter01 from './chapters/week01';
import Chapter02 from './chapters/week02';
import Chapter03 from './chapters/week03';
import Chapter04 from './chapters/week04';
import Chapter05 from './chapters/week05';
import Chapter06 from './chapters/week06';
import Chapter07 from './chapters/week07';
import Chapter08 from './chapters/week08';
import Chapter09 from './chapters/week09';
import Chapter10 from './chapters/week10';
import Chapter11 from './chapters/week11';
import Chapter12 from './chapters/week12';
import Chapter13 from './chapters/week13';
import ChapterEntity = Api.ChapterEntity;
import CourseEntity = Api.CourseEntity;
import ImageEntity = Api.ImageEntity;
import UserEntity = Api.UserEntity;
import LectureEntity = Api.LectureEntity;

const course: CourseEntity = {} as CourseEntity;

course.title = 'AI for Games';
course.slug = 'ai4games';
course.id = '1';
course.summary =
  'Students with a firm foundation in the basic techniques of artificial intelligence for games will apply their skills to program advanced pathfinding algorithms, artificial opponents, scripting tools and other real-time drivers for non-playable agents. The goal of the course is to provide finely-tuned artificial competition for players using all the rules followed by a human.';
course.owner = {
  id: '1',
  username: 'admin',
} as UserEntity;

course.body = syllabusBody;

const chapters: ChapterEntity[] = [];
chapters.push(Chapter01);
chapters.push(Chapter02);
chapters.push(Chapter03);
chapters.push(Chapter04);
chapters.push(Chapter05);
chapters.push(Chapter06);
chapters.push(Chapter07);
chapters.push(Chapter08);
chapters.push(Chapter09);
chapters.push(Chapter10);
chapters.push(Chapter11);
chapters.push(Chapter12);
chapters.push(Chapter13);

const lectures: LectureEntity[] = [];
lectures.push(
  ...Chapter01.lectures,
  ...Chapter02.lectures,
  ...Chapter03.lectures,
  ...Chapter04.lectures,
  ...Chapter05.lectures,
  ...Chapter06.lectures,
  ...Chapter07.lectures,
  ...Chapter08.lectures,
  ...Chapter09.lectures,
  ...Chapter10.lectures,
  ...Chapter11.lectures,
  ...Chapter12.lectures,
  ...Chapter13.lectures,
);

// set course for all lectures and chapters
lectures.forEach((lecture) => (lecture.course = course));
chapters.forEach((chapter) => (chapter.course = course));

course.chapters = chapters;
course.lectures = lectures;

course.thumbnail = {
  filename: 'ai-in-gaming-main-1600-1.jpg',
  path: 'https://pixelplex.io/wp-content/uploads/2021/11',
  description: 'AI for Games course thumbnail',
} as ImageEntity;

export default course;
