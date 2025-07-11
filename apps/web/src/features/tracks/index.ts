// Re-export all track-specific components
export { default as TrackCard } from './track-card';
export { default as TrackGrid } from './track-grid';
export { default as TrackFilters } from './track-filters';
export { default as TrackDetail } from './track-detail';
export { default as TrackProgress } from './track-progress';
export { default as TrackRoadmap } from './track-roadmap';
export { default as TrackCertification } from './track-certification';
export { default as EnhancedTracksPage } from './enhanced-tracks-page';
export { default as TrackDetailPage } from './track-detail-page';

// Legacy exports for backward compatibility
export { TrackGrid as LegacyTrackGrid } from '@/components/tracks/track-grid';
export { TrackFilters as LegacyTrackFilters } from '@/components/tracks/track-filters';
