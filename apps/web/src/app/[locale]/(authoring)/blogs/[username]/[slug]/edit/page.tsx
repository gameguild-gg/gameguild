import { getViewerBlogAuthor } from "@/lib/blogs/actions";
import { getMyPostBySlug } from "@/lib/blogs/queries";
import {
  BlogEditorWorkspace,
  type BlogEditorWorkspaceProps,
} from "@/components/blogs/editor/blog-editor-workspace";
import { notFound } from "next/navigation";

export default async function EditBlogPostPage({
  params,
}: PageProps<"/[locale]/blogs/[username]/[slug]/edit">) {
  const { username, slug } = await params;
  const [lookup, viewer] = await Promise.all([
    getMyPostBySlug(username, slug),
    getViewerBlogAuthor(),
  ]);
  if (lookup.status !== "ok") notFound();
  if (!viewer.userId) notFound();

  const post = lookup.post;
  const coauthors: BlogEditorWorkspaceProps["coauthors"] = [
    {
      userId: post.primaryAuthorId,
      handle: username,
      displayName: null,
      isPrimary: true,
    },
  ];

  return (
    <BlogEditorWorkspace
      post={post}
      viewerUserId={viewer.userId}
      primaryAuthorHandle={username}
      coauthors={coauthors}
    />
  );
}
