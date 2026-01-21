// BASE QUIZ ASSESSMENT ENTRY

class StudentFeedbackForQuizEntry {
    CorrectAnswerFeedback: string | null; // markdown / gglexical content for correct answer feedback
    IncorrectAnswerFeedback: string | null; // markdown / gglexical content for incorrect answer feedback
    GeneralFeedback: string | null; // markdown / gglexical content for general feedback
}

class QuizAssessmentEntry {
    title: string;
    stem: string; // markdown / gglexical content for the question stem
    points: number;
    feedback: StudentFeedbackForQuizEntry | null;
}



// FILL IN THE BLANK

enum QuizFillInTheBlankOptionType {
    regex = "REGEX", // on the interface, we will build the regex for them by providing options like contains, exact match, levenshteinDistance, list of correct answers, or allow them to simply wrie the regex etc.
    dropdown = "DROPDOWN", // it will be radio buttons or dropdown selection from a list of options
    wordbank = "WORDBANK", // it will be drag and drop from a word bank
}

class QuizFillInTheBlankOption {
    type: QuizFillInTheBlankOptionType;
}

class QuizFillInTheBlankOptionOpen extends QuizFillInTheBlankOption {
    type: QuizFillInTheBlankOptionType.regex;
    regex: string; // the regex pattern to match
}

class QuizFillInTheBlankOptionDropdown extends QuizFillInTheBlankOption {
    type: QuizFillInTheBlankOptionType.dropdown
    distractors: string[]; // list of choices for dropdown
    answer: string; // the correct choice
}

class QuizFillInTheBlankOptionWordbank extends QuizFillInTheBlankOption {
    type: QuizFillInTheBlankOptionType.wordbank
    distractors: string[]; // list of words in the word bank
    answers: string[]; // list of correct answers from the word bank
}

class QuizFillInTheBlankEntry extends QuizAssessmentEntry {
    filltheblankOptions: QuizFillInTheBlankOption[];
}



// FORMULA

class QuizFormulaEntryVariable {
    name: string // or letter
    minValue: number;
    maxValue: number;
    decimalPlaces: number; // number of decimal places to round to
}

class QuizFormulaEntry extends QuizAssessmentEntry {
    // extract the variables from the stem using `var` syntax
    variables: QuizFormulaEntryVariable[];
    formula: string; // formula to calculate the answer, e.g., (a + b) / c
    tolerance: number; // acceptable tolerance for numerical answers
}




// HOT SPOT

class Point2D {
    x: number
    y: number
}

class QuizHotSpotEntry {
    // the interface can generate shapes and store only the coordinates
    polygons: Point2D[];
}



// MATCHING

class QuizMatchingEntryPair {
    left: string; // markdown / gglexical content for left side
    right: string; // markdown / gglexical content for right side
}

class QuizMatchingEntry extends QuizAssessmentEntry {
    pairs: QuizMatchingEntryPair[];
    distractors: QuizMatchingEntryPair[]; // extra pairs that are not matched
    allowPartialCredit: boolean;
}



// QUIZ MULTIPLE CATEGORIZATION

class QuizMultipleCategorizationEntryPair {
    description: string;
    answer: string;
}

class QuizMultipleCategorizationEntry extends QuizAssessmentEntry {
    pairs: QuizMultipleCategorizationEntryPair[];
    distractors: string[];
}


// QUIZ ESSAY

enum QuizEssayEntryType {
    ShortAnswer = "SHORT_ANSWER",
    LongAnswer = "LONG_ANSWER"
}

class QuizEssayEntryOptions {
    minWordCount: number | null;
    maxWordCount: number | null;
    showCalculator: boolean;
    showRichTextEditor: boolean;
    showWordCount: boolean;
}

class QuizEssayEntry extends QuizAssessmentEntry {
    type: QuizEssayEntryType;
    options: QuizEssayEntryOptions;
}

// File upload

class QuizFileUploadEntry extends QuizAssessmentEntry {
    allowedFileTypes: string[]; // e.g., ['pdf', 'docx', 'png']
    maxFileSizeMB: number; // maximum file size in megabytes
    maxFiles: number; // maximum number of files allowed
}


// SINGLE CHOICE

class QuizSingleChoiceEntry extends QuizAssessmentEntry {
    distractors: string[]; // list of distractors
    answer: string; // the correct choice.
}

// MULTIPLE CHOICE

class QuizMultipleChoiceEntry extends QuizAssessmentEntry {
    distractors: string[]; // list of distractors
    answers: string[]; // list of correct choices. 
}

// NUMERIC ENTRY

enum QuizNumericEntryValidationType {
    Margin = "MARGIN",
    Range = "RANGE",
    Precision = "PRECISION"
}

class QuizNumericEntryBase extends QuizAssessmentEntry {
    answer: number; // the correct numeric answer
    validationType: QuizNumericEntryValidationType;
}

class QuizNumericEntryMargin extends QuizNumericEntryBase {
    validationType: QuizNumericEntryValidationType.Margin;
    margin: number; // margin of error for validation
}

class QuizNumericEntryRange extends QuizNumericEntryBase {
    validationType: QuizNumericEntryValidationType.Range;
    range: { min: number; max: number }; // acceptable range for validation
}

class QuizNumericEntryPrecision extends QuizNumericEntryBase {
    validationType: QuizNumericEntryValidationType.Precision;
    precision: { decimalPlaces: number } | { significantFigures: number }; // precision requirements
}

type QuizNumericEntry =
    | QuizNumericEntryMargin
    | QuizNumericEntryRange
    | QuizNumericEntryPrecision;

// TRUE/FALSE

class QuizTrueFalseEntry extends QuizAssessmentEntry {
    answer: boolean; // the correct answer (true or false)
}

// ORDERING

class QuizOrderingEntry extends QuizAssessmentEntry {
    items: string[]; // ordered list of items (markdown / gglexical content)
    allowPartialCredit: boolean; // whether to give partial credit for partially correct ordering
}

// QUIZ ASSESSMENT ROOT

enum QuizAssessmentType {
    Graded = "GRADED", // all submissions are graded
    Ungraded = "UNGRADED" // submissions are not graded
}

class QuizAssessment {
    id: string;
    type: QuizAssessmentType;
    points: number;

    parent: string | null; // something to link back the programcontent parent
    dueDate: string | null; // when it is due - ISO date string
    availableDate: string | null; // after which its available - ISO date string
    untilDate: string | null; // after which its not available - ISO date string

    instructions: string | null; // markdown / gglexical instructions for the quiz

    entries: QuizAssessmentEntry[];
}
