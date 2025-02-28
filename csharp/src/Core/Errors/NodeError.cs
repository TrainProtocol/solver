namespace Train.Solver.Core.Errors;

public class NodeError(string errorMessage) : InternalError(errorMessage)
{
}
