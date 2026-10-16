using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
    }
}
