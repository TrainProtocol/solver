using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.API.Models;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class DetailedTokenDto : TokenDto
{
    public string Logo { get; set; } = null!;

    public DateTimeOffset ListingDate { get; set; }
}
