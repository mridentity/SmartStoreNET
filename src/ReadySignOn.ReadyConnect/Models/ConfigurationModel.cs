using SmartStore.Web.Framework;
using SmartStore.Web.Framework.Modelling;

namespace ReadySignOn.ReadyConnect.Models
{
    public class ConfigurationModel : ModelBase
    {
        [SmartResourceDisplayName("Plugins.ExternalAuth.ReadyConnect.ClientKeyIdentifier")]
        public string ClientKeyIdentifier { get; set; }

        [SmartResourceDisplayName("Plugins.ExternalAuth.ReadyConnect.ClientSecret")]
        public string ClientSecret { get; set; }

		[SmartResourceDisplayName("Plugins.ExternalAuth.ReadyConnect.RedirectUri")]
		public string RedirectUrl { get; set; }
	}
}