using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260716160000_AddAssessmentIntegrityGuards")]
public partial class AddAssessmentIntegrityGuards : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE \"Assessments\" SET \"MaxScore\" = 1 WHERE \"MaxScore\" <= 0;");
        migrationBuilder.Sql("UPDATE \"Assessments\" SET \"PassingScore\" = 0 WHERE \"PassingScore\" < 0;");
        migrationBuilder.Sql("UPDATE \"Assessments\" SET \"PassingScore\" = \"MaxScore\" WHERE \"PassingScore\" > \"MaxScore\";");
        migrationBuilder.Sql("UPDATE \"AssessmentSubmissions\" AS submission SET \"Score\" = NULL WHERE \"Score\" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM \"Assessments\" AS assessment WHERE assessment.\"Id\" = submission.\"AssessmentId\");");
        migrationBuilder.Sql("UPDATE \"AssessmentSubmissions\" AS submission SET \"Score\" = CASE WHEN submission.\"Score\" < 0 THEN 0 WHEN submission.\"Score\" > assessment.\"MaxScore\" THEN assessment.\"MaxScore\" ELSE submission.\"Score\" END FROM \"Assessments\" AS assessment WHERE submission.\"AssessmentId\" = assessment.\"Id\" AND submission.\"Score\" IS NOT NULL;");
        migrationBuilder.Sql("""
            WITH numbered_attempts AS (
                SELECT "Id",
                       ROW_NUMBER() OVER (
                           PARTITION BY "AssessmentId", "EnrollmentId"
                           ORDER BY "StartedAt", "CreatedAt", "Id")::integer AS "AttemptNumber"
                FROM "AssessmentSubmissions"
            )
            UPDATE "AssessmentSubmissions" AS submission
            SET "AttemptNumber" = numbered_attempts."AttemptNumber"
            FROM numbered_attempts
            WHERE submission."Id" = numbered_attempts."Id";
            """);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Assessments_ScoreRange",
            table: "Assessments",
            sql: "\"MaxScore\" > 0 AND \"PassingScore\" >= 0 AND \"PassingScore\" <= \"MaxScore\"");
        migrationBuilder.AddCheckConstraint(
            name: "CK_AssessmentSubmissions_ScoreNonNegative",
            table: "AssessmentSubmissions",
            sql: "\"Score\" IS NULL OR \"Score\" >= 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_AssessmentSubmissions_AttemptNumberPositive",
            table: "AssessmentSubmissions",
            sql: "\"AttemptNumber\" > 0");
        migrationBuilder.CreateIndex(
            name: "UX_AssessmentSubmissions_Assessment_Enrollment_Attempt",
            table: "AssessmentSubmissions",
            columns: new[] { "AssessmentId", "EnrollmentId", "AttemptNumber" },
            unique: true);
        migrationBuilder.Sql("""
            CREATE FUNCTION enforce_assessment_submission_score() RETURNS trigger LANGUAGE plpgsql AS $$
            DECLARE assessment_max_score integer;
            BEGIN
                IF NEW."Score" IS NULL THEN RETURN NEW; END IF;
                SELECT "MaxScore" INTO assessment_max_score FROM "Assessments" WHERE "Id" = NEW."AssessmentId" FOR SHARE;
                IF NOT FOUND OR NEW."Score" > assessment_max_score THEN
                    RAISE EXCEPTION 'Assessment submission score exceeds the assessment maximum.' USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END $$;
            CREATE TRIGGER TR_AssessmentSubmissions_EnforceScore
                BEFORE INSERT OR UPDATE OF "AssessmentId", "Score" ON "AssessmentSubmissions"
                FOR EACH ROW EXECUTE FUNCTION enforce_assessment_submission_score();
            CREATE FUNCTION enforce_assessment_max_score() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                IF EXISTS (SELECT 1 FROM "AssessmentSubmissions" WHERE "AssessmentId" = NEW."Id" AND "Score" > NEW."MaxScore") THEN
                    RAISE EXCEPTION 'Assessment maximum cannot be lower than an assigned submission score.' USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END $$;
            CREATE TRIGGER TR_Assessments_EnforceMaximumScore
                BEFORE UPDATE OF "MaxScore" ON "Assessments"
                FOR EACH ROW EXECUTE FUNCTION enforce_assessment_max_score();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER TR_Assessments_EnforceMaximumScore ON \"Assessments\"; DROP FUNCTION enforce_assessment_max_score(); DROP TRIGGER TR_AssessmentSubmissions_EnforceScore ON \"AssessmentSubmissions\"; DROP FUNCTION enforce_assessment_submission_score();");
        migrationBuilder.DropIndex(name: "UX_AssessmentSubmissions_Assessment_Enrollment_Attempt", table: "AssessmentSubmissions");
        migrationBuilder.DropCheckConstraint(name: "CK_AssessmentSubmissions_AttemptNumberPositive", table: "AssessmentSubmissions");
        migrationBuilder.DropCheckConstraint(name: "CK_AssessmentSubmissions_ScoreNonNegative", table: "AssessmentSubmissions");
        migrationBuilder.DropCheckConstraint(name: "CK_Assessments_ScoreRange", table: "Assessments");
    }
}
