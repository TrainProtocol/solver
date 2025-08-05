import { Controller, Inject, Post, Body, Param, BadRequestException } from '@nestjs/common';
import { TreasuryService } from './interfaces/treasury.interface';
import { SignRequest, SignResponse } from './treasury.types';
import { GenerateResponse } from './dto/base.dto';
import { TREASURIES } from '../treasury/shared/constants.treasury';
import { SignRequestValidator } from '../common/pipes/sign.pipe';

@Controller('api/treasury')
export class TreasuryController {
  constructor(
    @Inject(TREASURIES)
    private readonly treasuries: TreasuryService[],
  ) {}

  @Post(':network/sign')
  async sign(
    @Param('network') network: string, 
    @Body(SignRequestValidator) request: SignRequest): Promise<SignResponse> {
    const treasury = this.resolveTreasury(network);
    return await treasury.sign(request);
  }

  @Post(':network/generate')
  async generate(@Param('network') network: string): Promise<GenerateResponse> {
    const treasury = this.resolveTreasury(network);
    return await treasury.generate();
  }

  private resolveTreasury(network: string) {
    const treasury = this.treasuries.find(s => s.network.toLowerCase() === network.toLowerCase());
    if (!treasury) throw new BadRequestException('Unsupported network');
    return treasury;
  } 
}