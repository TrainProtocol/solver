import {
  Column,
  Entity,
  Index,
  JoinColumn,
  ManyToOne,
  PrimaryGeneratedColumn,
} from "typeorm";
import { Networks } from "./Networks";

@Index("PK_Nodes", ["id"], { unique: true })
@Index("IX_Nodes_Type_NetworkId", ["networkId", "type"], { unique: true })
@Index("IX_Nodes_NetworkId", ["networkId"], {})
@Entity("Nodes", { schema: "public" })
export class Nodes {
  @PrimaryGeneratedColumn({ type: "integer", name: "Id" })
  id: number;

  @Column("text", { name: "Url" })
  url: string;

  @Column("integer", { name: "Type" })
  type: number;

  @Column("integer", { name: "NetworkId" })
  networkId: number;

  @Column("boolean", { name: "TraceEnabled" })
  traceEnabled: boolean;

  @Column("double precision", { name: "Priority", precision: 53 })
  priority: number;

  @Column("timestamp with time zone", {
    name: "CreatedDate",
    default: () => "now()",
  })
  createdDate: Date;

  @ManyToOne(() => Networks, (networks) => networks.nodes, {
    onDelete: "CASCADE",
  })
  @JoinColumn([{ name: "NetworkId", referencedColumnName: "id" }])
  network: Networks;
}

export enum NodeType {
  Primary ,
  DepositTracking,
  Public,
  Secondary,
}
