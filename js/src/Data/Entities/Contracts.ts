import {
  Column,
  Entity,
  Index,
  JoinColumn,
  ManyToOne,
  PrimaryGeneratedColumn,
} from "typeorm";
import { Networks } from "./Networks";

@Index("PK_Contracts", ["id"], { unique: true })
@Index("IX_Contracts_NetworkId", ["networkId"], {})
@Entity("Contracts", { schema: "public" })
export class Contracts {
  @PrimaryGeneratedColumn({ type: "integer", name: "Id" })
  id: number;

  @Column("integer", { name: "Type" })
  type: number;

  @Column("text", { name: "Address" })
  address: string;

  @Column("timestamp with time zone", {
    name: "CreatedDate",
    default: () => "now()",
  })
  createdDate: Date;

  @Column("integer", { name: "NetworkId", default: () => "0" })
  networkId: number;

  @ManyToOne(() => Networks, (networks) => networks.contracts, {
    onDelete: "CASCADE",
  })
  @JoinColumn([{ name: "NetworkId", referencedColumnName: "id" }])
  network: Networks;
}

export enum ContractType {
  HTLCNativeContractAddress ,
  HTLCTokenContractAddress,
  GasPriceOracleContract,
  EvmMultiCallContract,
}