import { getViewerBlogAuthor } from "@/lib/blogs/actions";
import { NewBlogPostForm } from "@/components/blogs/editor/new-blog-post-form";

export default async function NewBlogPostPage() {
  const viewer = await getViewerBlogAuthor();

  return <NewBlogPostForm viewerHandle={viewer.handle} />;
}
