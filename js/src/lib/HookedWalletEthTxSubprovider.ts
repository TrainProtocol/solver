// From https://github.com/trufflesuite/truffle/blob/v5.7.2/packages/hdwallet-provider/src/index.ts

const HookedWalletProvider = require('web3-provider-engine/subproviders/hooked-wallet');
import { signTypedData, SignTypedDataVersion, personalSign } from "@metamask/eth-sig-util";

export class HookedWalletTypedSignatureSubprovider extends HookedWalletProvider {

  constructor(private _opts: any) {
    super(_opts)
  }

  // https://github.com/trufflesuite/truffle/blob/v5.7.2/packages/hdwallet-provider/src/index.ts#L228
  signTypedMessage({ data, from }: { data: string; from: string }, cb: any) {
    this._opts.getPrivateKey(from, function (err: any, privateKey: any) {
      if (err) return cb(err)

      if (!data) {
        cb("No data to sign");
        return;
      }

      const signature = signTypedData({
        data: JSON.parse(data),
        privateKey: privateKey,
        version: SignTypedDataVersion.V4
      });

      cb(null, signature);
    })
  }

  signMessage({ data, from }: any, cb: any) {
    this._opts.getPrivateKey(from, function (err: any, privateKey: any) {
      if (err) return cb(err)

      if (!data) {
        cb("No data to sign");
        return;
      }

      const signature = personalSign({ privateKey, data })

      cb(null, signature);
    })
  }

  signPersonalMessage(payload: any, cb: any) {
    this.signMessage(payload, cb);
  };
}
