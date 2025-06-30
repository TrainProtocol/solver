import { HttpException, HttpStatus } from '@nestjs/common';

export class VaultException extends HttpException {
  constructor(message: string, status: HttpStatus = HttpStatus.INTERNAL_SERVER_ERROR) {
    super(message, status);
  }
}