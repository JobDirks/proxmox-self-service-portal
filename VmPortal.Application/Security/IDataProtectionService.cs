using System;
using System.Collections.Generic;
using System.Text;

namespace VmPortal.Application.Security
{
    public interface IDataProtectionService
    {
        string ProtectPII(string plainText);
        string UnprotectPII(string protectedText);
        string ProtectSession(string plainText);
        string UnprotectSession(string protectedText);
        bool IsProtected(string value);
    }
}
