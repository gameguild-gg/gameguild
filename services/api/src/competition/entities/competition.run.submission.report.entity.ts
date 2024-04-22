// how a given submission performed in a given run

import { Column, Entity, ManyToOne, OneToMany } from 'typeorm';
import { ApiProperty } from '@nestjs/swagger';
import { CompetitionRunEntity } from './competition.run.entity';
import { CompetitionSubmissionEntity } from './competition.submission.entity';
import { EntityBase } from '../../common/entities/entity.base';
import { UserEntity } from '../../user/entities';

@Entity()
export class CompetitionRunSubmissionReportEntity extends EntityBase {
  @Column({ type: 'integer', default: 0 })
  @ApiProperty()
  winsAsP1: number;

  @Column({ type: 'integer', default: 0 })
  @ApiProperty()
  winsAsP2: number;

  @Column({ type: 'integer', default: 0 })
  @ApiProperty()
  totalWins: number;

  @Column({ type: 'float8', default: 0 })
  @ApiProperty()
  pointsAsP1: number;

  @Column({ type: 'float8', default: 0 })
  @ApiProperty()
  pointsAsP2: number;

  @Column({ type: 'float8', default: 0 })
  @ApiProperty()
  totalPoints: number;

  @ManyToOne(() => CompetitionRunEntity, (c) => c.reports)
  run: CompetitionRunEntity;

  // link to the submission
  @ManyToOne(() => CompetitionSubmissionEntity, (s) => s.submissionReports)
  submission: CompetitionSubmissionEntity;

  // link to the user
  @ManyToOne(() => UserEntity)
  user: UserEntity;
}
