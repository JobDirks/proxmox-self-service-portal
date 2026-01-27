namespace VmPortal.Application.Security
{
    public interface IInputSanitizer
    {
        string SanitizeVmName(string input);
        string SanitizeNodeName(string input);
        bool IsValidVmId(int vmId);
        string SanitizeForDisplay(string input);
        string SanitizeForLog(string input);
    }
}
