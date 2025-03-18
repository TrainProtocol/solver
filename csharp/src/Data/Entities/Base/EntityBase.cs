﻿namespace Train.Solver.Data.Entities.Base;

public abstract class EntityBase<T>
{
    public T Id { get; set; }

    public DateTimeOffset CreatedDate { get; set; }

    public uint Version { get; set; }
}
