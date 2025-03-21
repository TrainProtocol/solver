import {
  Column,
  Entity,
  Index,
  OneToMany,
  PrimaryGeneratedColumn,
} from "typeorm";
import { Contracts } from "./Contracts";
import { ManagedAccounts } from "./ManagedAccounts";
import { Nodes } from "./Nodes";
import { Tokens } from "./Tokens";

@Index("PK_Networks", ["id"], { unique: true })
@Index("IX_Networks_Name", ["name"], { unique: true })
@Entity("Networks", { schema: "public" })
export class Networks {
  @PrimaryGeneratedColumn({ type: "integer", name: "Id" })
  id: number;

  @Column("text", { name: "Name" })
  name: string;

  @Column("text", { name: "DisplayName" })
  displayName: string;

  @Column("integer", { name: "Group" })
  group: number;

  @Column("text", { name: "ChainId", nullable: true })
  chainId: string | null;

  @Column("integer", { name: "FeePercentageIncrease" })
  feePercentageIncrease: number;

  @Column("integer", { name: "GasLimitPercentageIncrease", nullable: true })
  gasLimitPercentageIncrease: number | null;

  @Column("text", { name: "FixedGasPriceInGwei", nullable: true })
  fixedGasPriceInGwei: string | null;

  @Column("text", { name: "TransactionExplorerTemplate" })
  transactionExplorerTemplate: string;

  @Column("text", { name: "AccountExplorerTemplate" })
  accountExplorerTemplate: string;

  @Column("boolean", { name: "IsTestnet" })
  isTestnet: boolean;

  @Column("integer", { name: "ReplacementFeePercentage" })
  replacementFeePercentage: number;

  @Column("boolean", { name: "IsExternal" })
  isExternal: boolean;

  @Column("timestamp with time zone", {
    name: "CreatedDate",
    default: () => "now()",
  })
  createdDate: Date;

  @Column("text", { name: "Logo", default: () => "''" })
  logo: string;

  @OneToMany(() => Contracts, (contracts) => contracts.network)
  contracts: Contracts[];

  @OneToMany(
    () => ManagedAccounts,
    (managedAccounts) => managedAccounts.network
  )
  managedAccounts: ManagedAccounts[];

  @OneToMany(() => Nodes, (nodes) => nodes.network)
  nodes: Nodes[];

  @OneToMany(() => Tokens, (tokens) => tokens.network)
  tokens: Tokens[];
}
