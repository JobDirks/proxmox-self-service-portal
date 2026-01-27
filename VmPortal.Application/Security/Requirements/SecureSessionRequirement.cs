using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace VmPortal.Application.Security.Requirements
{
    public class SecureSessionRequirement : IAuthorizationRequirement
    {
        public bool RequireSessionHash { get; }
        public int MaxSessionAgeMinutes { get; }

        public SecureSessionRequirement(bool requireSessionHash = true, int maxSessionAgeMinutes = 480)
        {
            RequireSessionHash = requireSessionHash;
            MaxSessionAgeMinutes = maxSessionAgeMinutes;
        }
    }
}
