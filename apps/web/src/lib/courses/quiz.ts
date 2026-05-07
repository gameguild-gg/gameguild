// // BASE QUIZ ASSESSMENT ENTRY

// class StudentFeedbackForQuizEntry {
//     CorrectAnswerFeedback: string | null; // markdown / block-content-editor content for correct answer feedback
//     IncorrectAnswerFeedback: string | null; // markdown / block-content-editor content for incorrect answer feedback
//     GeneralFeedback: string | null; // markdown / block-content-editor content for general feedback
// }

// enum QuizAssessmentEntryType {
//     FillInTheBlank = "FILL_IN_THE_BLANK",
//     Formula = "FORMULA",
//     HotSpot = "HOT_SPOT",
//     Matching = "MATCHING",
//     MultipleCategorization = "MULTIPLE_CATEGORIZATION",
//     Essay = "ESSAY",
//     FileUpload = "FILE_UPLOAD",
//     SingleChoice = "SINGLE_CHOICE",
//     MultipleChoice = "MULTIPLE_CHOICE",
//     NumericEntry = "NUMERIC_ENTRY",
//     TrueFalse = "TRUE_FALSE",
//     Ordering = "ORDERING"
// }

// class QuizAssessmentEntry {
//     title: string;
//     stem: string; // markdown / block-content-editor content for the question stem
//     points: number;
//     feedback: StudentFeedbackForQuizEntry | null;
//     type: QuizAssessmentEntryType;
// }



// // FILL IN THE BLANK

// enum QuizFillInTheBlankOptionType {
//     regex = "REGEX", // on the interface, we will build the regex for them by providing options like contains, exact match, levenshteinDistance, list of correct answers, or allow them to simply wrie the regex etc.
//     dropdown = "DROPDOWN", // it will be radio buttons or dropdown selection from a list of options
//     wordbank = "WORDBANK", // it will be drag and drop from a word bank
// }

// class QuizFillInTheBlankOption {
//     type: QuizFillInTheBlankOptionType;
// }

// class QuizFillInTheBlankOptionOpen extends QuizFillInTheBlankOption {
//     type: QuizFillInTheBlankOptionType.regex;
//     regex: string; // the regex pattern to match
// }

// class QuizFillInTheBlankOptionDropdown extends QuizFillInTheBlankOption {
//     type: QuizFillInTheBlankOptionType.dropdown
//     distractors: string[]; // list of choices for dropdown
//     answer: string; // the correct choice
// }

// class QuizFillInTheBlankOptionWordbank extends QuizFillInTheBlankOption {
//     type: QuizFillInTheBlankOptionType.wordbank
//     distractors: string[]; // list of words in the word bank
//     answers: string[]; // list of correct answers from the word bank
// }

// class QuizFillInTheBlankEntry extends QuizAssessmentEntry {
//     filltheblankOptions: QuizFillInTheBlankOption[];
//     type: QuizAssessmentEntryType;.FillInTheBlank;
// }



// // FORMULA

// class QuizFormulaEntryVariable {
//     name: string // or letter
//     minValue: number;
//     maxValue: number;
//     decimalPlaces: number; // number of decimal places to round to
// }

// class QuizFormulaEntry extends QuizAssessmentEntry {
//     // extract the variables from the stem using `var` syntax
//     variables: QuizFormulaEntryVariable[];
//     formula: string; // formula to calculate the answer, e.g., (a + b) / c
//     tolerance: number; // acceptable tolerance for numerical answers
//     type: QuizAssessmentEntryType.Formula;
// }




// // HOT SPOT

// class Point2D {
//     x: number
//     y: number
// }

// class QuizHotSpotEntry {
//     // the interface can generate shapes and store only the coordinates
//     polygons: Point2D[];
// }



// // MATCHING

// class QuizMatchingEntryPair {
//     left: string; // markdown / block-content-editor content for left side
//     right: string; // markdown / block-content-editor content for right side
// }

// class QuizMatchingEntry extends QuizAssessmentEntry {
//     pairs: QuizMatchingEntryPair[];
//     distractors: QuizMatchingEntryPair[]; // extra pairs that are not matched
//     allowPartialCredit: boolean;
//     type: QuizAssessmentEntryType.Matching;
// }



// // QUIZ MULTIPLE CATEGORIZATION

// class QuizMultipleCategorizationEntryPair {
//     description: string;
//     answer: string;
// }

// class QuizMultipleCategorizationEntry extends QuizAssessmentEntry {
//     pairs: QuizMultipleCategorizationEntryPair[];
//     distractors: string[];
//     type: QuizAssessmentEntryType;.MultipleCategorization;
// }


// // QUIZ ESSAY

