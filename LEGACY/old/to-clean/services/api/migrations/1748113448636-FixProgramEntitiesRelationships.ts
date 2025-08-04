import { MigrationInterface, QueryRunner } from 'typeorm';

export class FixProgramEntitiesRelationships1748113448636 implements MigrationInterface {
  name = 'FixProgramEntitiesRelationships1748113448636';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(
      `CREATE TABLE "product_pricing" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "base_price" numeric(10,2) NOT NULL, "creator_share_percentage" numeric(5,2) NOT NULL DEFAULT '70', "tax_rate" numeric(5,4) NOT NULL, "availability_rules" jsonb, "deleted_at" TIMESTAMP, "product_id" uuid, CONSTRAINT "PK_96a4a861354899893dcf7c8d313" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."promo_codes_type_enum" AS ENUM('percentage_off', 'fixed_amount_off', 'buy_one_get_one', 'first_month_free')`,
    );
    await queryRunner.query(
      `CREATE TABLE "promo_codes" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "code" character varying(50) NOT NULL, "type" "public"."promo_codes_type_enum" NOT NULL, "discount_percentage" numeric(5,2), "affiliate_percentage" numeric(5,2), "max_uses" integer, "uses_count" integer NOT NULL DEFAULT '0', "max_uses_per_user" integer NOT NULL DEFAULT '1', "starts_at" TIMESTAMP NOT NULL DEFAULT now(), "expires_at" TIMESTAMP, "is_active" boolean NOT NULL DEFAULT true, "deleted_at" TIMESTAMP, "product_id" uuid, "affiliate_user_id" uuid, "created_by" uuid, CONSTRAINT "UQ_2f096c406a9d9d5b8ce204190c3" UNIQUE ("code"), CONSTRAINT "PK_c7b4f01710fda5afa056a2b4a35" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."financial_transactions_transaction_type_enum" AS ENUM('purchase', 'refund', 'withdrawal', 'deposit', 'transfer', 'fee', 'adjustment')`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."financial_transactions_status_enum" AS ENUM('pending', 'processing', 'completed', 'failed', 'refunded', 'cancelled')`,
    );
    await queryRunner.query(
      `CREATE TABLE "financial_transactions" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "transaction_type" "public"."financial_transactions_transaction_type_enum" NOT NULL, "amount" numeric(10,2) NOT NULL, "original_amount" numeric(10,2) NOT NULL, "referral_commission_amount" numeric(10,2), "status" "public"."financial_transactions_status_enum" NOT NULL, "payment_provider" character varying(255) NOT NULL, "payment_provider_transaction_id" character varying(255) NOT NULL, "metadata" jsonb, "deleted_at" TIMESTAMP, "from_user_id" uuid, "to_user_id" uuid, "product_id" uuid, "pricing_id" uuid, "subscription_plan_id" uuid, "promo_code_id" uuid, "referrer_user_id" uuid, CONSTRAINT "PK_3f0ffe3ca2def8783ad8bb5036b" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_cfae546547cae2739baf60206d" ON "financial_transactions" ("referrer_user_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_31d48836ca680ca1c8e7d5f655" ON "financial_transactions" ("created_at") `);
    await queryRunner.query(`CREATE INDEX "IDX_9a1240d5ca2fcce9854367fac3" ON "financial_transactions" ("payment_provider_transaction_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_5e1d060cfea925ab38f6edadbc" ON "financial_transactions" ("status") `);
    await queryRunner.query(`CREATE INDEX "IDX_0714f08a777d81a139cf7bbf5e" ON "financial_transactions" ("transaction_type") `);
    await queryRunner.query(`CREATE INDEX "IDX_09bfda2980f0e5a353b8a1a6fc" ON "financial_transactions" ("product_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_f0c62234bbde401a4f11a9af3e" ON "financial_transactions" ("to_user_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_70a343d1aa87998fff00092d47" ON "financial_transactions" ("from_user_id") `);
    await queryRunner.query(`CREATE TYPE "public"."user_products_acquisition_type_enum" AS ENUM('purchase', 'subscription', 'free', 'gift')`);
    await queryRunner.query(`CREATE TYPE "public"."user_products_status_enum" AS ENUM('active', 'expired', 'revoked', 'suspended')`);
    await queryRunner.query(
      `CREATE TABLE "user_products" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "acquisition_type" "public"."user_products_acquisition_type_enum" NOT NULL, "access_expires_at" TIMESTAMP, "access_started_at" TIMESTAMP NOT NULL DEFAULT now(), "status" "public"."user_products_status_enum" NOT NULL DEFAULT 'active', "metadata" jsonb NOT NULL, "deleted_at" TIMESTAMP, "user_id" uuid, "product_id" uuid, "subscription_id" uuid, "purchase_transaction_id" uuid, CONSTRAINT "PK_347cc741febfe07d6d46d048fb4" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_8524bda5fc20e6b2e064391198" ON "user_products" ("acquisition_type") `);
    await queryRunner.query(`CREATE INDEX "IDX_2d4f33717491f83635416f67b3" ON "user_products" ("access_expires_at") `);
    await queryRunner.query(`CREATE INDEX "IDX_8b059a1db24054ba759c7de408" ON "user_products" ("status") `);
    await queryRunner.query(`CREATE INDEX "IDX_f87fb160bbd9d4d52bd67624a1" ON "user_products" ("subscription_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_b9470e455b81e2f0bc0d32f269" ON "user_products" ("user_id", "product_id") `);
    await queryRunner.query(
      `CREATE TYPE "public"."program_user_roles_role_enum" AS ENUM('student', 'instructor', 'editor', 'administrator', 'teaching_assistant')`,
    );
    await queryRunner.query(
      `CREATE TABLE "program_user_roles" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "role" "public"."program_user_roles_role_enum" NOT NULL, "deleted_at" TIMESTAMP, "program_id" uuid, "program_user_id" uuid, CONSTRAINT "PK_7b4c06031cac4c8dc3598306c4d" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TABLE "program_users" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "analytics" jsonb NOT NULL DEFAULT '{}', "grades" jsonb NOT NULL DEFAULT '{}', "progress" jsonb NOT NULL DEFAULT '{}', "deleted_at" TIMESTAMP, "user_product_id" uuid, "program_id" uuid, CONSTRAINT "PK_6cd9ef478a28b846fc1bf51bed7" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_1c5d533ae17f9d4db2d0185b80" ON "program_users" ("user_product_id") WHERE deleted_at IS NULL`);
    await queryRunner.query(`CREATE INDEX "IDX_8386bbf0d442848d7be2993fbb" ON "program_users" ("program_id") WHERE deleted_at IS NULL`);
    await queryRunner.query(
      `CREATE UNIQUE INDEX "IDX_7c4e86ab4c02a65ee72992cb57" ON "program_users" ("user_product_id", "program_id") WHERE deleted_at IS NULL`,
    );
    await queryRunner.query(
      `CREATE TABLE "activity_grades" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "grade" numeric(5,2), "graded_at" TIMESTAMP, "feedback" text, "rubric_assessment" jsonb, "metadata" jsonb, "content_interaction_id" uuid, "grader_program_user_id" uuid, CONSTRAINT "PK_fa586cf477e73abb88e12e87f4a" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_5f550ee6239b8f4b0931ecc938" ON "activity_grades" ("content_interaction_id") `);
    await queryRunner.query(`CREATE TYPE "public"."content_interactions_status_enum" AS ENUM('not_started', 'in_progress', 'completed', 'skipped')`);
    await queryRunner.query(
      `CREATE TABLE "content_interactions" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "status" "public"."content_interactions_status_enum" NOT NULL DEFAULT 'not_started', "started_at" TIMESTAMP, "completed_at" TIMESTAMP, "time_spent_seconds" integer NOT NULL DEFAULT '0', "last_accessed_at" TIMESTAMP, "submitted_at" TIMESTAMP, "answers" jsonb, "text_response" text, "url_response" character varying, "file_response" jsonb, "metadata" jsonb, "deleted_at" TIMESTAMP, "program_user_id" uuid, "content_id" uuid, CONSTRAINT "PK_4eb661a3ed146d6cc809100e7dd" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_eccd0e268fd973ba96803d0e6f" ON "content_interactions" ("completed_at") `);
    await queryRunner.query(`CREATE INDEX "IDX_6b25f0f08b688b0e17ac97793a" ON "content_interactions" ("submitted_at") `);
    await queryRunner.query(`CREATE INDEX "IDX_4ee189ef0ebab2ab5645ddb880" ON "content_interactions" ("content_id", "status") `);
    await queryRunner.query(`CREATE INDEX "IDX_39e13bc2b7fb5360fe2ecc30de" ON "content_interactions" ("program_user_id", "status") `);
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_a86ba3342df6bca2e02ff903ad" ON "content_interactions" ("program_user_id", "content_id") `);
    await queryRunner.query(
      `CREATE TYPE "public"."program_contents_type_enum" AS ENUM('page', 'assignment', 'questionnaire', 'discussion', 'code', 'challenge', 'reflection', 'survey')`,
    );
    await queryRunner.query(`CREATE TYPE "public"."program_contents_grading_method_enum" AS ENUM('instructor', 'peer', 'ai', 'automated_tests')`);
    await queryRunner.query(
      `CREATE TABLE "program_contents" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "type" "public"."program_contents_type_enum" NOT NULL, "title" character varying(255) NOT NULL, "summary" text, "order" double precision NOT NULL, "body" jsonb NOT NULL, "previewable" boolean NOT NULL DEFAULT false, "due_date" TIMESTAMP, "available_from" TIMESTAMP, "available_to" TIMESTAMP, "grading_method" "public"."program_contents_grading_method_enum", "duration_minutes" integer, "text_response" boolean NOT NULL DEFAULT false, "url_response" boolean NOT NULL DEFAULT false, "file_response_extensions" jsonb, "grading_rubric" jsonb, "metadata" jsonb, "deleted_at" TIMESTAMP, "program_id" uuid, "parent_id" uuid, CONSTRAINT "PK_8652fc3c6cbfc6aef46914e0bd9" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_4ae43a5f02434f93cf29cb749d" ON "program_contents" ("parent_id", "previewable") `);
    await queryRunner.query(`CREATE INDEX "IDX_9f6833d8894e69409e75271965" ON "program_contents" ("available_from", "available_to") `);
    await queryRunner.query(`CREATE INDEX "IDX_e1730f758bb452e2a32d6be228" ON "program_contents" ("due_date") `);
    await queryRunner.query(`CREATE INDEX "IDX_745ed5daf12f8291044fbc4450" ON "program_contents" ("previewable") `);
    await queryRunner.query(`CREATE INDEX "IDX_c18934543aa5eebcb3d4a34a66" ON "program_contents" ("type") `);
    await queryRunner.query(`CREATE INDEX "IDX_8eb4657a44b37f7bb0423c6784" ON "program_contents" ("parent_id", "order") `);
    await queryRunner.query(`CREATE INDEX "IDX_788346b7a51507192230730db7" ON "program_contents" ("parent_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_940a83edd0d1a685b73ac8bb39" ON "program_contents" ("program_id", "order") `);
    await queryRunner.query(`CREATE INDEX "IDX_2a00830247b0e93cb6c9d6a168" ON "program_contents" ("program_id") `);
    await queryRunner.query(
      `CREATE TABLE "programs" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "slug" character varying(255) NOT NULL, "summary" text NOT NULL, "body" jsonb NOT NULL, "tenancy_domains" jsonb, "cached_enrollment_count" integer NOT NULL DEFAULT '0', "cached_completion_count" integer NOT NULL DEFAULT '0', "cached_students_completion_rate" numeric(5,2) NOT NULL DEFAULT '0', "cached_rating" numeric(3,2) NOT NULL DEFAULT '0', "metadata" jsonb, "deleted_at" TIMESTAMP, CONSTRAINT "UQ_4180c2bfa0402878a63b70cb4a4" UNIQUE ("slug"), CONSTRAINT "PK_d43c664bcaafc0e8a06dfd34e05" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_3381fedfde637cded46e8b0a77" ON "programs" ("cached_rating") `);
    await queryRunner.query(`CREATE INDEX "IDX_e9fc8a6502df1912d4eaae8132" ON "programs" ("cached_students_completion_rate") `);
    await queryRunner.query(`CREATE INDEX "IDX_6782795eacefb9ec300673f6f2" ON "programs" ("cached_enrollment_count") `);
    await queryRunner.query(`CREATE INDEX "IDX_e39bc1bb7b6fdddc3ccc0c6d4b" ON "programs" ("tenancy_domains") `);
    await queryRunner.query(`CREATE INDEX "IDX_cd28bc7f50ea4bb46771d0ec9b" ON "programs" ("created_at") `);
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_4180c2bfa0402878a63b70cb4a" ON "programs" ("slug") `);
    await queryRunner.query(
      `CREATE TABLE "product_programs" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "order_index" double precision NOT NULL, "is_primary" boolean NOT NULL DEFAULT false, "deleted_at" TIMESTAMP, "product_id" uuid, "program_id" uuid, CONSTRAINT "PK_4f4bb57866a8128ded92fb01f90" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."products_type_enum" AS ENUM('program', 'learning_pathway', 'bundle', 'subscription', 'workshop', 'mentorship', 'ebook', 'resource_pack', 'community', 'certification', 'other')`,
    );
    await queryRunner.query(`CREATE TYPE "public"."products_visibility_enum" AS ENUM('draft', 'published', 'archived')`);
    await queryRunner.query(
      `CREATE TABLE "products" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "title" character varying(255) NOT NULL, "description" text NOT NULL, "thumbnail" character varying, "type" "public"."products_type_enum" NOT NULL DEFAULT 'program', "is_bundle" boolean NOT NULL DEFAULT false, "bundle_items" jsonb, "metadata" jsonb NOT NULL, "referral_commission_percentage" numeric(5,2) NOT NULL DEFAULT '30', "max_affiliate_discount" numeric(5,2) NOT NULL DEFAULT '0', "affiliate_commission_percentage" numeric(5,2) NOT NULL DEFAULT '30', "visibility" "public"."products_visibility_enum" NOT NULL DEFAULT 'draft', "deleted_at" TIMESTAMP, CONSTRAINT "PK_0806c755e0aca124e67c0cf6d7d" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_995d8194c43edfc98838cabc5a" ON "products" ("created_at") `);
    await queryRunner.query(`CREATE INDEX "IDX_ce2ba38200f73815993d6e96de" ON "products" ("visibility") `);
    await queryRunner.query(`CREATE INDEX "IDX_5b0f3fe151f941e51d4491cfa8" ON "products" ("is_bundle") `);
    await queryRunner.query(`CREATE INDEX "IDX_d5662d5ea5da62fc54b0f12a46" ON "products" ("type") `);
    await queryRunner.query(`CREATE TYPE "public"."product_subscription_plans_type_enum" AS ENUM('monthly', 'quarterly', 'annual', 'lifetime')`);
    await queryRunner.query(`CREATE TYPE "public"."product_subscription_plans_billing_interval_enum" AS ENUM('day', 'week', 'month', 'year')`);
    await queryRunner.query(
      `CREATE TABLE "product_subscription_plans" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "name" character varying(255) NOT NULL, "description" text, "type" "public"."product_subscription_plans_type_enum" NOT NULL, "price" numeric(10,2) NOT NULL, "base_price" numeric(10,2) NOT NULL, "billing_interval" "public"."product_subscription_plans_billing_interval_enum" NOT NULL, "billing_interval_count" integer NOT NULL DEFAULT '1', "trial_period_days" integer, "features" jsonb, "availability_rules" jsonb, "deleted_at" TIMESTAMP, "product_id" uuid, CONSTRAINT "PK_bf93ad4daf677b4fec9a61633ad" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."user_subscriptions_status_enum" AS ENUM('active', 'trialing', 'past_due', 'canceled', 'incomplete', 'incomplete_expired', 'unpaid')`,
    );
    await queryRunner.query(
      `CREATE TABLE "user_subscriptions" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "status" "public"."user_subscriptions_status_enum" NOT NULL, "current_period_start" TIMESTAMP NOT NULL, "current_period_end" TIMESTAMP NOT NULL, "cancel_at_period_end" boolean NOT NULL DEFAULT false, "canceled_at" TIMESTAMP, "ended_at" TIMESTAMP, "trial_end" TIMESTAMP, "deleted_at" TIMESTAMP, "user_id" uuid, "subscription_plan_id" uuid, CONSTRAINT "PK_9e928b0954e51705ab44988812c" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."user_financial_methods_method_type_enum" AS ENUM('credit_card', 'debit_card', 'crypto_wallet', 'wallet_balance', 'bank_transfer')`,
    );
    await queryRunner.query(`CREATE TYPE "public"."user_financial_methods_status_enum" AS ENUM('active', 'inactive', 'expired', 'removed')`);
    await queryRunner.query(
      `CREATE TABLE "user_financial_methods" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "user_id" uuid NOT NULL, "method_type" "public"."user_financial_methods_method_type_enum" NOT NULL, "provider_method_id" character varying(255), "last_four_digits" character varying(100), "brand" character varying(100), "expiry_month" integer, "expiry_year" integer, "is_active" boolean NOT NULL DEFAULT true, "is_default" boolean NOT NULL DEFAULT false, "status" "public"."user_financial_methods_status_enum" NOT NULL DEFAULT 'active', "metadata" jsonb, CONSTRAINT "PK_b0118602348fc1d679602903966" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_bcc4dfbc00db2f49919b3399fd" ON "user_financial_methods" ("user_id", "status") `);
    await queryRunner.query(
      `CREATE TABLE "program_feedback_submissions" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "feedback_responses" jsonb NOT NULL, "feedback_template_version" character varying NOT NULL, "submitted_at" TIMESTAMP NOT NULL DEFAULT now(), "ip_address" character varying, "user_agent" text, "is_valid" boolean NOT NULL DEFAULT true, "user_id" uuid, "program_id" uuid, "product_id" uuid, "program_user_id" uuid, CONSTRAINT "PK_a27f818de2c259092282649f215" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE TYPE "public"."user_kyc_verifications_provider_enum" AS ENUM('sumsub', 'shufti', 'onfido', 'jumio', 'custom')`);
    await queryRunner.query(
      `CREATE TYPE "public"."user_kyc_verifications_status_enum" AS ENUM('pending', 'in_progress', 'approved', 'rejected', 'suspended', 'expired')`,
    );
    await queryRunner.query(
      `CREATE TABLE "user_kyc_verifications" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "provider" "public"."user_kyc_verifications_provider_enum" NOT NULL, "provider_applicant_id" character varying NOT NULL, "provider_verification_id" character varying NOT NULL, "verification_level" character varying NOT NULL, "status" "public"."user_kyc_verifications_status_enum" NOT NULL DEFAULT 'pending', "last_check_at" TIMESTAMP NOT NULL, "approved_at" TIMESTAMP, "rejected_at" TIMESTAMP, "rejection_reason" text, "expires_at" TIMESTAMP, "metadata" jsonb, "user_id" uuid, CONSTRAINT "UQ_3cbbc217c93d23bf19132eb0fcc" UNIQUE ("provider_applicant_id"), CONSTRAINT "UQ_d8ffff8cfd13fe59f7eb8f17f0e" UNIQUE ("provider_verification_id"), CONSTRAINT "PK_b2713fe3f62fa4a2731c72dac33" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE TYPE "public"."tag_relationships_type_enum" AS ENUM('related', 'parent', 'child', 'requires', 'suggested')`);
    await queryRunner.query(
      `CREATE TABLE "tag_relationships" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "type" "public"."tag_relationships_type_enum" NOT NULL, "metadata" jsonb, "deleted_at" TIMESTAMP, "source_id" uuid, "target_id" uuid, CONSTRAINT "PK_006f1c1f0cbca49effaf09799e9" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."tags_type_enum" AS ENUM('skill', 'topic', 'technology', 'difficulty', 'category', 'industry', 'certification')`,
    );
    await queryRunner.query(
      `CREATE TABLE "tags" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "name" character varying(255) NOT NULL, "description" text, "type" "public"."tags_type_enum" NOT NULL, "category" character varying(100), "metadata" jsonb, "deleted_at" TIMESTAMP, CONSTRAINT "PK_e7dc17249a1148a1970748eda99" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_e66a4ffa4213d40bef0da5bc44" ON "tags" ("category") `);
    await queryRunner.query(`CREATE INDEX "IDX_bcffa2d68eb6e69d436f69b3f1" ON "tags" ("type") `);
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_1f0d687740481a26473720e159" ON "tags" ("name", "type") `);
    await queryRunner.query(
      `CREATE TYPE "public"."tag_proficiencies_proficiency_level_enum" AS ENUM('awareness', 'novice', 'beginner', 'intermediate', 'advanced', 'expert', 'master')`,
    );
    await queryRunner.query(
      `CREATE TABLE "tag_proficiencies" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "proficiency_level" "public"."tag_proficiencies_proficiency_level_enum" NOT NULL, "proficiency_level_value" double precision NOT NULL, "description" text, "prerequisites" jsonb, "metadata" jsonb, "deleted_at" TIMESTAMP, "tag_id" uuid, CONSTRAINT "PK_4e846dacf04db68651a21aa3ecd" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_b8d1fe1a0510ae5012bd7b1898" ON "tag_proficiencies" ("deleted_at") `);
    await queryRunner.query(`CREATE INDEX "IDX_744364c4c8c528f26673dd64a6" ON "tag_proficiencies" ("proficiency_level_value") `);
    await queryRunner.query(`CREATE INDEX "IDX_13a27f06ef2e3f70d1bd88f1dd" ON "tag_proficiencies" ("proficiency_level") `);
    await queryRunner.query(`CREATE INDEX "IDX_345d7b97c4909b5ea559c914ef" ON "tag_proficiencies" ("tag_id") `);
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_77d3fb222924a32da12afe5ab0" ON "tag_proficiencies" ("tag_id", "proficiency_level_value") `);
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_8bae11b40cccfa5f56fcc31892" ON "tag_proficiencies" ("tag_id", "proficiency_level") `);
    await queryRunner.query(`CREATE TYPE "public"."certificate_tags_type_enum" AS ENUM('requirement', 'recommendation', 'provides')`);
    await queryRunner.query(
      `CREATE TABLE "certificate_tags" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "type" "public"."certificate_tags_type_enum" NOT NULL, "metadata" jsonb, "certificate_id" uuid, "tag_id" uuid, CONSTRAINT "PK_5d34b1014c933c6dc8da5764c08" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."certificates_certificate_type_enum" AS ENUM('program_completion', 'product_bundle_completion', 'learning_pathway', 'skill_mastery', 'event_participation', 'assessment_passed', 'project_completion', 'specialization', 'professional', 'achievement', 'instructor', 'time_investment', 'peer_recognition')`,
    );
    await queryRunner.query(`CREATE TYPE "public"."certificates_certificate_verification_method_enum" AS ENUM('code', 'blockchain', 'both')`);
    await queryRunner.query(
      `CREATE TABLE "certificates" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "certificate_type" "public"."certificates_certificate_type_enum" NOT NULL DEFAULT 'program_completion', "name" character varying(255) NOT NULL, "description" text NOT NULL, "html_template" text NOT NULL, "css_styles" text NOT NULL, "auto_issue" boolean NOT NULL DEFAULT true, "minimum_grade" double precision NOT NULL DEFAULT '70', "completion_percentage" integer NOT NULL DEFAULT '100', "requires_feedback" boolean NOT NULL DEFAULT false, "requires_rating" boolean NOT NULL DEFAULT false, "minimum_rating" double precision, "feedback_form_template" jsonb, "expiration_months" integer, "certificate_verification_method" "public"."certificates_certificate_verification_method_enum" NOT NULL DEFAULT 'code', "prerequisites" jsonb, "badge_image" character varying, "signature_image" character varying, "credential_title" character varying, "issuer_name" character varying, "metadata" jsonb, "is_active" boolean NOT NULL DEFAULT true, "deleted_at" TIMESTAMP, "program_id" uuid, "product_id" uuid, CONSTRAINT "PK_e4c7e31e2144300bea7d89eb165" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_d5cdecac88236f3c3a3e1a2205" ON "certificates" ("is_active") `);
    await queryRunner.query(`CREATE INDEX "IDX_82c9eb5da92f6eb4b1a0f285fe" ON "certificates" ("certificate_type") `);
    await queryRunner.query(`CREATE INDEX "IDX_9e25120646f90fd0815171dba0" ON "certificates" ("product_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_1eb0d8d2840d24d211ac1e29b7" ON "certificates" ("program_id") `);
    await queryRunner.query(`CREATE TYPE "public"."user_certificates_status_enum" AS ENUM('active', 'expired', 'revoked', 'pending')`);
    await queryRunner.query(
      `CREATE TABLE "user_certificates" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "certificate_number" character varying(100) NOT NULL, "status" "public"."user_certificates_status_enum" NOT NULL, "issued_at" TIMESTAMP NOT NULL, "expires_at" TIMESTAMP, "revoked_at" TIMESTAMP, "revocation_reason" text, "certificate_url" character varying, "metadata" jsonb, "deleted_at" TIMESTAMP, "program_id" uuid, "product_id" uuid, "user_id" uuid, "program_user_id" uuid, "certificate_id" uuid, CONSTRAINT "UQ_62166957dad7b82241c3888c2e0" UNIQUE ("certificate_number"), CONSTRAINT "PK_58e6b21aa02f666a20886d895ac" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_a243781516ea785025af146b6e" ON "user_certificates" ("expires_at") `);
    await queryRunner.query(`CREATE INDEX "IDX_8e6d34aa76b22cd2cb5dbfed53" ON "user_certificates" ("issued_at") `);
    await queryRunner.query(`CREATE INDEX "IDX_f91b60ef6aeb3549d79a3081e6" ON "user_certificates" ("status") `);
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_62166957dad7b82241c3888c2e" ON "user_certificates" ("certificate_number") `);
    await queryRunner.query(`CREATE INDEX "IDX_5d5e924c347ffb90821fce8534" ON "user_certificates" ("certificate_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_f8ae29f5803a17157f033ff96a" ON "user_certificates" ("program_user_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_0fb66c2b2d4edbb223682730e8" ON "user_certificates" ("user_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_1eb0cddc4690c9153e3bf5cf0b" ON "user_certificates" ("product_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_122c8480175f9e5c6166768a13" ON "user_certificates" ("program_id") `);
    await queryRunner.query(
      `CREATE TABLE "promo_code_uses" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "discount_applied" numeric(10,2) NOT NULL, "used_at" TIMESTAMP NOT NULL DEFAULT now(), "promo_code_id" uuid NOT NULL, "user_id" uuid NOT NULL, "financial_transaction_id" uuid NOT NULL, CONSTRAINT "PK_d5e6f1cb226ee85a66aa1b14b2c" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_3510b9afbd630080495fbcbd80" ON "promo_code_uses" ("used_at") `);
    await queryRunner.query(`CREATE INDEX "IDX_845967e02e20790222d217b13f" ON "promo_code_uses" ("financial_transaction_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_84e9d82f9d6e3c9e53f5d19988" ON "promo_code_uses" ("user_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_d82ddb9839c0092afc178357ef" ON "promo_code_uses" ("promo_code_id") `);
    await queryRunner.query(`CREATE TYPE "public"."program_ratings_moderation_status_enum" AS ENUM('pending', 'approved', 'rejected', 'flagged')`);
    await queryRunner.query(
      `CREATE TABLE "program_ratings" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "rating" integer NOT NULL, "review_title" character varying, "review_text" text, "content_quality_rating" double precision, "instructor_rating" double precision, "difficulty_rating" double precision, "value_rating" double precision, "submitted_at" TIMESTAMP NOT NULL DEFAULT now(), "ip_address" character varying, "user_agent" text, "is_public" boolean NOT NULL DEFAULT true, "is_verified" boolean NOT NULL DEFAULT false, "moderation_status" "public"."program_ratings_moderation_status_enum" NOT NULL DEFAULT 'pending', "moderation_notes" text, "moderated_at" TIMESTAMP, "user_id" uuid NOT NULL, "program_id" uuid, "product_id" uuid, "program_user_id" uuid, "moderated_by_id" uuid, CONSTRAINT "PK_d653dbffa3cd8b66a337276efda" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_1d9dec735da6a8fa878376fa34" ON "program_ratings" ("moderation_status", "submitted_at") `);
    await queryRunner.query(`CREATE INDEX "IDX_271f6fab838387902f291e38b9" ON "program_ratings" ("is_public", "moderation_status") `);
    await queryRunner.query(`CREATE INDEX "IDX_87b9687f34eecf7f845fad5a9b" ON "program_ratings" ("rating", "submitted_at") `);
    await queryRunner.query(`CREATE INDEX "IDX_e381f739722db5b73ee0dd0c86" ON "program_ratings" ("product_id", "rating") `);
    await queryRunner.query(`CREATE INDEX "IDX_ba6419d58689aec1b8b61fa01e" ON "program_ratings" ("program_id", "rating") `);
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_f4e8bd0c60d52a3e63e5985824" ON "program_ratings" ("user_id", "product_id") WHERE product_id IS NOT NULL`);
    await queryRunner.query(`CREATE UNIQUE INDEX "IDX_8a096e1dd761ef5b88c85b6763" ON "program_ratings" ("user_id", "program_id") WHERE program_id IS NOT NULL`);
    await queryRunner.query(
      `CREATE TABLE "certificate_blockchain_anchors" ("id" uuid NOT NULL DEFAULT uuid_generate_v4(), "created_at" TIMESTAMP NOT NULL DEFAULT now(), "updated_at" TIMESTAMP NOT NULL DEFAULT now(), "blockchain_network" character varying NOT NULL, "transaction_hash" character varying NOT NULL, "block_number" bigint NOT NULL, "smart_contract_address" character varying, "token_id" character varying, "anchored_at" TIMESTAMP NOT NULL, "public_verification_url" character varying, "ipfs_hash" character varying, "metadata" jsonb, "deleted_at" TIMESTAMP, "certificate_id" uuid NOT NULL, CONSTRAINT "PK_64e844852c45801b2a3529e049f" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(`CREATE INDEX "IDX_283d4b4842f0e3a7953c5b0f1b" ON "certificate_blockchain_anchors" ("token_id") `);
    await queryRunner.query(`CREATE INDEX "IDX_0033f7bb83698a13a8914b9c66" ON "certificate_blockchain_anchors" ("blockchain_network") `);
    await queryRunner.query(`CREATE INDEX "IDX_37fc52ce06b6bc0a18d605186a" ON "certificate_blockchain_anchors" ("transaction_hash") `);
    await queryRunner.query(`CREATE INDEX "IDX_3291395ce689c51d103f8b6e09" ON "certificate_blockchain_anchors" ("certificate_id") `);
    await queryRunner.query(
      `ALTER TABLE "product_pricing" ADD CONSTRAINT "FK_bf55ac56eaa6394e8dfca101d2c" FOREIGN KEY ("product_id") REFERENCES "products"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "promo_codes" ADD CONSTRAINT "FK_2d2ebccb233751a53febc336065" FOREIGN KEY ("product_id") REFERENCES "products"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "promo_codes" ADD CONSTRAINT "FK_1f9ab6a0dcf9e824384fd539d99" FOREIGN KEY ("affiliate_user_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "promo_codes" ADD CONSTRAINT "FK_b92a143baf32a667bb94c3ba38d" FOREIGN KEY ("created_by") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "financial_transactions" ADD CONSTRAINT "FK_70a343d1aa87998fff00092d472" FOREIGN KEY ("from_user_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "financial_transactions" ADD CONSTRAINT "FK_f0c62234bbde401a4f11a9af3e1" FOREIGN KEY ("to_user_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "financial_transactions" ADD CONSTRAINT "FK_09bfda2980f0e5a353b8a1a6fc6" FOREIGN KEY ("product_id") REFERENCES "products"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "financial_transactions" ADD CONSTRAINT "FK_429bec4cb1c63911ef39b4bd6da" FOREIGN KEY ("pricing_id") REFERENCES "product_pricing"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "financial_transactions" ADD CONSTRAINT "FK_7095a5495c4a48cb95a30d522b4" FOREIGN KEY ("subscription_plan_id") REFERENCES "product_subscription_plans"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "financial_transactions" ADD CONSTRAINT "FK_d0aeb7a50b21d7e71f218d8b627" FOREIGN KEY ("promo_code_id") REFERENCES "promo_codes"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "financial_transactions" ADD CONSTRAINT "FK_cfae546547cae2739baf60206dc" FOREIGN KEY ("referrer_user_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_products" ADD CONSTRAINT "FK_494f0246efbe65076d1051c6539" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_products" ADD CONSTRAINT "FK_1c5a5dc69b4ac2b5ee475684779" FOREIGN KEY ("product_id") REFERENCES "products"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_products" ADD CONSTRAINT "FK_f87fb160bbd9d4d52bd67624a11" FOREIGN KEY ("subscription_id") REFERENCES "user_subscriptions"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_products" ADD CONSTRAINT "FK_79b7aee8397a9e6ec5257799bba" FOREIGN KEY ("purchase_transaction_id") REFERENCES "financial_transactions"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_user_roles" ADD CONSTRAINT "FK_a903f0fc1935805fea5adc8d21e" FOREIGN KEY ("program_id") REFERENCES "programs"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_user_roles" ADD CONSTRAINT "FK_9a3d3e5fcf1d8b3d8e6574f6589" FOREIGN KEY ("program_user_id") REFERENCES "program_users"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_users" ADD CONSTRAINT "FK_3f8269ece5c96c3cbbc315d82cd" FOREIGN KEY ("user_product_id") REFERENCES "user_products"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_users" ADD CONSTRAINT "FK_371d13d3b21508c397bea86270d" FOREIGN KEY ("program_id") REFERENCES "programs"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "activity_grades" ADD CONSTRAINT "FK_5f550ee6239b8f4b0931ecc9388" FOREIGN KEY ("content_interaction_id") REFERENCES "content_interactions"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "activity_grades" ADD CONSTRAINT "FK_96272d95bb0bb9a8eb19e85a4b6" FOREIGN KEY ("grader_program_user_id") REFERENCES "program_users"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "content_interactions" ADD CONSTRAINT "FK_fe4f7763585463950f995ac722a" FOREIGN KEY ("program_user_id") REFERENCES "program_users"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "content_interactions" ADD CONSTRAINT "FK_3785dd8784af01122265d7e0e69" FOREIGN KEY ("content_id") REFERENCES "program_contents"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_contents" ADD CONSTRAINT "FK_2a00830247b0e93cb6c9d6a1684" FOREIGN KEY ("program_id") REFERENCES "programs"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_contents" ADD CONSTRAINT "FK_788346b7a51507192230730db7d" FOREIGN KEY ("parent_id") REFERENCES "program_contents"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "product_programs" ADD CONSTRAINT "FK_85bae7ba4e07a22184cf986b8cf" FOREIGN KEY ("product_id") REFERENCES "products"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "product_programs" ADD CONSTRAINT "FK_9a46b918935c93c037d5660c05e" FOREIGN KEY ("program_id") REFERENCES "programs"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "product_subscription_plans" ADD CONSTRAINT "FK_b26d5f84c1f600fc683ef84bb35" FOREIGN KEY ("product_id") REFERENCES "products"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_subscriptions" ADD CONSTRAINT "FK_0641da02314913e28f6131310eb" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_subscriptions" ADD CONSTRAINT "FK_b6e02561ba40a3798a7e1432f2e" FOREIGN KEY ("subscription_plan_id") REFERENCES "product_subscription_plans"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_financial_methods" ADD CONSTRAINT "FK_0917f56cef87d5a529e4b925723" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_feedback_submissions" ADD CONSTRAINT "FK_ac5e3ad0aaa6502bc0ef74ddb28" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_feedback_submissions" ADD CONSTRAINT "FK_49115775d5897ffb7bb287a1e6c" FOREIGN KEY ("program_id") REFERENCES "programs"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_feedback_submissions" ADD CONSTRAINT "FK_50a9835764a366420c6be67f65f" FOREIGN KEY ("product_id") REFERENCES "products"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_feedback_submissions" ADD CONSTRAINT "FK_9cf9818c6645c0b5d7d9ae11957" FOREIGN KEY ("program_user_id") REFERENCES "program_users"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_kyc_verifications" ADD CONSTRAINT "FK_eeff64a6066bb1ca6fc7b1ea4cb" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "tag_relationships" ADD CONSTRAINT "FK_5c00ef066a17f4cde33e03b2638" FOREIGN KEY ("source_id") REFERENCES "tags"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "tag_relationships" ADD CONSTRAINT "FK_839d5643d2b30cf68c3971558ef" FOREIGN KEY ("target_id") REFERENCES "tags"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "tag_proficiencies" ADD CONSTRAINT "FK_345d7b97c4909b5ea559c914ef9" FOREIGN KEY ("tag_id") REFERENCES "tags"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "certificate_tags" ADD CONSTRAINT "FK_46ba38d909db356de7d2f5ae497" FOREIGN KEY ("certificate_id") REFERENCES "certificates"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "certificate_tags" ADD CONSTRAINT "FK_18742b080354846bb198a7f5e5b" FOREIGN KEY ("tag_id") REFERENCES "tag_proficiencies"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "certificates" ADD CONSTRAINT "FK_1eb0d8d2840d24d211ac1e29b79" FOREIGN KEY ("program_id") REFERENCES "programs"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "certificates" ADD CONSTRAINT "FK_9e25120646f90fd0815171dba08" FOREIGN KEY ("product_id") REFERENCES "products"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_certificates" ADD CONSTRAINT "FK_122c8480175f9e5c6166768a134" FOREIGN KEY ("program_id") REFERENCES "programs"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_certificates" ADD CONSTRAINT "FK_1eb0cddc4690c9153e3bf5cf0bb" FOREIGN KEY ("product_id") REFERENCES "products"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_certificates" ADD CONSTRAINT "FK_0fb66c2b2d4edbb223682730e86" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_certificates" ADD CONSTRAINT "FK_f8ae29f5803a17157f033ff96ad" FOREIGN KEY ("program_user_id") REFERENCES "program_users"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "user_certificates" ADD CONSTRAINT "FK_5d5e924c347ffb90821fce8534c" FOREIGN KEY ("certificate_id") REFERENCES "certificates"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "promo_code_uses" ADD CONSTRAINT "FK_d82ddb9839c0092afc178357ef7" FOREIGN KEY ("promo_code_id") REFERENCES "promo_codes"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "promo_code_uses" ADD CONSTRAINT "FK_84e9d82f9d6e3c9e53f5d199887" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "promo_code_uses" ADD CONSTRAINT "FK_845967e02e20790222d217b13fe" FOREIGN KEY ("financial_transaction_id") REFERENCES "financial_transactions"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_ratings" ADD CONSTRAINT "FK_bbd044e8a6b67d25fef18488aa3" FOREIGN KEY ("user_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_ratings" ADD CONSTRAINT "FK_4c556a1f46688c7ffca855ff35a" FOREIGN KEY ("program_id") REFERENCES "programs"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_ratings" ADD CONSTRAINT "FK_390c0f8c8082d01c407e13a35ca" FOREIGN KEY ("product_id") REFERENCES "products"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_ratings" ADD CONSTRAINT "FK_5677bbc4c5ffbca69b9f3fc6b65" FOREIGN KEY ("program_user_id") REFERENCES "program_users"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "program_ratings" ADD CONSTRAINT "FK_b75d9735a481fa024f490c109e1" FOREIGN KEY ("moderated_by_id") REFERENCES "user"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "certificate_blockchain_anchors" ADD CONSTRAINT "FK_3291395ce689c51d103f8b6e093" FOREIGN KEY ("certificate_id") REFERENCES "user_certificates"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`ALTER TABLE "certificate_blockchain_anchors" DROP CONSTRAINT "FK_3291395ce689c51d103f8b6e093"`);
    await queryRunner.query(`ALTER TABLE "program_ratings" DROP CONSTRAINT "FK_b75d9735a481fa024f490c109e1"`);
    await queryRunner.query(`ALTER TABLE "program_ratings" DROP CONSTRAINT "FK_5677bbc4c5ffbca69b9f3fc6b65"`);
    await queryRunner.query(`ALTER TABLE "program_ratings" DROP CONSTRAINT "FK_390c0f8c8082d01c407e13a35ca"`);
    await queryRunner.query(`ALTER TABLE "program_ratings" DROP CONSTRAINT "FK_4c556a1f46688c7ffca855ff35a"`);
    await queryRunner.query(`ALTER TABLE "program_ratings" DROP CONSTRAINT "FK_bbd044e8a6b67d25fef18488aa3"`);
    await queryRunner.query(`ALTER TABLE "promo_code_uses" DROP CONSTRAINT "FK_845967e02e20790222d217b13fe"`);
    await queryRunner.query(`ALTER TABLE "promo_code_uses" DROP CONSTRAINT "FK_84e9d82f9d6e3c9e53f5d199887"`);
    await queryRunner.query(`ALTER TABLE "promo_code_uses" DROP CONSTRAINT "FK_d82ddb9839c0092afc178357ef7"`);
    await queryRunner.query(`ALTER TABLE "user_certificates" DROP CONSTRAINT "FK_5d5e924c347ffb90821fce8534c"`);
    await queryRunner.query(`ALTER TABLE "user_certificates" DROP CONSTRAINT "FK_f8ae29f5803a17157f033ff96ad"`);
    await queryRunner.query(`ALTER TABLE "user_certificates" DROP CONSTRAINT "FK_0fb66c2b2d4edbb223682730e86"`);
    await queryRunner.query(`ALTER TABLE "user_certificates" DROP CONSTRAINT "FK_1eb0cddc4690c9153e3bf5cf0bb"`);
    await queryRunner.query(`ALTER TABLE "user_certificates" DROP CONSTRAINT "FK_122c8480175f9e5c6166768a134"`);
    await queryRunner.query(`ALTER TABLE "certificates" DROP CONSTRAINT "FK_9e25120646f90fd0815171dba08"`);
    await queryRunner.query(`ALTER TABLE "certificates" DROP CONSTRAINT "FK_1eb0d8d2840d24d211ac1e29b79"`);
    await queryRunner.query(`ALTER TABLE "certificate_tags" DROP CONSTRAINT "FK_18742b080354846bb198a7f5e5b"`);
    await queryRunner.query(`ALTER TABLE "certificate_tags" DROP CONSTRAINT "FK_46ba38d909db356de7d2f5ae497"`);
    await queryRunner.query(`ALTER TABLE "tag_proficiencies" DROP CONSTRAINT "FK_345d7b97c4909b5ea559c914ef9"`);
    await queryRunner.query(`ALTER TABLE "tag_relationships" DROP CONSTRAINT "FK_839d5643d2b30cf68c3971558ef"`);
    await queryRunner.query(`ALTER TABLE "tag_relationships" DROP CONSTRAINT "FK_5c00ef066a17f4cde33e03b2638"`);
    await queryRunner.query(`ALTER TABLE "user_kyc_verifications" DROP CONSTRAINT "FK_eeff64a6066bb1ca6fc7b1ea4cb"`);
    await queryRunner.query(`ALTER TABLE "program_feedback_submissions" DROP CONSTRAINT "FK_9cf9818c6645c0b5d7d9ae11957"`);
    await queryRunner.query(`ALTER TABLE "program_feedback_submissions" DROP CONSTRAINT "FK_50a9835764a366420c6be67f65f"`);
    await queryRunner.query(`ALTER TABLE "program_feedback_submissions" DROP CONSTRAINT "FK_49115775d5897ffb7bb287a1e6c"`);
    await queryRunner.query(`ALTER TABLE "program_feedback_submissions" DROP CONSTRAINT "FK_ac5e3ad0aaa6502bc0ef74ddb28"`);
    await queryRunner.query(`ALTER TABLE "user_financial_methods" DROP CONSTRAINT "FK_0917f56cef87d5a529e4b925723"`);
    await queryRunner.query(`ALTER TABLE "user_subscriptions" DROP CONSTRAINT "FK_b6e02561ba40a3798a7e1432f2e"`);
    await queryRunner.query(`ALTER TABLE "user_subscriptions" DROP CONSTRAINT "FK_0641da02314913e28f6131310eb"`);
    await queryRunner.query(`ALTER TABLE "product_subscription_plans" DROP CONSTRAINT "FK_b26d5f84c1f600fc683ef84bb35"`);
    await queryRunner.query(`ALTER TABLE "product_programs" DROP CONSTRAINT "FK_9a46b918935c93c037d5660c05e"`);
    await queryRunner.query(`ALTER TABLE "product_programs" DROP CONSTRAINT "FK_85bae7ba4e07a22184cf986b8cf"`);
    await queryRunner.query(`ALTER TABLE "program_contents" DROP CONSTRAINT "FK_788346b7a51507192230730db7d"`);
    await queryRunner.query(`ALTER TABLE "program_contents" DROP CONSTRAINT "FK_2a00830247b0e93cb6c9d6a1684"`);
    await queryRunner.query(`ALTER TABLE "content_interactions" DROP CONSTRAINT "FK_3785dd8784af01122265d7e0e69"`);
    await queryRunner.query(`ALTER TABLE "content_interactions" DROP CONSTRAINT "FK_fe4f7763585463950f995ac722a"`);
    await queryRunner.query(`ALTER TABLE "activity_grades" DROP CONSTRAINT "FK_96272d95bb0bb9a8eb19e85a4b6"`);
    await queryRunner.query(`ALTER TABLE "activity_grades" DROP CONSTRAINT "FK_5f550ee6239b8f4b0931ecc9388"`);
    await queryRunner.query(`ALTER TABLE "program_users" DROP CONSTRAINT "FK_371d13d3b21508c397bea86270d"`);
    await queryRunner.query(`ALTER TABLE "program_users" DROP CONSTRAINT "FK_3f8269ece5c96c3cbbc315d82cd"`);
    await queryRunner.query(`ALTER TABLE "program_user_roles" DROP CONSTRAINT "FK_9a3d3e5fcf1d8b3d8e6574f6589"`);
    await queryRunner.query(`ALTER TABLE "program_user_roles" DROP CONSTRAINT "FK_a903f0fc1935805fea5adc8d21e"`);
    await queryRunner.query(`ALTER TABLE "user_products" DROP CONSTRAINT "FK_79b7aee8397a9e6ec5257799bba"`);
    await queryRunner.query(`ALTER TABLE "user_products" DROP CONSTRAINT "FK_f87fb160bbd9d4d52bd67624a11"`);
    await queryRunner.query(`ALTER TABLE "user_products" DROP CONSTRAINT "FK_1c5a5dc69b4ac2b5ee475684779"`);
    await queryRunner.query(`ALTER TABLE "user_products" DROP CONSTRAINT "FK_494f0246efbe65076d1051c6539"`);
    await queryRunner.query(`ALTER TABLE "financial_transactions" DROP CONSTRAINT "FK_cfae546547cae2739baf60206dc"`);
    await queryRunner.query(`ALTER TABLE "financial_transactions" DROP CONSTRAINT "FK_d0aeb7a50b21d7e71f218d8b627"`);
    await queryRunner.query(`ALTER TABLE "financial_transactions" DROP CONSTRAINT "FK_7095a5495c4a48cb95a30d522b4"`);
    await queryRunner.query(`ALTER TABLE "financial_transactions" DROP CONSTRAINT "FK_429bec4cb1c63911ef39b4bd6da"`);
    await queryRunner.query(`ALTER TABLE "financial_transactions" DROP CONSTRAINT "FK_09bfda2980f0e5a353b8a1a6fc6"`);
    await queryRunner.query(`ALTER TABLE "financial_transactions" DROP CONSTRAINT "FK_f0c62234bbde401a4f11a9af3e1"`);
    await queryRunner.query(`ALTER TABLE "financial_transactions" DROP CONSTRAINT "FK_70a343d1aa87998fff00092d472"`);
    await queryRunner.query(`ALTER TABLE "promo_codes" DROP CONSTRAINT "FK_b92a143baf32a667bb94c3ba38d"`);
    await queryRunner.query(`ALTER TABLE "promo_codes" DROP CONSTRAINT "FK_1f9ab6a0dcf9e824384fd539d99"`);
    await queryRunner.query(`ALTER TABLE "promo_codes" DROP CONSTRAINT "FK_2d2ebccb233751a53febc336065"`);
    await queryRunner.query(`ALTER TABLE "product_pricing" DROP CONSTRAINT "FK_bf55ac56eaa6394e8dfca101d2c"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_3291395ce689c51d103f8b6e09"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_37fc52ce06b6bc0a18d605186a"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_0033f7bb83698a13a8914b9c66"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_283d4b4842f0e3a7953c5b0f1b"`);
    await queryRunner.query(`DROP TABLE "certificate_blockchain_anchors"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_8a096e1dd761ef5b88c85b6763"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_f4e8bd0c60d52a3e63e5985824"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_ba6419d58689aec1b8b61fa01e"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_e381f739722db5b73ee0dd0c86"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_87b9687f34eecf7f845fad5a9b"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_271f6fab838387902f291e38b9"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_1d9dec735da6a8fa878376fa34"`);
    await queryRunner.query(`DROP TABLE "program_ratings"`);
    await queryRunner.query(`DROP TYPE "public"."program_ratings_moderation_status_enum"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_d82ddb9839c0092afc178357ef"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_84e9d82f9d6e3c9e53f5d19988"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_845967e02e20790222d217b13f"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_3510b9afbd630080495fbcbd80"`);
    await queryRunner.query(`DROP TABLE "promo_code_uses"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_122c8480175f9e5c6166768a13"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_1eb0cddc4690c9153e3bf5cf0b"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_0fb66c2b2d4edbb223682730e8"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_f8ae29f5803a17157f033ff96a"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_5d5e924c347ffb90821fce8534"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_62166957dad7b82241c3888c2e"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_f91b60ef6aeb3549d79a3081e6"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_8e6d34aa76b22cd2cb5dbfed53"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_a243781516ea785025af146b6e"`);
    await queryRunner.query(`DROP TABLE "user_certificates"`);
    await queryRunner.query(`DROP TYPE "public"."user_certificates_status_enum"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_1eb0d8d2840d24d211ac1e29b7"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_9e25120646f90fd0815171dba0"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_82c9eb5da92f6eb4b1a0f285fe"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_d5cdecac88236f3c3a3e1a2205"`);
    await queryRunner.query(`DROP TABLE "certificates"`);
    await queryRunner.query(`DROP TYPE "public"."certificates_certificate_verification_method_enum"`);
    await queryRunner.query(`DROP TYPE "public"."certificates_certificate_type_enum"`);
    await queryRunner.query(`DROP TABLE "certificate_tags"`);
    await queryRunner.query(`DROP TYPE "public"."certificate_tags_type_enum"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_8bae11b40cccfa5f56fcc31892"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_77d3fb222924a32da12afe5ab0"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_345d7b97c4909b5ea559c914ef"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_13a27f06ef2e3f70d1bd88f1dd"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_744364c4c8c528f26673dd64a6"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_b8d1fe1a0510ae5012bd7b1898"`);
    await queryRunner.query(`DROP TABLE "tag_proficiencies"`);
    await queryRunner.query(`DROP TYPE "public"."tag_proficiencies_proficiency_level_enum"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_1f0d687740481a26473720e159"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_bcffa2d68eb6e69d436f69b3f1"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_e66a4ffa4213d40bef0da5bc44"`);
    await queryRunner.query(`DROP TABLE "tags"`);
    await queryRunner.query(`DROP TYPE "public"."tags_type_enum"`);
    await queryRunner.query(`DROP TABLE "tag_relationships"`);
    await queryRunner.query(`DROP TYPE "public"."tag_relationships_type_enum"`);
    await queryRunner.query(`DROP TABLE "user_kyc_verifications"`);
    await queryRunner.query(`DROP TYPE "public"."user_kyc_verifications_status_enum"`);
    await queryRunner.query(`DROP TYPE "public"."user_kyc_verifications_provider_enum"`);
    await queryRunner.query(`DROP TABLE "program_feedback_submissions"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_bcc4dfbc00db2f49919b3399fd"`);
    await queryRunner.query(`DROP TABLE "user_financial_methods"`);
    await queryRunner.query(`DROP TYPE "public"."user_financial_methods_status_enum"`);
    await queryRunner.query(`DROP TYPE "public"."user_financial_methods_method_type_enum"`);
    await queryRunner.query(`DROP TABLE "user_subscriptions"`);
    await queryRunner.query(`DROP TYPE "public"."user_subscriptions_status_enum"`);
    await queryRunner.query(`DROP TABLE "product_subscription_plans"`);
    await queryRunner.query(`DROP TYPE "public"."product_subscription_plans_billing_interval_enum"`);
    await queryRunner.query(`DROP TYPE "public"."product_subscription_plans_type_enum"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_d5662d5ea5da62fc54b0f12a46"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_5b0f3fe151f941e51d4491cfa8"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_ce2ba38200f73815993d6e96de"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_995d8194c43edfc98838cabc5a"`);
    await queryRunner.query(`DROP TABLE "products"`);
    await queryRunner.query(`DROP TYPE "public"."products_visibility_enum"`);
    await queryRunner.query(`DROP TYPE "public"."products_type_enum"`);
    await queryRunner.query(`DROP TABLE "product_programs"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_4180c2bfa0402878a63b70cb4a"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_cd28bc7f50ea4bb46771d0ec9b"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_e39bc1bb7b6fdddc3ccc0c6d4b"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_6782795eacefb9ec300673f6f2"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_e9fc8a6502df1912d4eaae8132"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_3381fedfde637cded46e8b0a77"`);
    await queryRunner.query(`DROP TABLE "programs"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_2a00830247b0e93cb6c9d6a168"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_940a83edd0d1a685b73ac8bb39"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_788346b7a51507192230730db7"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_8eb4657a44b37f7bb0423c6784"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_c18934543aa5eebcb3d4a34a66"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_745ed5daf12f8291044fbc4450"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_e1730f758bb452e2a32d6be228"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_9f6833d8894e69409e75271965"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_4ae43a5f02434f93cf29cb749d"`);
    await queryRunner.query(`DROP TABLE "program_contents"`);
    await queryRunner.query(`DROP TYPE "public"."program_contents_grading_method_enum"`);
    await queryRunner.query(`DROP TYPE "public"."program_contents_type_enum"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_a86ba3342df6bca2e02ff903ad"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_39e13bc2b7fb5360fe2ecc30de"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_4ee189ef0ebab2ab5645ddb880"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_6b25f0f08b688b0e17ac97793a"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_eccd0e268fd973ba96803d0e6f"`);
    await queryRunner.query(`DROP TABLE "content_interactions"`);
    await queryRunner.query(`DROP TYPE "public"."content_interactions_status_enum"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_5f550ee6239b8f4b0931ecc938"`);
    await queryRunner.query(`DROP TABLE "activity_grades"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_7c4e86ab4c02a65ee72992cb57"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_8386bbf0d442848d7be2993fbb"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_1c5d533ae17f9d4db2d0185b80"`);
    await queryRunner.query(`DROP TABLE "program_users"`);
    await queryRunner.query(`DROP TABLE "program_user_roles"`);
    await queryRunner.query(`DROP TYPE "public"."program_user_roles_role_enum"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_b9470e455b81e2f0bc0d32f269"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_f87fb160bbd9d4d52bd67624a1"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_8b059a1db24054ba759c7de408"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_2d4f33717491f83635416f67b3"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_8524bda5fc20e6b2e064391198"`);
    await queryRunner.query(`DROP TABLE "user_products"`);
    await queryRunner.query(`DROP TYPE "public"."user_products_status_enum"`);
    await queryRunner.query(`DROP TYPE "public"."user_products_acquisition_type_enum"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_70a343d1aa87998fff00092d47"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_f0c62234bbde401a4f11a9af3e"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_09bfda2980f0e5a353b8a1a6fc"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_0714f08a777d81a139cf7bbf5e"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_5e1d060cfea925ab38f6edadbc"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_9a1240d5ca2fcce9854367fac3"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_31d48836ca680ca1c8e7d5f655"`);
    await queryRunner.query(`DROP INDEX "public"."IDX_cfae546547cae2739baf60206d"`);
    await queryRunner.query(`DROP TABLE "financial_transactions"`);
    await queryRunner.query(`DROP TYPE "public"."financial_transactions_status_enum"`);
    await queryRunner.query(`DROP TYPE "public"."financial_transactions_transaction_type_enum"`);
    await queryRunner.query(`DROP TABLE "promo_codes"`);
    await queryRunner.query(`DROP TYPE "public"."promo_codes_type_enum"`);
    await queryRunner.query(`DROP TABLE "product_pricing"`);
  }
}
