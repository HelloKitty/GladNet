﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GladNet
{
	public interface INetworkSerializationService
	{
		/// <summary>
		/// Attempts to serialize the provided <paramref name="data"/>.
		/// </summary>
		/// <typeparam name="TTypeToSerialize">Type that is being serialized (can be inferred).</typeparam>
		/// <param name="data">Instance/value to serialize.</param>
		/// <returns>Byte array representation of the object.</returns>
		byte[] Serialize<TTypeToSerialize>(TTypeToSerialize data);

		//We shouldn't expect the deserialize to provide always non-null values.
		//That is a serialization implementation detail.
		/// <summary>
		/// Attempts to deserialize to <typeparamref name="TTypeToDeserializeTo"/> from the provided <see cref="byte[]"/>.
		/// </summary>
		/// <typeparam name="TTypeToDeserializeTo"></typeparam>
		/// <param name="data">Byte repsentation of <typeparamref name="TTypeToDeserializeTo"/>.</param>
		/// <returns>An instance of <typeparamref name="TTypeToDeserializeTo"/> or null if failed.</returns>
		TTypeToDeserializeTo Deserialize<TTypeToDeserializeTo>(byte[] data);
	}
}
