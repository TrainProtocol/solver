import { Controller, Get } from "@nestjs/common";

@Controller('api/health')
export class HealthController {
  
  @Get()
  async healthcheck(){
    return { status: 'OK'};
  }
} 
