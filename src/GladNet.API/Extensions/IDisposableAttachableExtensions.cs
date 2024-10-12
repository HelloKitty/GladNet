using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Glader.Essentials;

namespace GladNet
{
	/// <summary>
	/// Extensions for <see cref="IDisposableAttachable"/> type.
	/// </summary>
	public static class IDisposableAttachableExtensions
	{
		/// <summary>
		/// Attaches the disposable instance to the object.
		/// </summary>
		/// <param name="attachTarget">The target to attach the disposable to.</param>
		/// <param name="disposable">The disposable to attach.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Obsolete("This method has been deprecated. Use IDisposableAttachable.AttachDisposable.")]
		public static void AttachDisposableResource(this IDisposableAttachable attachTarget, IDisposable disposable)
		{
			if (attachTarget == null) throw new ArgumentNullException(nameof(attachTarget));
			attachTarget.AttachDisposable(disposable);
		}
	}
}
