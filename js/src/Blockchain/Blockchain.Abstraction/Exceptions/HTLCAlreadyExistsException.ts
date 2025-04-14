export class HTLCAlreadyExistsException extends Error {
    constructor(message?: string) {
      super(message || 'Hashlock already exists');
      this.name = 'HTLCAlreadyExistsException';
      Object.setPrototypeOf(this, HTLCAlreadyExistsException.prototype);
    }
  }
  