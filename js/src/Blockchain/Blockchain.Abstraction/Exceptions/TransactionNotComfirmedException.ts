export class TransactionNotComfirmedException extends Error {
    constructor(message?: string) {
        super(message || 'Transaction not confirmed');
        this.name = 'TransactionNotComfirmedException';
        Object.setPrototypeOf(this, TransactionNotComfirmedException.prototype);
    }
}
