using Autofac;
using ReadySignOn.ReadyPay.Services;
using SmartStore.Core.Infrastructure;
using SmartStore.Core.Infrastructure.DependencyManagement;
using SmartStore.Web.Controllers;

namespace ReadySignOn.ReadyPay
{
	public class DependencyRegistrar : IDependencyRegistrar
	{
		public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, bool isActiveModule)
		{
			builder.RegisterType<ReadyPayService>().As<IReadyPayService>().InstancePerRequest();
		}

		public int Order
		{
			get { return 1; }
		}
	}
}
