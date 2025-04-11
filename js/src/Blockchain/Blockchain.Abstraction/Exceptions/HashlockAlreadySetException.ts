export class HashlockAlreadySetException extends Error {
    constructor(message?: string) {
      super(message || 'Hashlock already set');
      this.name = 'HashlockAlreadySetException';
      Object.setPrototypeOf(this, HashlockAlreadySetException.prototype);
    }
  }
  