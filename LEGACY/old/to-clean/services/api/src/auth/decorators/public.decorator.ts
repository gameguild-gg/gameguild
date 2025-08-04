import type { CustomDecorator } from '@nestjs/common';
import { SetMetadata } from '@nestjs/common';

export const PUBLIC_ROUTE_KEY = 'public_route';

export const PublicRoute = (isPublic = false): CustomDecorator => SetMetadata(PUBLIC_ROUTE_KEY, isPublic);
