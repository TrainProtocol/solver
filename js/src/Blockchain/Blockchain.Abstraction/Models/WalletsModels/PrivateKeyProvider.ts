import { HookedWalletTypedSignatureSubprovider } from "./HookedWalletEthTxSubprovider";
import RpcSubprovider from 'web3-provider-engine/subproviders/rpc';
import Web3ProviderEngine from 'web3-provider-engine';
import { Wallet } from 'ethers'

function PrivateKeyProvider(privateKey: string, providerUrl: string = null) {
  if (!privateKey) {
    throw new Error(`Private Key missing, non-empty string expected, got "${privateKey}"`);
  }

  this._providers = []

  let wallet = new Wallet(privateKey);
  var walletProvider = new HookedWalletTypedSignatureSubprovider({
    getAccounts: function (cb) {
      cb(null, [wallet.address])
    },
    getPrivateKey: function (address, cb) {
      if (address.toLowerCase() !== wallet.address.toLowerCase()) {
        return cb('Account not found')
      }

      let privateKey = wallet.privateKey;
      if (privateKey.startsWith("0x")) {
        privateKey = privateKey.substring(2);
      }

      cb(null, privateKey)
    }
  });

  this._providers.push(walletProvider);
  if (providerUrl) {
    this._providers.push(new RpcSubprovider({ rpcUrl: providerUrl }));
  }
}

PrivateKeyProvider.prototype._handleAsync = Web3ProviderEngine.prototype._handleAsync;

PrivateKeyProvider.prototype.sendAsync = function (payload, cb) {
  this._handleAsync(payload, cb)
}

export default PrivateKeyProvider;
