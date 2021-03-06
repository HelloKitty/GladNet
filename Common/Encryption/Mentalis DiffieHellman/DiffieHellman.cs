//
// DiffieHellman.cs: Defines a base class from which all Diffie-Hellman implementations inherit
//
// Author:
//	Pieter Philippaerts (Pieter@mentalis.org)
//
// (C) 2003 The Mentalis.org Team (http://www.mentalis.org/)
//

using System;
using System.Text;
using System.Security;
using System.Security.Cryptography;

namespace Org.Mentalis.Security.Cryptography {
	/// <summary>
	/// Defines a base class from which all Diffie-Hellman implementations inherit.
	/// </summary>
	public abstract class DiffieHellman : AsymmetricAlgorithm {
		/// <summary>
		/// Creates an instance of the default implementation of the <see cref="DiffieHellman"/> algorithm.
		/// </summary>
		/// <returns>A new instance of the default implementation of DiffieHellman.</returns>
		public static new DiffieHellman Create () {
			return Create ("Mono.Security.Cryptography.DiffieHellman");
		}
		/// <summary>
		/// Creates an instance of the specified implementation of <see cref="DiffieHellman"/>.
		/// </summary>
		/// <param name="algName">The name of the implementation of DiffieHellman to use.</param>
		/// <returns>A new instance of the specified implementation of DiffieHellman.</returns>
		public static new DiffieHellman Create (string algName) {
			return (DiffieHellman) CryptoConfig.CreateFromName (algName);
		}

		/// <summary>
		/// Initializes a new <see cref="DiffieHellman"/> instance.
		/// </summary>
		public DiffieHellman() {}

		/// <summary>
		/// When overridden in a derived class, creates the key exchange data. 
		/// </summary>
		/// <returns>The key exchange data to be sent to the intended recipient.</returns>
		public abstract byte[] CreateKeyExchange();
		/// <summary>
		/// When overridden in a derived class, extracts secret information from the key exchange data.
		/// </summary>
		/// <param name="keyEx">The key exchange data within which the secret information is hidden.</param>
		/// <returns>The secret information derived from the key exchange data.</returns>
		public abstract byte[] DecryptKeyExchange(byte[] keyEx);

		/// <summary>
		/// When overridden in a derived class, exports the <see cref="DHParameters"/>.
		/// </summary>
		/// <param name="includePrivate"><b>true</b> to include private parameters; otherwise, <b>false</b>.</param>
		/// <returns>The parameters for Diffie-Hellman.</returns>
		public abstract DHParameters ExportParameters (bool includePrivate);
		/// <summary>
		/// When overridden in a derived class, imports the specified <see cref="DHParameters"/>.
		/// </summary>
		/// <param name="parameters">The parameters for Diffie-Hellman.</param>
		public abstract void ImportParameters (DHParameters parameters);

		private byte[] GetNamedParam(SecurityElement se, string param) {
			SecurityElement sep = se.SearchForChildByTag(param);
			if (sep == null)
				return null;
			return Convert.FromBase64String(sep.Text);
		}
	}
}