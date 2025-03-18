using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Model;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Mappers;
using Nethereum.Signer;
using Nethereum.Util;

namespace Train.Solver.Core.Blockchain.EVM.Helpers;

public class RawTransactionModel
{
    public string Hash { get; set; } = null!;

    public string SignedTxn { get; set; } = null!;
}

public class EVMTransactionSigner
{
    private readonly Transaction1559Signer _transaction1559Signer;

    public EVMTransactionSigner(Transaction1559Signer transaction1559Signer)
    {
        _transaction1559Signer = transaction1559Signer;
    }

    public EVMTransactionSigner()
    {
        _transaction1559Signer = new Transaction1559Signer();
    }

    public RawTransactionModel SignTransaction(
        Nethereum.Web3.Accounts.Account account,
        TransactionInput transaction)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));

        if (string.IsNullOrWhiteSpace(transaction.From))
            transaction.From = account.Address;

        else if (!transaction.From.IsTheSameAddress(account.Address))
            throw new Exception("Invalid account used for signing, does not match the transaction input");

        var chainId = account.ChainId;

        var nonce = transaction.Nonce;
        if (nonce == null)
            throw new ArgumentNullException(nameof(transaction), "Transaction nonce has not been set");

        var gasLimit = transaction.Gas;
        var value = transaction.Value ?? new HexBigInteger(0);

        if (chainId == null) throw new ArgumentException("ChainId required for TransactionType 0X02 EIP1559");

        if (transaction.Type != null && transaction.Type.Value == TransactionType.EIP1559.AsByte())
        {
            var maxPriorityFeePerGas = transaction.MaxPriorityFeePerGas.Value;
            var maxFeePerGas = transaction.MaxFeePerGas.Value;

            var transaction1559 = new Transaction1559(
                chainId.Value, 
                nonce, 
                maxPriorityFeePerGas, 
                maxFeePerGas,
                gasLimit, 
                transaction.To, 
                value, 
                transaction.Data,
                transaction.AccessList.ToSignerAccessListItemArray());

            _transaction1559Signer.SignTransaction(new EthECKey(account.PrivateKey), transaction1559);

            return new RawTransactionModel
            {
                SignedTxn = transaction1559.GetRLPEncoded().ToHex(),
                Hash = transaction1559.Hash.ToHex(),
            };
        }
        else
        {
            return SignTransaction(
                account.PrivateKey.HexToByteArray(), 
                chainId.Value,
                transaction.To,
                value.Value, 
                nonce,
                transaction.GasPrice.Value, 
                gasLimit.Value, 
                transaction.Data);
        }
    }

    private RawTransactionModel SignTransaction(
        byte[] privateKey,
        BigInteger chainId, 
        string to, 
        BigInteger amount,
        BigInteger nonce, 
        BigInteger gasPrice,
        BigInteger gasLimit, 
        string data)
    {
        var transaction = new LegacyTransactionChainId(to, amount, nonce, gasPrice, gasLimit, data, chainId);
        var signature = new EthECKey(privateKey, true).SignAndCalculateV(transaction.RawHash, transaction.GetChainIdAsBigInteger());
        transaction.SetSignature(new Signature() { R = signature.R, S = signature.S, V = signature.V });

        return new RawTransactionModel
        {
            Hash = transaction.Hash.ToHex(),
            SignedTxn = transaction.GetRLPEncoded().ToHex(),
        };
    }
}
