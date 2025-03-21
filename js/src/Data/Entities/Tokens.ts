import {
  Column,
  Entity,
  Index,
  JoinColumn,
  ManyToOne,
  PrimaryGeneratedColumn,
} from "typeorm";
import { Networks } from "./Networks";

@Index("IX_Tokens_NetworkId_Asset", ["asset", "networkId"], { unique: true })
@Index("PK_Tokens", ["id"], { unique: true })
@Index("IX_Tokens_TokenPriceId", ["tokenPriceId"], {})
@Entity("Tokens", { schema: "public" })
export class Tokens {
  @PrimaryGeneratedColumn({ type: "integer", name: "Id" })
  id: number;

  @Column("text", { name: "Asset" })
  asset: string;

  @Column("text", { name: "TokenContract", nullable: true })
  tokenContract: string | null;

  @Column("boolean", { name: "IsNative" })
  isNative: boolean;

  @Column("integer", { name: "Precision" })
  precision: number;

  @Column("integer", { name: "Decimals" })
  decimals: number;

  @Column("integer", { name: "NetworkId" })
  networkId: number;

  @Column("timestamp with time zone", {
    name: "CreatedDate",
    default: () => "now()",
  })
  createdDate: Date;

  @Column("text", { name: "Logo", default: () => "''" })
  logo: string;

  @Column("integer", { name: "TokenPriceId" })
  tokenPriceId: number;

  @ManyToOne(() => Networks, (networks) => networks.tokens, {
    onDelete: "CASCADE",
  })
  @JoinColumn([{ name: "NetworkId", referencedColumnName: "id" }])
  network: Networks;

}
