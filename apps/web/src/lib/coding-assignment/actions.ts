"use server";

/**
 * Server action wrapper around `putCodingAssignment` so client components can
 * save a CodingAssignmentContent without importing the auth-dependent client
 * module (which transitively pulls `next/headers`).
 *
 * Server components continue to call the wrappers in `./client.ts` directly.
 */

import { putCodingAssignment as putCodingAssignmentClient } from "./client";
import type {
  CodingAssignmentContent,
  CodingEnvironment,
  FileVisibility,
  FunctionParameter,
  FunctionParameterWithName,
  FunctionParameterType,
  FunctionParameterValue,
  StandardTest,
  FunctionalTestGroup,
  FunctionalTestCase,
  Test,
} from "./types";
import { revalidatePath } from "next/cache";

export type {
  CodingAssignmentContent,
  CodingEnvironment,
  FileVisibility,
  FunctionParameter,
  FunctionParameterWithName,
  FunctionParameterType,
  FunctionParameterValue,
  StandardTest,
  FunctionalTestGroup,
  FunctionalTestCase,
  Test,
} from "./types";

export type PutCodingAssignmentActionResult =
  | { success: true }
  | { success: false; error: string };

export async function putCodingAssignmentAction(
  programId: string,
  contentId: string,
  content: CodingAssignmentContent,
): Promise<PutCodingAssignmentActionResult> {
  const result = await putCodingAssignmentClient(programId, contentId, content);
  if (result.success) {
    revalidatePath(
      `/dashboard/platform/learning/courses/${programId}/content/${contentId}`,
    );
  }
  return result;
}
