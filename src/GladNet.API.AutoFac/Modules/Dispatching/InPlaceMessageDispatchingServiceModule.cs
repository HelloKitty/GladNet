using System;
using System.Collections.Generic;
using System.Text;
using Autofac;

namespace GladNet
{
	//From Booma
	/// <summary>
	/// Service registration module for <see cref="INetworkMessageDispatchingStrategy{TPayloadReadType,TPayloadWriteType}"/>.
	/// Registers <see cref="InPlaceMessageDispatchingServiceModule{TMessageReadType,TMessageWriteType}"/> for in-place message dispatching.
	/// </summary>
	/// <typeparam name="TMessageReadType"></typeparam>
	/// <typeparam name="TMessageWriteType"></typeparam>
	public sealed class InPlaceMessageDispatchingServiceModule<TMessageReadType, TMessageWriteType> : Module 
		where TMessageWriteType : class 
		where TMessageReadType : class
	{
		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			//Register just inplace dispatching which is basically handle on same thread.
			builder.RegisterType<InPlaceNetworkMessageDispatchingStrategy<TMessageReadType, TMessageWriteType>>()
				.AsImplementedInterfaces()
				.InstancePerLifetimeScope();
		}
	}
}
