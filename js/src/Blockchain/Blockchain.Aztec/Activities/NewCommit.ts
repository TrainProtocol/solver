import { getSchnorrAccount, getSchnorrWallet, SchnorrAccountContractArtifact } from "@aztec/accounts/schnorr";
import { AztecAddress, Contract, createAztecNodeClient, Fr, waitForPXE, ContractInstanceWithAddress, getContractInstanceFromDeployParams, SponsoredFeePaymentMethod } from "@aztec/aztec.js";
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

    const decimal = 1752159541;
    const hex = decimal.toString(16)
    console.log(Fr.fromHexString(hex))

    
}
catch (e) {
    console.log("ERROR::", e);
}