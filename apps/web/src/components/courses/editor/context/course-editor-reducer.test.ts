import { describe, expect, it } from 'vitest';
import type { Course } from '@/lib/courses';
import { CourseEditorActionType } from '../types';
import {
  courseEditorReducer,
  createEmptyCourse,
  createInitialCourseEditorState,
  validateCourse,
} from './course-editor-reducer';

const validCourse: Course = {
  id: 'course-1',
  title: 'Production Game AI',
  description: 'A complete production course for game AI behaviors.',
  category: 'Game AI',
  level: 'Advanced',
  duration: '12h',
  enrolledStudents: 12,
  rating: 4.8,
  price: 199,
  image: '/courses/game-ai.jpg',
  slug: 'production-game-ai',
  instructor: {
    name: 'Alex Instructor',
    avatar: '/avatars/alex.jpg',
  },
  isEnrolled: false,
  progress: 0,
  certification: true,
};

describe('course editor reducer utilities', () => {
  it('creates a real empty course and validates required publishing fields', () => {
    const emptyCourse = createEmptyCourse();

    expect(emptyCourse.id).toMatch(/^course-/);
    expect(emptyCourse.level).toBe('Beginner');
    expect(validateCourse(emptyCourse).map((error) => error.field)).toEqual([
      'title',
      'description',
      'category',
      'slug',
      'instructor.name',
    ]);
    expect(validateCourse(validCourse)).toEqual([]);
  });

  it('creates an initial editor state with merged config', () => {
    const state = createInitialCourseEditorState({
      mode: 'edit',
      config: {
        ...createInitialCourseEditorState().config,
        autoSave: false,
        maxHistorySteps: 5,
      },
    });

    expect(state.mode).toBe('edit');
    expect(state.config.autoSave).toBe(false);
    expect(state.config.maxHistorySteps).toBe(5);
    expect(state.lastUpdated).toBeInstanceOf(Date);
  });

  it('loads and edits course state instead of returning the same object', () => {
    const initialState = createInitialCourseEditorState();
    const loaded = courseEditorReducer(initialState, {
      type: CourseEditorActionType.SET_COURSE,
      payload: validCourse,
    });

    const updated = courseEditorReducer(loaded, {
      type: CourseEditorActionType.UPDATE_COURSE_FIELD,
      payload: { field: 'title', value: 'Updated Game AI' },
    });

    expect(loaded.course?.title).toBe('Production Game AI');
    expect(updated.course?.title).toBe('Updated Game AI');
    expect(updated.hasUnsavedChanges).toBe(true);
    expect(updated).not.toBe(loaded);
  });

  it('adds chapters and lessons while maintaining content totals', () => {
    const loaded = courseEditorReducer(createInitialCourseEditorState(), {
      type: CourseEditorActionType.SET_COURSE,
      payload: validCourse,
    });

    const withChapter = courseEditorReducer(loaded, {
      type: CourseEditorActionType.ADD_CHAPTER,
      payload: {
        id: '',
        title: 'Behavior Trees',
        description: 'Decision-making foundations.',
        order: 0,
        isPublished: false,
        lessons: [],
      },
    });

    const chapter = (withChapter.course as Course & { content?: { chapters: Array<{ id: string }> } }).content?.chapters[0];
    expect(chapter?.id).toBeTruthy();

    const withLesson = courseEditorReducer(withChapter, {
      type: CourseEditorActionType.ADD_LESSON,
      payload: {
        chapterId: chapter!.id,
        lesson: {
          id: '',
          title: 'Selector nodes',
          description: 'Build robust selector behavior.',
          content: 'Lesson content',
          duration: 45,
          order: 0,
          isPublished: false,
        },
      },
    });

    const content = (withLesson.course as Course & {
      content?: { chapters: Array<{ lessons: Array<{ title: string }> }>; totalLessons?: number; totalDuration?: number };
    }).content;

    expect(content?.chapters[0].lessons[0].title).toBe('Selector nodes');
    expect(content?.totalLessons).toBe(1);
    expect(content?.totalDuration).toBe(45);
    expect(withLesson.canUndo).toBe(true);
  });
});
