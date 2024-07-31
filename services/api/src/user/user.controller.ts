import { Controller } from '@nestjs/common';
import { UserService } from './user.service';
import { Auth } from '../auth/decorators/http.decorator';
import { UserEntity } from './entities';
import { AuthUser } from '../auth/decorators/auth-user.decorator';
import { ApiTags } from '@nestjs/swagger';

@Controller('users')
@ApiTags('users')
export class UserController {
  constructor(private readonly userService: UserService) {}
}
