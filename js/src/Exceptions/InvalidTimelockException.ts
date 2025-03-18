export class InvalidTimelockException extends Error {
    constructor(message?: string) {
      super(message || 'Invalid TimeLock encountered');
      this.name = 'InvalidTimelockException';
      Object.setPrototypeOf(this, InvalidTimelockException.prototype);
    }
  }
  