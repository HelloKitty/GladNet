using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Autofac;
using Glader.Essentials;
using Module = Autofac.Module;

namespace GladNet
{
	/// <summary>
	/// Service module that registers all message handlers within the provided <see cref="Assembly"/>
	/// </summary>
	public class AssemblyMessageHandlerServiceModule<TMessageReadType, TMessageWriteType> : Module where TMessageReadType : class where TMessageWriteType : class
	{
		private Assembly TargetAssembly { get; }

		public AssemblyMessageHandlerServiceModule(Assembly targetAssembly)
		{
			TargetAssembly = targetAssembly ?? throw new ArgumentNullException(nameof(targetAssembly));
		}

		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			//Register all handlers in the assembly
			foreach(var handler in GetHandlerTypes(TargetAssembly))
				RegisterHandler(builder, handler);
		}

		/// <summary>
		/// Registers a bindable <see cref="IMessageHandler{TMessageType,TMessageContext}"/> in the provider container.
		/// </summary>
		/// <typeparam name="THandlerType">The handler to register.</typeparam>
		/// <param name="builder"></param>
		/// <returns></returns>
		private ContainerBuilder RegisterHandler<THandlerType>(ContainerBuilder builder)
			where THandlerType : IMessageHandler<TMessageReadType, SessionMessageContext<TMessageWriteType>>,
			ITypeBindable<IMessageHandler<TMessageReadType, SessionMessageContext<TMessageWriteType>>, TMessageReadType>
		{
			RegisterHandler(builder, typeof(THandlerType));
			return builder;
		}

		/// <summary>
		/// Registers a bindable <see cref="IMessageHandler{TMessageType,TMessageContext}"/> in the provider container.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="handlerType">Type of handler.</param>
		/// <exception cref="ArgumentException">Throws if the handler type isn't a message handler.</exception>
		/// <returns></returns>
		private static void RegisterHandler(ContainerBuilder builder, Type handlerType)
		{
			//New design will give a unique handler per session.
			//Makes this SO much easier in some cases.
			var registrationBuilder = builder.RegisterType(handlerType)
				.As<ITypeBindable<IMessageHandler<TMessageReadType, SessionMessageContext<TMessageWriteType>>, TMessageReadType>>()
				.InstancePerLifetimeScope();

			//TODO: Assert it is assignable to.
			foreach(var additional in handlerType.GetCustomAttributes<AdditionalRegistrationAsAttribute>())
				registrationBuilder = registrationBuilder
					.As(additional.ServiceType);
		}

		/// <summary>
		/// Parses the provided <see cref="Assembly"/> to locate all handler types.
		/// </summary>
		/// <param name="assembly">The assembly to parse.</param>
		/// <returns>Enumerable of all available message handler types.</returns>
		private static IEnumerable<Type> GetHandlerTypes(Assembly assembly)
		{
			if(assembly == null) throw new ArgumentNullException(nameof(assembly));

			return assembly.GetTypes()
				.Where(t => t.IsAssignableTo<ITypeBindable<IMessageHandler<TMessageReadType, SessionMessageContext<TMessageWriteType>>, TMessageReadType>>())
				.Where(t => !t.IsAbstract)
				.Where(t => !t.IsAssignableTo<BaseDefaultMessageHandler<TMessageReadType, SessionMessageContext<TMessageWriteType>>>()) //not a default handler
				.ToArray();
		}
	}
}
