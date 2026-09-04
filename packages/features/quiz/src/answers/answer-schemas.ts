import { z } from "zod";
import type { QuizAnswer } from "./answers";
import { QuizEntryType } from "../questions/question-types";

const stringRecordSchema = z.record(z.string(), z.string());
const stringArrayRecordSchema = z.record(z.string(), z.array(z.string()));
const richTextSchema = z.record(z.string(), z.unknown()).nullable();
const spanSchema = z.object({
  start: z.number().int().nonnegative(),
  end: z.number().int().positive(),
}).strict().refine(({ start, end }) => end > start, {
  message: "Highlight span end must be greater than start.",
});

const rawQuizAnswerSchema = z.discriminatedUnion("type", [
  z.object({
    type: z.literal(QuizEntryType.SingleChoice),
    optionId: z.string().nullable(),
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.MultipleChoice),
    optionIds: z.array(z.string()),
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.TrueFalse),
    value: z.boolean().nullable(),
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.FillInTheBlank),
    values: stringRecordSchema,
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.ShortAnswer),
    value: z.string(),
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.Essay),
    richText: richTextSchema,
    plainText: z.string(),
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.Matching),
    matches: stringRecordSchema,
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.Ordering),
    itemIds: z.array(z.string()),
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.Categorization),
    categoryIdsByItem: stringArrayRecordSchema,
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.Rating),
    value: z.number().finite().nullable(),
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.Numeric),
    value: z.string(),
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.Formula),
    expression: z.string(),
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.Hotspot),
    point: z.object({
      x: z.number().finite(),
      y: z.number().finite(),
    }).strict().nullable(),
  }).strict(),
  z.object({
    type: z.literal(QuizEntryType.Highlight),
    spans: z.array(spanSchema),
  }).strict(),
]);

type ParsedQuizAnswer = z.infer<typeof rawQuizAnswerSchema>;
const _answerTypeCheck: QuizAnswer = null as unknown as ParsedQuizAnswer;
const _parsedTypeCheck: ParsedQuizAnswer = null as unknown as QuizAnswer;
void _answerTypeCheck;
void _parsedTypeCheck;

export const quizAnswerSchema: z.ZodType<QuizAnswer> = rawQuizAnswerSchema;

export function parseQuizAnswer(value: unknown): QuizAnswer {
  return quizAnswerSchema.parse(value);
}

export function safeParseQuizAnswer(value: unknown) {
  return quizAnswerSchema.safeParse(value);
}
