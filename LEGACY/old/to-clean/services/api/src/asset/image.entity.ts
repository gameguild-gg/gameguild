import { Column, Entity, Index } from 'typeorm';
import { AssetBase } from './asset.base';
import { ApiProperty } from '@nestjs/swagger';
import { IsOptional, IsString } from 'class-validator';
import { IsIntegerNumber } from '../common/decorators/validator.decorator';

@Entity({ name: 'images' })
@Index('pathUniqueness', ['path', 'filename', 'source'], { unique: true })
export class ImageEntity extends AssetBase {
  @ApiProperty({ type: 'integer' })
  @IsOptional()
  @IsIntegerNumber()
  @Column({ nullable: true, type: 'integer' })
  @Index({ unique: false })
  readonly width: number;

  @ApiProperty({ type: 'integer' })
  @IsOptional()
  @IsIntegerNumber()
  @Column({ nullable: true, type: 'integer' })
  @Index({ unique: false })
  readonly height: number;

  @ApiProperty()
  @IsOptional()
  @IsString()
  @Column({ nullable: true, default: null })
  readonly description: string;
}
