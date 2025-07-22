import { BigNumber, utils } from "ethers";
import { RpcProvider } from "starknet";
import 'reflect-metadata';
import { TrackBlockEventsAsync } from "../../Blockchain.Starknet/Activities/Helper/StarknetEventTracker";
import { Address, AssetId, B256Address, BN, bn, Contract, DateTime, formatUnits, Provider, ScriptTransactionRequest, transactionRequestify, Wallet } from "fuels";
import abi from '../Activities/ABIs/ERC20.json';
//import { generateUint256Hex } from "../Activities/Helper/FuelTransactionBuilder";

describe("Test for klir", () => {
    it('returns correct puc valid input', async () => {

        const createEmptyArray = (length: number, char: string) =>
            Array.from({ length }, () => ''.padEnd(64, char));

        const lpAddress = '0xecc1ecfac722c678d44cd3f82cf3cf59dab2c3f6c9feecec4404e54bebac1cc4'
        const lpPrivKey = '0x70b41a122dd7c94c7ad330f189b35d23f2966cb96048aa04a9e52e81ef71b5c1'
        const destinationChain = 'ETHEREUM_SEPOLIA'
        const destinationAsset = 'ETH'
        const address = '0x0000000000000000000000000000000000000000'
        const sourceAsset = 'ETH'
        const receiverAddressik = '0x0B1956a6737cb62fF5E66479F7770315fb5055BB75888b6C2Be43155F6dF1704'

        const amount = 0.0001


        const hopChains = createEmptyArray(5, ' ')
        const hopAssets = createEmptyArray(5, ' ')
        const hopAddresses = createEmptyArray(5, ' ')

        const provider = new Provider("https://testnet.fuel.network/v1/graphql");

        const wallet = Wallet.fromPrivateKey(lpPrivKey, provider);

        const commitId = generateUint256Hex().toString();

        const LOCK_TIME = 1000 * 60 * 20 // 20 minutes
        const timeLockMS = Math.floor((Date.now() + LOCK_TIME) / 1000)
        const timelock = DateTime.fromUnixSeconds(timeLockMS).toTai64();

        const dstChain = destinationChain.padEnd(64, ' ');
        const dstAsset = destinationAsset.padEnd(64, ' ');
        const dstAddress = address.padEnd(64, ' ');
        const srcAsset = sourceAsset.padEnd(64, ' ');
        const srcReceiver = { bits: lpAddress };
        const b256: B256Address = "0xf8f8b6283d7fa5b672b530cbb84fcccb4ff8dc40f8176ef4544ddb1f1952ad07";
        const chaddress: Address = Address.fromB256(b256);
        const assetId: AssetId = chaddress.toAssetId();

        const contractInstance = new Contract("0x18bf15b00de68f6cc8504fbc0204b5b3de9faebce3e488ebe3efc7d7e82fee27", abi, provider);
        const receiverAddress = { bits: receiverAddressik };

        const callConfig = contractInstance.functions
            .commit(
                hopChains,
                hopChains,
                hopChains,
                dstChain,
                dstAsset,
                dstAddress,
                srcAsset,
                commitId,
                receiverAddress,
                timelock)
            .callParams({
                forward: [1, assetId.bits]
            })
            .txParams(
                {
                    gasLimit: 100_00000,
                    maxFee: bn(100_000)
                });

        const txRequest = await callConfig.getTransactionRequest();

        const klir = await wallet.provider.estimateTxDependencies(txRequest);

        txRequest.gasLimit = bn(klir.dryRunStatus.totalGas);
        txRequest.maxFee = bn(klir.dryRunStatus.totalFee);
        const callData = JSON.stringify(txRequest);


        try {
            const txReq = transactionRequestify(JSON.parse(callData));

            const { coins } = await wallet.getCoins(assetId.bits);
            for (const coin of coins) {
                txReq.addCoinInput(coin);
            }


            console.log('maxFee:', txReq.maxFee?.toString()); // Should be > 0

            txReq.addAccountWitnesses(wallet);
            const transactionId = await wallet.sendTransaction(txReq);
            const result = transactionId.id;


            expect(result).toBeDefined();
        }
        catch (error) {
            console.error("Error sending transaction:", error);
            throw error;
        }
    });
});

function generateUint256Hex() {
    const bytes = new Uint8Array(32);
    crypto.getRandomValues(bytes);
    // turn into a 64-char hex string
    const hex = Array.from(bytes)
        .map(b => b.toString(16).padStart(2, '0'))
        .join('');
    return '0x' + hex;
}
