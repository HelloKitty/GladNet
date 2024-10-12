using System;
using System.Collections.Generic;
using System.Text;
using Autofac;

namespace GladNet
{
	/// <summary>
	/// Service module that registers the following services:
	/// <see cref="IMessageSendService{TMessageBaseType}"/>
	/// <see cref="IAsyncMessageQueue{TMessageType}"/> for outgoing write types
	/// <see cref="SessionMessageInterfaceServiceContext{TPayloadReadType,TPayloadWriteType}"/>.
	/// </summary>
	/// <typeparam name="TMessageReadType"></typeparam>
	/// <typeparam name="TMessageWriteType"></typeparam>
	public sealed class GameMessagingServicesModule<TMessageReadType, TMessageWriteType> : Module 
		where TMessageWriteType : class 
		where TMessageReadType : class
	{
		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			//The following registers messaging and message interface dependencies.
			builder.RegisterType<QueueBasedMessageSendService<TMessageWriteType>>()
				.As<IMessageSendService<TMessageWriteType>>()
				.InstancePerLifetimeScope();

			builder.RegisterType<AsyncExProducerConsumerQueueAsyncMessageQueue<TMessageWriteType>>()
				.As<IAsyncMessageQueue<TMessageWriteType>>()
				.InstancePerLifetimeScope();

			builder.RegisterType<SessionMessageInterfaceServiceContext<TMessageReadType, TMessageWriteType>>()
				.AsSelf()
				.InstancePerLifetimeScope();
		}
	}
}
