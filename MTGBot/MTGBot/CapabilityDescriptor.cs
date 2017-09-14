using Newtonsoft.Json;

namespace MTGBot
{
    class CapabilityDescriptorIcon
    {
        public string url { get; set; }

        [JsonProperty("url@2x")]
        public string url2x { get; set; }
    }

    class CapabilityDescriptorLinks
    {
        public string homepage { get; set; }
        public string self { get; set; }
    }

    class CapabilityDescriptorVendor
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    class CapabilityDescriptorConsumer
    {
        public string fromName { get; set; }
        public string[] scopes { get; set; }
    }
    class CapabilityDescriptorInstallable
    {
        public bool allowGlobal { get; set; }
        public bool allowRoom { get; set; }
        public string callbackUrl { get; set; }
        public string uninstalledUrl { get; set; }

    }
    class CapabilityDescriptorWebhook
    {
        public string Url { get; set; }
        public string @event { get; set; }
        public string pattern { get; set; }
        public string name { get; set; }
        public string authentication { get; set; }
    }
    class CapabilityDescriptorConfigurable
    {
        public string url { get; set; }
    }

    class CapabilityDescriptorValue
    {
        public string value { get; set; }
    }

    class CapabilityDescriptorGlance
    {
        public CapabilityDescriptorIcon icon { get; set; }
        public string key { get; set; }
        CapabilityDescriptorValue name { get; set; }
        public string queryUrl { get; set; }
        public string target { get; set; }
    }
    class CapabilityDescriptorWebPanel
    {
        public CapabilityDescriptorIcon icon { get; set; }
        public string key { get; set; }
        CapabilityDescriptorValue name { get; set; }
        public string url { get; set; }
        public string location { get; set; }
    }

    class CapabilityDescriptorDialog
    {
        public CapabilityDescriptorValue title { get; set; }
        public string key { get; set; }
        public object options { get; set; }
        public string url { get; set; }
    }

    class CapabilityDescriptorAction
    {
        public string key { get; set; }
        public CapabilityDescriptorValue name { get; set; }
        public string target { get; set; }
        public string location { get; set; }
    }

    class CapabilityDescriptorCapabilities
    {
        public CapabilityDescriptorConsumer hipchatApiConsumer { get; set; }
        public CapabilityDescriptorInstallable installable { get; set; }
        public CapabilityDescriptorWebhook[] webhook { get; set; }
        public CapabilityDescriptorConfigurable configurable { get; set; }
        public CapabilityDescriptorGlance[] glance { get; set; }
        public CapabilityDescriptorWebPanel[] webpanel { get; set; }
        public CapabilityDescriptorDialog[] dialog { get; set; }
        public CapabilityDescriptorAction[] action { get; set; }
    }

    class CapabilityDescriptor
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Key { get; set; }
        public CapabilityDescriptorLinks Links { get; set; }
        public CapabilityDescriptorVendor Vendor { get; set; }
        public CapabilityDescriptorCapabilities Capabilities { get; set; }
    }
}
