namespace VmPortal.Application.Vms
{
    public class VmResourceLimitsOptions
    {
        public int MaxDiskGiB { get; set; } = 120;

        public int MaxCpuCores { get; set; } = 8;

        public int MaxMemoryMiB { get; set; } = 16384;
    }
}
