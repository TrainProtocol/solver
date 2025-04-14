export class TransactionFailedException extends Error {
  constructor(message?: string) {
    super(message || 'Transaction failed');
    this.name = 'TransactionFailedException';
    Object.setPrototypeOf(this, TransactionFailedException.prototype);
  }
}
