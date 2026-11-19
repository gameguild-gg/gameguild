/** @typedef {import('../src/lib/coding-assignment/types').CodingAssignmentContent} CodingAssignmentContent */

/**
 * The historical cycle deliberately covers both execution channels in one
 * definition. Its explicit experience is function; test kinds do not infer it.
 * @param {number} maxScore
 * @returns {CodingAssignmentContent}
 */
export function buildCodingContent(maxScore) {
  return {
    Type: "coding-assignment",
    Version: 1,
    ExerciseType: "function",
    Environment: {
      Language: "cpp",
      Tools: "clang",
      LibBundle: null,
      AllowStudentCreateFiles: false,
    },
    Data: {
      Files: {
        "/user/main.cpp": {
          Content:
            'extern "C" int printf(const char*, ...);\n' +
            'extern "C" int add(int a, int b) {\n' +
            "    return 0; // TODO: replace with your implementation\n" +
            "}\n" +
            "\n" +
            "int main() {\n" +
            '    printf("%d\\n", add(2, 3));\n' +
            "    return add(2, 3);\n" +
            "}\n",
          Encoding: "text",
          Visibility: "Public",
          Modifiable: true,
        },
      },
    },
    Tests: {
      Public: [
        { kind: "standard", Name: "prints-sum", Stdin: "", Stdout: "5\n", Weight: 1 },
        {
          kind: "functional",
          Name: "add-fn",
          Function: {
            FunctionName: "add",
            Parameters: [{ Type: "integer", Name: "a" }, { Type: "integer", Name: "b" }],
            ReturnType: { Type: "integer" },
          },
          Cases: [{
            Inputs: [{ Type: "integer", Content: 2 }, { Type: "integer", Content: 3 }],
            Expected: { Type: "integer", Content: 5 },
          }],
          Weight: 1,
        },
      ],
      Private: [{ kind: "standard", Name: "secret-exit", Stdin: "", Stdout: "5\n", ExitCode: 5, Weight: 1 }],
    },
    Grading: { MaxScore: maxScore },
  };
}
