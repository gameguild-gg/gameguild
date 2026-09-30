/**
 * Blog asset surface. The composed GameGuild asset repository is scope-agnostic
 * — the caller passes `assetScope={{ type: "BlogPost", id: postId }}` to
 * `LexicalSurface`, and the remote provider forwards it as
 * `parentResourceType`/`parentResourceId`. Re-exported here so blog components
 * depend on `lib/blogs`, not `lib/learning`.
 */
export { getLearningAssetRepository as getBlogAssetRepository } from '@/lib/learning/assets/learning-asset-repository';
export type { ComposedAssetRepository } from '@game-guild/assets/providers';
