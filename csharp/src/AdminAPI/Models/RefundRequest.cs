using Train.Solver.Common.Enums;

namespace Train.Solver.AdminAPI.Models;

public class RefundRequest
{
    public NetworkType Type { get; set; }

    public string Address { get; set; }
}
