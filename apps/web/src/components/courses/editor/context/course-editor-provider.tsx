'use client';

import React, { createContext, useContext, useState } from 'react';

interface CourseSession {
  id: string;
  title: string;
  startDate: string;
  endDate: string;
  capacity: number;
}

interface CourseEditorState {
  delivery: {
    mode: string;
    sessions: CourseSession[];
    timezone: string;
  };
  accessWindow: {
    start: string;
    end: string;
  };
  enrollmentWindow: {
    start: string;
    end: string;
  };
}

interface CourseEditorContextType {
  state: CourseEditorState;
  setDeliveryMode: (mode: string) => void;
  setAccessWindow: (window: { start: string; end: string }) => void;
  setEnrollmentWindow: (window: { start: string; end: string }) => void;
  addSession: (session: Omit<CourseSession, 'id'>) => void;
  removeSession: (sessionId: string) => void;
  setTimezone: (timezone: string) => void;
}

const CourseEditorContext = createContext<CourseEditorContextType | undefined>(undefined);

export function CourseEditorProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<CourseEditorState>({
    delivery: {
      mode: 'self-paced',
      sessions: [],
      timezone: 'UTC',
    },
    accessWindow: {
      start: '',
      end: '',
    },
    enrollmentWindow: {
      start: '',
      end: '',
    },
  });

  const setDeliveryMode = (mode: string) => {
    setState((prev) => ({
      ...prev,
      delivery: { ...prev.delivery, mode },
    }));
  };

  const setAccessWindow = (window: { start: string; end: string }) => {
    setState((prev) => ({ ...prev, accessWindow: window }));
  };

  const setEnrollmentWindow = (window: { start: string; end: string }) => {
    setState((prev) => ({ ...prev, enrollmentWindow: window }));
  };

  const addSession = (sessionData: Omit<CourseSession, 'id'>) => {
    const newSession = { ...sessionData, id: Date.now().toString() };
    setState((prev) => ({
      ...prev,
      delivery: {
        ...prev.delivery,
        sessions: [...prev.delivery.sessions, newSession],
      },
    }));
  };

  const removeSession = (sessionId: string) => {
    setState((prev) => ({
      ...prev,
      delivery: {
        ...prev.delivery,
        sessions: prev.delivery.sessions.filter((session) => session.id !== sessionId),
      },
    }));
  };

  const setTimezone = (timezone: string) => {
    setState((prev) => ({
      ...prev,
      delivery: { ...prev.delivery, timezone },
    }));
  };

  return (
    <CourseEditorContext.Provider
      value={{
        state,
        setDeliveryMode,
        setAccessWindow,
        setEnrollmentWindow,
        addSession,
        removeSession,
        setTimezone,
      }}
    >
      {children}
    </CourseEditorContext.Provider>
  );
}

export function useCourseEditor() {
  const context = useContext(CourseEditorContext);
  if (context === undefined) {
    throw new Error('useCourseEditor must be used within a CourseEditorProvider');
  }
  return context;
}
