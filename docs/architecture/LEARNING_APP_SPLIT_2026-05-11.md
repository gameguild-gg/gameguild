# Learning App Split

## Goal

Separate Game Guild learning delivery into three clear surfaces:

- `apps/web`: public website and course catalog
- `apps/web` dashboard routes: internal course authoring and management
- `apps/learning`: student attendance and course consumption

## Current State

- Course authoring already lives under the dashboard route tree in `apps/web`.
- The public catalog already lives in `apps/web`.
- The learner experience still lives in `apps/web` and mixes real API-backed logic with older mock-era behavior.

## Target Ownership

### Dashboard

Dashboard remains the authoring console for:

- creating courses
- editing course identity and listing metadata
- editing content structure and lessons
- managing enrollments, analytics, settings, and launch controls

### Web

Web remains the public surface for:

- marketing pages
- course catalog
- course landing pages
- enrollment and discovery flows

### Learning

Learning becomes the dedicated student surface for:

- accessing enrolled courses
- attending lessons
- tracking progress
- submitting activities and assessments
- certificate and completion flows

## Route Strategy

### Keep in `apps/web`

- `/courses`
- `/courses/[slug]`
- `/dashboard/learning/courses/*`

### Move to `apps/learning`

- `/courses/[slug]/content`
- future learner home routes such as `/me/courses`
- learner progress, certificate, and submission flows

## Delivery Order

1. Finish dashboard editing and listing behavior in the existing dashboard.
2. Keep the public catalog on `apps/web` and remove stale static catalog claims.
3. Create `apps/learning` as a dedicated Next.js app.
4. Move the learner attendance route and learner-facing components into `apps/learning`.
5. Replace remaining mock learner behaviors with generated-client-only API flows.
6. Validate cross-app navigation between public web and learning.

## First Increment Implemented

This document accompanies the first implementation slice:

- scaffold `apps/learning`
- wire a real course attendance route in the new app
- keep it backed by `@game-guild/client`
- preserve dashboard authoring in the existing dashboard surface

## Follow-up Work

- wire shared authentication and sign-in UX across `web` and `learning`
- move learner progress mutations and activity submission into `apps/learning`
- align course launch state so dashboard preview opens the learning app directly
- update deployment manifests so `apps/learning` can be deployed as its own service
