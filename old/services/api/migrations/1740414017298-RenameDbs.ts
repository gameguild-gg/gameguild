import { MigrationInterface, QueryRunner } from 'typeorm';

export class RenameDbs1740414017298 implements MigrationInterface {
  name = 'RenameDbs1740414017298';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`ALTER TABLE "job_post" DROP CONSTRAINT "FK_af3a1de2186046eb06e9aeb5809"`);
    await queryRunner.query(`ALTER TABLE "job_post" DROP CONSTRAINT "FK_33aeb92a17d4888a8f62336bb8e"`);
    await queryRunner.query(`ALTER TABLE "job_application" DROP CONSTRAINT "FK_d88c856eb48cbc63bde511c6cad"`);
    await queryRunner.query(`ALTER TABLE "job_application" DROP CONSTRAINT "FK_ca676357efe051c6135232d3ffc"`);
    await queryRunner.query(`ALTER TABLE "job_post_editors_user" DROP CONSTRAINT "FK_612fe7371eb62167d48add018f4"`);
    await queryRunner.query(`ALTER TABLE "job_post_editors_user" DROP CONSTRAINT "FK_31aeb5812812256abf78ba261b5"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP CONSTRAINT "FK_044f1ccbefb86ed608a0a17c3bd"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP CONSTRAINT "FK_251949f6aae5f190956d20ed635"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_12d56147187c085b6bea313618"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_23668b4e7f7b6908f461bc5522"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_86431017ad9aaf29066c5f697c"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_a84bb70f034d1ba5605cd3fbc5"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_4914621fb42025de12e8ec2225"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_244c4f211882ced89a917ec4f3"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_31aeb5812812256abf78ba261b"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_612fe7371eb62167d48add018f"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_251949f6aae5f190956d20ed63"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_044f1ccbefb86ed608a0a17c3b"`);
    await queryRunner.query(`ALTER TABLE "job_post_editors_user" RENAME COLUMN "job-post_id" TO "job_post_id"`);
    await queryRunner.query(`CREATE TYPE "public"."quiz_visibility_enum" AS ENUM('DRAFT', 'PUBLISHED', 'FUTURE', 'PENDING', 'PRIVATE', 'TRASH')`);
    await queryRunner.query(
      `CREATE TABLE "quiz" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "slug" character varying(255) NOT NULL, "title" character varying(255) NOT NULL, "summary" character varying(1024), "body" text, "visibility" "public"."quiz_visibility_enum" NOT NULL DEFAULT 'DRAFT', "questions" jsonb, "grading_instructions" character varying, "owner_id" uuid, "thumbnail_id" uuid, CONSTRAINT "PK_422d974e7217414e029b3e641d0" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_80e0595aa5572aca26e681ee1e" ON "quiz" ("slug") `);
    await queryRunner.query(`CREATE INDEX "IDX_91b3636bd5cc303c7409c55088" ON "quiz" ("title") `);
    await queryRunner.query(`CREATE INDEX "IDX_37105dd2b728dfbba2f313c907" ON "quiz" ("visibility") `);
    await queryRunner.query(
      `CREATE TABLE "quiz_editors_user" ("quiz_id" uuid NOT NULL, "user_id" uuid NOT NULL, CONSTRAINT "PK_fbb93aa1e4225ccb62d11a796c0" PRIMARY KEY ("quiz_id", "user_id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_536f6eac3215f5823c76623df1" ON "quiz_editors_user" ("quiz_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_f09c6797b5611ed29a94d7df8b" ON "quiz_editors_user" ("user_id") `);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP CONSTRAINT "PK_7a49c7ad19ebb3e814e2918012f"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" ADD CONSTRAINT "PK_7a49c7ad19ebb3e814e2918012f" PRIMARY KEY ("job-tag_id")`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP COLUMN "job-post_id"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP CONSTRAINT "PK_7a49c7ad19ebb3e814e2918012f"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP COLUMN "job-tag_id"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" ADD "job_post_id" uuid NOT NULL`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" ADD CONSTRAINT "PK_bce26f11d48dfe6a441bb8d8a07" PRIMARY KEY ("job_post_id")`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" ADD "job_tag_id" uuid NOT NULL`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP CONSTRAINT "PK_bce26f11d48dfe6a441bb8d8a07"`);
    await queryRunner.query(
      `ALTER TABLE "job_post_job_tags_job_tag" ADD CONSTRAINT "PK_0a636facbab11355e4f4bbbf3f3" PRIMARY KEY ("job_post_id", "job_tag_id")`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_0c4e9330020ee70d765dff0486" ON "job_tag" ("name") `);
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_902620afcf0f13d981154aac83" ON "job_post" ("slug") `);
    await queryRunner.query(`CREATE INDEX "IDX_ee26d130dc420c2e35f42573ce" ON "job_post" ("title") `);
    await queryRunner.query(`CREATE INDEX "IDX_03b7a2e0ae0f87dae10d1e9f00" ON "job_post" ("visibility") `);
    await queryRunner.query(`CREATE INDEX "IDX_ec0b3c542afe53891e30476936" ON "job_post" ("location") `);
    await queryRunner.query(`CREATE INDEX "IDX_f8c69a1e815225cdd8a30de66b" ON "job_post" ("job_type") `);
    await queryRunner.query(`CREATE INDEX "IDX_d41a469fa70f6e342f137a61d0" ON "job_post_editors_user" ("job_post_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_332380fd9f01b6e74b2509f5f6" ON "job_post_editors_user" ("user_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_bce26f11d48dfe6a441bb8d8a0" ON "job_post_job_tags_job_tag" ("job_post_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_22911465a689416e80716524bd" ON "job_post_job_tags_job_tag" ("job_tag_id") `);
    await queryRunner.query(
      `ALTER TABLE "job_post" ADD CONSTRAINT "FK_25bb3b2508647e45ae8859341c2" FOREIGN KEY ("owner_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_post" ADD CONSTRAINT "FK_f072b11c11702359d65a552affb" FOREIGN KEY ("thumbnail_id") REFERENCES "images"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_application" ADD CONSTRAINT "FK_7ee93afec8bb94bc62d0e6014cc" FOREIGN KEY ("applicant_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_application" ADD CONSTRAINT "FK_a7f70771aaf242d17ef281570cf" FOREIGN KEY ("job_id") REFERENCES "job_post"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "quiz" ADD CONSTRAINT "FK_be83e259b70f9b1d2eb25bd049c" FOREIGN KEY ("owner_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "quiz" ADD CONSTRAINT "FK_736041642ce62379cad4a41b6c8" FOREIGN KEY ("thumbnail_id") REFERENCES "images"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_post_editors_user" ADD CONSTRAINT "FK_d41a469fa70f6e342f137a61d00" FOREIGN KEY ("job_post_id") REFERENCES "job_post"("id") ON DELETE CASCADE ON UPDATE CASCADE`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_post_editors_user" ADD CONSTRAINT "FK_332380fd9f01b6e74b2509f5f6f" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE ON UPDATE CASCADE`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_post_job_tags_job_tag" ADD CONSTRAINT "FK_bce26f11d48dfe6a441bb8d8a07" FOREIGN KEY ("job_post_id") REFERENCES "job_post"("id") ON DELETE CASCADE ON UPDATE CASCADE`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_post_job_tags_job_tag" ADD CONSTRAINT "FK_22911465a689416e80716524bd1" FOREIGN KEY ("job_tag_id") REFERENCES "job_tag"("id") ON DELETE CASCADE ON UPDATE CASCADE`,
    );
    await queryRunner.query(
      `ALTER TABLE "quiz_editors_user" ADD CONSTRAINT "FK_536f6eac3215f5823c76623df1a" FOREIGN KEY ("quiz_id") REFERENCES "quiz"("id") ON DELETE CASCADE ON UPDATE CASCADE`,
    );
    await queryRunner.query(
      `ALTER TABLE "quiz_editors_user" ADD CONSTRAINT "FK_f09c6797b5611ed29a94d7df8b0" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE ON UPDATE CASCADE`,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`ALTER TABLE "quiz_editors_user" DROP CONSTRAINT "FK_f09c6797b5611ed29a94d7df8b0"`);
    await queryRunner.query(`ALTER TABLE "quiz_editors_user" DROP CONSTRAINT "FK_536f6eac3215f5823c76623df1a"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP CONSTRAINT "FK_22911465a689416e80716524bd1"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP CONSTRAINT "FK_bce26f11d48dfe6a441bb8d8a07"`);
    await queryRunner.query(`ALTER TABLE "job_post_editors_user" DROP CONSTRAINT "FK_332380fd9f01b6e74b2509f5f6f"`);
    await queryRunner.query(`ALTER TABLE "job_post_editors_user" DROP CONSTRAINT "FK_d41a469fa70f6e342f137a61d00"`);
    await queryRunner.query(`ALTER TABLE "quiz" DROP CONSTRAINT "FK_736041642ce62379cad4a41b6c8"`);
    await queryRunner.query(`ALTER TABLE "quiz" DROP CONSTRAINT "FK_be83e259b70f9b1d2eb25bd049c"`);
    await queryRunner.query(`ALTER TABLE "job_application" DROP CONSTRAINT "FK_a7f70771aaf242d17ef281570cf"`);
    await queryRunner.query(`ALTER TABLE "job_application" DROP CONSTRAINT "FK_7ee93afec8bb94bc62d0e6014cc"`);
    await queryRunner.query(`ALTER TABLE "job_post" DROP CONSTRAINT "FK_f072b11c11702359d65a552affb"`);
    await queryRunner.query(`ALTER TABLE "job_post" DROP CONSTRAINT "FK_25bb3b2508647e45ae8859341c2"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_22911465a689416e80716524bd"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_bce26f11d48dfe6a441bb8d8a0"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_332380fd9f01b6e74b2509f5f6"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_d41a469fa70f6e342f137a61d0"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_f8c69a1e815225cdd8a30de66b"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_ec0b3c542afe53891e30476936"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_03b7a2e0ae0f87dae10d1e9f00"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_ee26d130dc420c2e35f42573ce"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_902620afcf0f13d981154aac83"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_0c4e9330020ee70d765dff0486"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP CONSTRAINT "PK_0a636facbab11355e4f4bbbf3f3"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" ADD CONSTRAINT "PK_bce26f11d48dfe6a441bb8d8a07" PRIMARY KEY ("job_post_id")`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP COLUMN "job_tag_id"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP CONSTRAINT "PK_bce26f11d48dfe6a441bb8d8a07"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP COLUMN "job_post_id"`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" ADD "job-tag_id" uuid NOT NULL`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" ADD CONSTRAINT "PK_7a49c7ad19ebb3e814e2918012f" PRIMARY KEY ("job-tag_id")`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" ADD "job-post_id" uuid NOT NULL`);
    await queryRunner.query(`ALTER TABLE "job_post_job_tags_job_tag" DROP CONSTRAINT "PK_7a49c7ad19ebb3e814e2918012f"`);
    await queryRunner.query(
      `ALTER TABLE "job_post_job_tags_job_tag" ADD CONSTRAINT "PK_7a49c7ad19ebb3e814e2918012f" PRIMARY KEY ("job-post_id", "job-tag_id")`,
    );
    await queryRunner.query(`DROP INDEX "public"."IDX_f09c6797b5611ed29a94d7df8b"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_536f6eac3215f5823c76623df1"`);
    await queryRunner.query(`DROP TABLE "quiz_editors_user"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_37105dd2b728dfbba2f313c907"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_91b3636bd5cc303c7409c55088"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_80e0595aa5572aca26e681ee1e"`);
    await queryRunner.query(`DROP TABLE "quiz"`);
    await queryRunner.query(`DROP TYPE "public"."quiz_visibility_enum"`);
    await queryRunner.query(`ALTER TABLE "job_post_editors_user" RENAME COLUMN "job_post_id" TO "job-post_id"`);
    await queryRunner.query(`CREATE INDEX "IDX_044f1ccbefb86ed608a0a17c3b" ON "job_post_job_tags_job_tag" ("job-tag_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_251949f6aae5f190956d20ed63" ON "job_post_job_tags_job_tag" ("job-post_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_612fe7371eb62167d48add018f" ON "job_post_editors_user" ("user_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_31aeb5812812256abf78ba261b" ON "job_post_editors_user" ("job-post_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_244c4f211882ced89a917ec4f3" ON "job_post" ("title") `);
    await queryRunner.query(`CREATE INDEX "IDX_4914621fb42025de12e8ec2225" ON "job_post" ("job_type") `);
    await queryRunner.query(`CREATE INDEX "IDX_a84bb70f034d1ba5605cd3fbc5" ON "job_post" ("location") `);
    await queryRunner.query(`CREATE INDEX "IDX_86431017ad9aaf29066c5f697c" ON "job_post" ("visibility") `);
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_23668b4e7f7b6908f461bc5522" ON "job_post" ("slug") `);
    await queryRunner.query(`CREATE INDEX "IDX_12d56147187c085b6bea313618" ON "job_tag" ("name") `);
    await queryRunner.query(
      `ALTER TABLE "job_post_job_tags_job_tag" ADD CONSTRAINT "FK_251949f6aae5f190956d20ed635" FOREIGN KEY ("job-post_id") REFERENCES "job_post"("id") ON DELETE CASCADE ON UPDATE CASCADE`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_post_job_tags_job_tag" ADD CONSTRAINT "FK_044f1ccbefb86ed608a0a17c3bd" FOREIGN KEY ("job-tag_id") REFERENCES "job_tag"("id") ON DELETE CASCADE ON UPDATE CASCADE`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_post_editors_user" ADD CONSTRAINT "FK_31aeb5812812256abf78ba261b5" FOREIGN KEY ("job-post_id") REFERENCES "job_post"("id") ON DELETE CASCADE ON UPDATE CASCADE`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_post_editors_user" ADD CONSTRAINT "FK_612fe7371eb62167d48add018f4" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE ON UPDATE CASCADE`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_application" ADD CONSTRAINT "FK_ca676357efe051c6135232d3ffc" FOREIGN KEY ("applicant_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_application" ADD CONSTRAINT "FK_d88c856eb48cbc63bde511c6cad" FOREIGN KEY ("job_id") REFERENCES "job_post"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_post" ADD CONSTRAINT "FK_33aeb92a17d4888a8f62336bb8e" FOREIGN KEY ("owner_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "job_post" ADD CONSTRAINT "FK_af3a1de2186046eb06e9aeb5809" FOREIGN KEY ("thumbnail_id") REFERENCES "images"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
  }
}
