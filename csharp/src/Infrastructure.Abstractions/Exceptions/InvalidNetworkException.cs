using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class InvalidNetworkException(string message) : UserFacingException(message)
{
}