// enum QuizEssayEntryType {
//     ShortAnswer = "SHORT_ANSWER",
//     LongAnswer = "LONG_ANSWER"
// }

// class QuizEssayEntryOptions {
//     minWordCount: number | null;
//     maxWordCount: number | null;
//     showCalculator: boolean;
//     showRichTextEditor: boolean;
//     showWordCount: boolean;
// }

// class QuizEssayEntry extends QuizAssessmentEntry {
//     size: QuizEssayEntryType;
//     options: QuizEssayEntryOptions;
//     type: QuizAssessmentEntryType.Essay;
// }

// // File upload

// class QuizFileUploadEntry extends QuizAssessmentEntry {
//     allowedFileTypes: string[]; // e.g., ['pdf', 'docx', 'png']
//     maxFileSizeMB: number; // maximum file size in megabytes
//     maxFiles: number; // maximum number of files allowed
//     type: QuizAssessmentEntryType.FileUpload;
// }


// // SINGLE CHOICE

// class QuizSingleChoiceEntry extends QuizAssessmentEntry {
//     distractors: string[]; // list of distractors
//     answer: string; // the correct choice.
//     type: QuizAssessmentEntryType.SingleChoice;
// }

// // MULTIPLE CHOICE

// class QuizMultipleChoiceEntry extends QuizAssessmentEntry {
//     distractors: string[]; // list of distractors
//     answers: string[]; // list of correct choices. 
//     type: QuizAssessmentEntryType.MultipleChoice;
// }

// // NUMERIC ENTRY

// enum QuizNumericEntryValidationType {
//     Margin = "MARGIN",
//     Range = "RANGE",
//     Precision = "PRECISION"
// }

// class QuizNumericEntryBase extends QuizAssessmentEntry {
//     answer: number; // the correct numeric answer
//     validationType: QuizNumericEntryValidationType;
//     type: QuizAssessmentEntryType.NumericEntry;
// }

// class QuizNumericEntryMargin extends QuizNumericEntryBase {
//     validationType: QuizNumericEntryValidationType.Margin;
//     margin: number; // margin of error for validation
// }

// class QuizNumericEntryRange extends QuizNumericEntryBase {
//     validationType: QuizNumericEntryValidationType.Range;
//     range: { min: number; max: number }; // acceptable range for validation
// }

// class QuizNumericEntryPrecision extends QuizNumericEntryBase {
//     validationType: QuizNumericEntryValidationType.Precision;
//     precision: { decimalPlaces: number } | { significantFigures: number }; // precision requirements
// }

// type QuizNumericEntry =
//     | QuizNumericEntryMargin
//     | QuizNumericEntryRange
//     | QuizNumericEntryPrecision;

// // TRUE/FALSE

// class QuizTrueFalseEntry extends QuizAssessmentEntry {
//     answer: boolean; // the correct answer (true or false)
//     type: QuizAssessmentEntryType.TrueFalse;

// // ORDERING

// class QuizOrderingEntry extends QuizAssessmentEntry {
//     items: string[]; // ordered list of items (markdown / block-content-editor content)
//     allowPartialCredit: boolean; // whether to give partial credit for partially correct ordering
//     type: QuizAssessmentEntryType.Ordering;
// }

// // QUIZ ASSESSMENT ROOT

// enum QuizAssessmentType {
//     Graded = "GRADED", // all submissions are graded
//     Ungraded = "UNGRADED" // submissions are not graded
// }

// class QuizAssessment {
//     id: string;
//     type: QuizAssessmentType;
//     points: number;

//     parent: string | null; // something to link back the programcontent parent
//     dueDate: string | null; // when it is due - ISO date string
//     availableDate: string | null; // after which its available - ISO date string
//     untilDate: string | null; // after which its not available - ISO date string

//     instructions: string | null; // markdown / block-content-editor instructions for the quiz

//     entries: QuizAssessmentEntry[];
// }


// const t: QuizAssessment = {
//     id: "quiz1",
//     type: QuizAssessmentType.Graded,
//     points: 100,
//     parent: null,
//     dueDate: null,
//     availableDate: null,
//     untilDate: null,
//     instructions: "Please complete the quiz.",
//     entries: [
//         new QuizTrueFalseEntry({
//             title: "Sample True/False Question",
//             stem: "The sky is blue.",
//             points: 10,
//             answer: true,
//             feedback: {
//                 CorrectAnswerFeedback: "Correct! The sky appears blue due to the scattering of sunlight.",
//                 IncorrectAnswerFeedback: "Incorrect. The sky appears blue due to the scattering of sunlight.",
//                 GeneralFeedback: "The sky's color is a result of Rayleigh scattering."
//             }
//         })
//     ]
// };