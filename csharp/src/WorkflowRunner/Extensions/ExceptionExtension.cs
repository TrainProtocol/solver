﻿using Temporalio.Exceptions;

namespace Train.Solver.WorkflowRunner.Extensions;

public static class ExceptionExtension
{
    public static bool HasError<T>(this ApplicationFailureException ex) where T : Exception
    {
        return ex.ErrorType == typeof(T).Name;
    }
}
