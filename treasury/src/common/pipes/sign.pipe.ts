import { Injectable, PipeTransform, BadRequestException } from "@nestjs/common";
import { SignRequest,  } from "../../app/treasury.types";
import { plainToInstance } from 'class-transformer';
import { validate } from 'class-validator';
import { BaseSignRequest } from "../../app/dto/base.dto";

@Injectable()
export class SignRequestValidator implements PipeTransform {
  async transform(value: SignRequest): Promise<SignRequest> {
    const baseValidation = plainToInstance(BaseSignRequest, value);
    
    const errors = await validate(baseValidation);
    console.log('Validation errors:', errors);

     if (errors.length > 0) {
      const errorMessages = errors.flatMap(error => 
            Object.values(error.constraints ?? {}));

      throw new BadRequestException(errorMessages.join(', '));
    }

    return value;
  }
}