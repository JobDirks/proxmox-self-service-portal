namespace VmPortal.Application.Proxmox
{
    public sealed class ProxmoxTagOptions
    {
        public string ManagedVmTag { get; set; } = "vmportal-managed";
        public string TemplateTag { get; set; } = "vmportal-template";
        public string TemplateNameTagPrefix { get; set; } = "tmpl-";
    }
}
