import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AssetService } from './asset.service';
import { AssetController } from './asset.controller';
import { ImageEntity } from './image.entity';
import { S3ImageStorage } from './storages/s3.image.storage';
import { CommonModule } from '../common/common.module';
import { FileCacheStorageService } from './storages/filecache.storage';
import { CompressImageService } from './compress.image.service';

@Module({
  imports: [TypeOrmModule.forFeature([ImageEntity]), CommonModule],
  controllers: [AssetController],
  providers: [AssetService, FileCacheStorageService, S3ImageStorage, CompressImageService],
  exports: [AssetService],
})
export class AssetModule {}
