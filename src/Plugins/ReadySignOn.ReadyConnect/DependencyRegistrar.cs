using Autofac;
using ReadySignOn.ReadyConnect.Core;
using SmartStore.Core.Infrastructure;
using SmartStore.Core.Infrastructure.DependencyManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReadySignOn.ReadyConnect
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, bool isActiveModule)
        {
            builder.RegisterType<ReadyConnectProviderAuthorizer>().As<IOAuthProviderReadyConnectAuthorizer>().InstancePerRequest();
        }

        public int Order
        {
            get { return 1; }
        }
    }
}