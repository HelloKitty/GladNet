using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Autofac;
using Glader.Essentials;
using GladNet;
using Module = Autofac.Module;

namespace GladNet
{
	//From Booma
	/// <summary>
	/// Game message handler service module.
	/// Registers the services and handling for message handlers.
	/// Registers the following services:
	/// <see cref="BaseDefaultMessageHandler{TMessageType,TMessageContext}"/> for <typeparamref name="TDefaultHandlerType"/>
	/// <para />
	/// <see cref="DefaultMessageHandlerService{TMessageType,TMessageContext}"/> for specified message types parameters.
	/// </summary>
	public abstract class GameMessageHandlerServiceModule<TMessageReadType, TMessageWriteType, TDefaultHandlerType> : Module 
		where TMessageReadType : class 
		where TMessageWriteType : class
		where TDefaultHandlerType : BaseDefaultMessageHandler<TMessageReadType, SessionMessageContext<TMessageWriteType>>
	{
		/// <inheritdoc />
		protected sealed override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			//We Register the default handler because we'll internally bind it to the handler service
			//we create. This simplifies handler discovery abit too.
			builder.RegisterType<TDefaultHandlerType>()
				.AsSelf()
				.As<BaseDefaultMessageHandler<TMessageReadType, SessionMessageContext<TMessageWriteType>>>()
				.SingleInstance();

			//New Design makes these handlers NOT stateless. It makes certain things WAY easier to deal with.
			builder
				.RegisterType<DefaultMessageHandlerService<TMessageReadType, SessionMessageContext<TMessageWriteType>>>()
				.As<IMessageHandlerService<TMessageReadType, SessionMessageContext<TMessageWriteType>>>()
				.OnActivated(args =>
				{
					//Bind one of the default handlers
					var handler = args.Context.Resolve<TDefaultHandlerType>();
					args.Instance.Bind<TMessageReadType>(handler);

					//Now we resolve ALL bindable handlers
					//Any handler that is registered will now be bound to this handler service.
					foreach (var bindable in args.Context.Resolve<IEnumerable<ITypeBindable<IMessageHandler<TMessageReadType, SessionMessageContext<TMessageWriteType>>, TMessageReadType>>>())
					{
						bindable.BindTo(args.Instance);
					}
				})
				.InstancePerLifetimeScope();

			RegisterHandlers(builder);
		}

		/// <summary>
		/// Implementers can register additional handlers here.
		/// </summary>
		/// <param name="builder"></param>
		protected virtual void RegisterHandlers(ContainerBuilder builder)
		{

		}
	}
}
