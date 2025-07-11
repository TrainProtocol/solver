import { getSchnorrAccount, getSchnorrWallet, SchnorrAccountContractArtifact } from "@aztec/accounts/schnorr";
import { AztecAddress, Contract, createAztecNodeClient, Fr, waitForPXE, ContractInstanceWithAddress, getContractInstanceFromDeployParams, SponsoredFeePaymentMethod, TxHash } from "@aztec/aztec.js";
import { TokenContract } from "@aztec/noir-contracts.js/Token";
import { getPXEServiceConfig } from "@aztec/pxe/config";
import { createPXEService } from "@aztec/pxe/server";
import { deriveSigningKey, PublicKeys } from '@aztec/stdlib/keys';
import { createStore } from '@aztec/kv-store/lmdb';
import { SponsoredFPCContract } from '@aztec/noir-contracts.js/SponsoredFPC';
import { PXECreationOptions } from '../../../../node_modules/@aztec/pxe/src/entrypoints/pxe_creation_options.ts';
import { createHash, randomBytes } from "crypto";
import { TrainContract } from '../Activities/Helper/Train.ts';
import { getSponsoredFPCInstance } from '../Activities/Helper/fpc.ts';

try {

    const TrainContractArtifact = TrainContract.artifact;
    const sponseredFPC = await getSponsoredFPCInstance();

    const TokenContractArtifact = TokenContract.artifact;
    const provider = createAztecNodeClient("https://aztec-alpha-testnet-fullnode.zkv.xyz");
    const paymentMethod = new SponsoredFeePaymentMethod(sponseredFPC.address);

    const userSecretKey = Fr.fromString('0x1d1feba03a14bd74be59a3a9dc93231d54b0716f18cbdd3b65d3e2bd0b30ecaa');
    const userSalt = Fr.fromString("0x199178978cca5913ca8a43210c32f08af7bd0f279c0ba8a5b3f5f82e02b9d31a");

    const fullConfig = {
        ...getPXEServiceConfig(),
        l1Contracts: await provider.getL1ContractAddresses(),
    };

    const store = await createStore('PLOR', {
        dataDirectory: 'store',
        dataStoreMapSizeKB: 1e6,
    });

    const options: PXECreationOptions = {
        loggers: {},
        store,
    };

    const pxe = await createPXEService(provider, fullConfig, options);
    await waitForPXE(pxe);

    // console.log("1:", pxe)

    const schnorrAccount = await getSchnorrAccount(
        pxe,
        userSecretKey,
        deriveSigningKey(userSecretKey),
        userSalt
    );

    // console.log("2:", schnorrAccount)

    const schnorrWallet = await schnorrAccount.getWallet();

    const tokenaddress = "0x0b0a7faf4fb47eac4ac302b3ea990902549b17015e6fda92c934ee522cfda8c2"
    const assetContractInstance = await provider.getContract(AztecAddress.fromString(tokenaddress))
    // console.log("3:")

    await pxe.registerContract({
        instance: assetContractInstance,
        artifact: TokenContractArtifact
    });

    const contractInstance = await provider.getContract(AztecAddress.fromString("0x2734d2a16b57ecb97c5d68cc8e10e2f53b4caf3e1c6bf52e5bb8423952ac6f24"))
    // console.log("654:")

    await pxe.registerContract({
        instance: contractInstance,
        artifact: TrainContractArtifact
    });

    await schnorrAccount.register();

    const token = AztecAddress.fromString(tokenaddress)
    // console.log("4")



    //  const feePayerContractInstance = await provider.getContract(sponseredFPC.address)
    // console.log("65455:")

    await pxe.registerContract({
        instance: sponseredFPC,
        artifact: SponsoredFPCContract.artifact
    });

    const randomes = generateSecretAndHashlock();
    console.log("randomes:", randomes);

    const Id = generateId();
    console.log("ID:", Id);

    const hashlock = randomes[1];
    console.log("hashlock:", hashlock);

    const amount = 1n;
    console.log("amount:", amount);

    const ownership_hash = randomes[1];
    console.log("ownership_hash:", ownership_hash);

    const now = Math.floor(new Date().getTime() / 1000);
    console.log("now (UNIX timestamp):", now);

    const timelock = now + 2000;
    console.log("timelock:", timelock);

    const randomness = generateId();
    console.log("randomness:", randomness);

    const dst_chain = 'TON'.padEnd(8, ' ');
    console.log("dst_chain:", dst_chain);

    const dst_asset = 'TON'.padEnd(8, ' ');
    console.log("dst_asset:", dst_asset);

    const dst_address = 'TON'.padEnd(8, ' ');
    console.log("dst_address:", dst_address);


    // console.log("5")

    // Token contract operations using auth witness
    const asset = await Contract.at(
        token,
        TokenContractArtifact,
        schnorrWallet,
    );

    // console.log("6")
    const transfer = asset
        .withWallet(schnorrWallet)
        .methods.transfer_to_public(
            schnorrWallet.getAddress(),
            AztecAddress.fromString('0x2734d2a16b57ecb97c5d68cc8e10e2f53b4caf3e1c6bf52e5bb8423952ac6f24'),
            amount,
            randomness,
        );

    // console.log("7")
    const witness = await schnorrWallet.createAuthWit({
        caller: AztecAddress.fromString('0x2734d2a16b57ecb97c5d68cc8e10e2f53b4caf3e1c6bf52e5bb8423952ac6f24'),
        action: transfer,
    });

    // console.log("8")
    const trainContract = await Contract.at(
        AztecAddress.fromString('0x2734d2a16b57ecb97c5d68cc8e10e2f53b4caf3e1c6bf52e5bb8423952ac6f24'),
        TrainContractArtifact,
        schnorrWallet,
    );

    // console.log("9")
    // const is_contract_initialized = await trainContract.methods
    //     .is_contract_initialized(Id)
    //     .simulate();

    // if (is_contract_initialized) throw new Error('HTLC Exsists');

    const lockTx = await trainContract.methods
        .lock_private_solver(
            Id,
            hashlock,
            amount,
            ownership_hash,
            timelock,
            token,
            randomness,
            dst_chain,
            dst_asset,
            dst_address,
        ).estimateGas({ authWitnesses: [witness] });
    // .send({ authWitnesses: [witness], fee: { paymentMethod } })//paymentMethod
    // .wait({ timeout: 12000000 });
    // console.log("lockTx.stats.nodeRPCCalls.getTxEffect~~~~~~~~~~ ",lockTx.stats.nodeRPCCalls.getTxEffect)
    // console.log("lockTx.stats.nodeRPCCalls.getPrivateLogs~~~~~~~~~~ ",lockTx.stats.nodeRPCCalls.getPrivateLogs)
    // console.log("lockTx.stats.nodeRPCCalls.getTxReceipt~~~~~~~~~~ ",lockTx.stats.nodeRPCCalls.)


    // .send({ authWitnesses: [witness], fee: { paymentMethod } })//paymentMethod
    // .wait({ timeout: 12000000 });

    console.log('tx : ', lockTx);


    // const blocksData = await provider.getPublicLogs({
    //     contractAddress: AztecAddress.fromString("0x2734d2a16b57ecb97c5d68cc8e10e2f53b4caf3e1c6bf52e5bb8423952ac6f24"),
    //     fromBlock: 69700,
    //     toBlock: 71000,
    // })

    // console.log(blocksData)

















    //     const TokenContractArtifact = TokenContract.artifact;
    //     const provider = createAztecNodeClient("https://aztec-alpha-testnet-fullnode.zkv.xyz");

    //     // const userSecretKey = Fr.fromString('0x2e4e404a1c0cd93104820772923614af46e8d29dbf777408a7bef4eb746bf2ec');
    //     // const partAddress = Fr.fromString('0x17795ec1f4819c3189d5140fdbda763def7a3b747f477337bc221f006f2771ae');
    //     // const userAddress = AztecAddress.fromString('0x17795ec1f4819c3189d5140fdbda763def7a3b747f477337bc221f006f2771ae');
    //     // const userSalt = Fr.fromString("0x103e85691b5e1862a9c3f7eb8c6660e4b3a3b0620fda64d48da872e2ea2e9f62");



    //     const userSecretKey = Fr.fromString('0x1d1feba03a14bd74be59a3a9dc93231d54b0716f18cbdd3b65d3e2bd0b30ecaa');
    //     // const partAddress = Fr.fromString('0x2e5e13dc9db268893291f32b1080f16787d22675137e251f904163147eea55d7');
    //     // const userAddress = AztecAddress.fromString('0x1d6c8c53ab932356bc23932ce37a67952f364c614e91e97013719519b9784e57');
    //     const userSalt = Fr.fromString("0x199178978cca5913ca8a43210c32f08af7bd0f279c0ba8a5b3f5f82e02b9d31a");



    //   // //     {
    //     // //   "userSecretKey": "0x1d1feba03a14bd74be59a3a9dc93231d54b0716f18cbdd3b65d3e2bd0b30ecaa",
    //     // //   "userSalt": "0x199178978cca5913ca8a43210c32f08af7bd0f279c0ba8a5b3f5f82e02b9d31a",
    //     // //   "userAddress": "0x1d6c8c53ab932356bc23932ce37a67952f364c614e91e97013719519b9784e57",
    //     // //   "userPartialAddress": "0x2e5e13dc9db268893291f32b1080f16787d22675137e251f904163147eea55d7",
    //     // //   "solverSecretKey": "0x2bdbcd9120047d15955b73ddbcd918af2e3a985d111e78e83847b3153d30d2d2",
    //     // //   "solverSalt": "0x1dc14c357ed108e2c36af384bd465ce47a1079cdc4f4db2f09ecff1c4e1f05e8",
    //     // //   "solverAddress": "0x01768667c69784cb7d5b00a8e3057a6feb86c583c59510b25dac02c814cafc3f",
    //     // //   "solverPartialAddress": "0x1ec344944be5a4f4c58daacfd96e167c864ed2d0887dd171f2a06ad4a1eb4d8f",
    //     // //   "deployerSecretKey": "0x1d7926cd813f6dd782b3ec381c2a0f7f2d20f470396a539966a2c38cd35f1322",
    //     // //   "deployerSalt": "0x1bd06597b5e9800fab4483d4617b951ab892e1f53b72d8eb4f967968385365de",
    //     // //   "deployerAddress": "0x1061ac77908604d564ff9d2fe439e2e7d40910a54f42813656d76b088d9931c5",
    //     // //   "deployerPartailAddress": "0x2a9772879530f6e59a53a40b0030a82b9296de99dddf3ebd0b4cf1548c3d9036",
    //     // //   "tokenAddress": "0x0d5c4d20d07099e6195f773d6aa46cb50f23a14c73e2f503c3f092e5dceabc4f"
    //     // // }

    //     const fullConfig = {
    //         ...getPXEServiceConfig(),
    //         l1Contracts: await provider.getL1ContractAddresses(),
    //     };

    //     const store = await createStore('PLOR', {
    //         dataDirectory: 'store',
    //         dataStoreMapSizeKB: 1e6,
    //     });

    //     const options: PXECreationOptions = {
    //         loggers: {},
    //         store,
    //     };


    //     const pxe = await createPXEService(provider, fullConfig, options);
    //     await waitForPXE(pxe);


    //     await pxe.registerSender(AztecAddress.fromString("0x0bf0c3cd8f476c952b1a9282bb0b9d3da2d7f6a0f7db45848cc659ef612c5e8b"))



    //     const assetContractInstance = await provider.getContract(AztecAddress.fromString("0x0b0a7faf4fb47eac4ac302b3ea990902549b17015e6fda92c934ee522cfda8c2"))

    //     console.log("SENDERS",await pxe.getSenders())

    //     await pxe.registerContract({
    //         instance: assetContractInstance,
    //         // artifact: TokenContractArtifact
    //     });

    //     const sdsds = await pxe.getContracts();
    //     console.log("GET CONTRACTS         ", sdsds)
    //     const schnorrAccount = await getSchnorrAccount(
    //         pxe,
    //         userSecretKey,
    //         deriveSigningKey(userSecretKey),
    //         userSalt
    //     );

    //     const schnorrWallet = await schnorrAccount.getWallet();

    //     await schnorrAccount.register()

    //     const tokenInstance = await Contract.at(
    //         AztecAddress.fromString("0x0b0a7faf4fb47eac4ac302b3ea990902549b17015e6fda92c934ee522cfda8c2"),
    //         TokenContractArtifact,
    //         schnorrWallet,
    //     );

    //     const assetResponse = await tokenInstance.methods.balance_of_private(schnorrWallet.getAddress())
    //         .simulate();

    //     console.log(assetResponse)

    //     // console.log("assetResponse~~~~~~~~~~~~", assetResponse);


    //     // const ownerPublicBalanceSlot = await cheats.aztec.computeSlotInMap(
    //     //     TokenContract.storage.public_balances.slot,
    //     //     owner.getAddress(),
    //     // );
}
catch (e) {
    console.log(e);
}
// {
//   "userSecretKey": "0x2e4e404a1c0cd93104820772923614af46e8d29dbf777408a7bef4eb746bf2ec",
//   "userSalt": "0x103e85691b5e1862a9c3f7eb8c6660e4b3a3b0620fda64d48da872e2ea2e9f62",
//   "userAddress": "0x17795ec1f4819c3189d5140fdbda763def7a3b747f477337bc221f006f2771ae",
//   "solverSecretKey": "0x028e3235c691fa1d83cefb7ea3d927ca3bd57a1e03a91b8a1e333d32a22beee1",
//   "solverSalt": "0x2b2863f39aeea472921f2c45b00468c062c364919cef51aa807b38c9e6689b76",
//   "solverAddress": "0x18038ab3c78aa11cb99083ae53b0353ae92c22632f9b0ab378c0daa1f3ad2058",
//   "deployerSecretKey": "0x1edd974511ac58e2e4f7dbcf23bb0d462e9adc75b82fa2536e83989ccceca58a",
//   "deployerSalt": "0x2f13829ff98d0230687f698582552ab62119af309f9b75335b0c79c18457a761",
//   "deployerAddress": "0x039749215a004dae1b32d6aee56b08a1336b15d8e305021de70564155dff8746",
//   "tokenAddress": "0x11d76cc29de19710bdfb50878ef77763e24835817147f478e0ef14512958520a"
// }

function generateSecretAndHashlock(): [Uint8Array, Uint8Array] {
    const secret = randomBytes(32);
    const hashlock = createHash('sha256').update(secret).digest();
    return [new Uint8Array(secret), new Uint8Array(hashlock)];
}

function generateId(): bigint {
    const bytes = randomBytes(24);
    return BigInt('0x' + bytes.toString('hex'));
}