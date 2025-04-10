export class AlreadyClaimedExceptions extends Error {
    constructor(message?: string) {
      super(message || 'HTLC already claimed');
      this.name = 'AlreadyClaimedExceptions';
      Object.setPrototypeOf(this, AlreadyClaimedExceptions.prototype);
    }
  }
  