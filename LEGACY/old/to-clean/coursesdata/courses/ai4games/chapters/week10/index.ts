import { Api } from '@/types/course-types';
import { createChapter, createLecture } from '@/data/coursesLib';
import lecture from './lecture.md';
import ChapterEntity = Api.ChapterEntity;
import LectureEntity = Api.LectureEntity;

const week10lectures: LectureEntity[] = [];

week10lectures.push(createLecture('10-1', 'chess-ds', 'Chess Data Structures', 'Chess Data Structures', lecture, 1) as LectureEntity);

const Chapter10 = createChapter('10', 'week10', 'Week 10: Chess', 'Chess.', 10, ['10-1'], week10lectures) as ChapterEntity;

// set chapter for each lecture
for (let i = 0; i < week10lectures.length; i++) {
  week10lectures[i].chapter = Chapter10;
}

export default Chapter10;
