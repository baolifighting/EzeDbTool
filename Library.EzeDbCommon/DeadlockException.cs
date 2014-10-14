using System;
using System.Runtime.Serialization;

namespace Libraries.EzeDbCommon
{
	/// <summary>
	///     The exception that's thrown when the database sees a deadlock
	/// </summary>
	[Serializable]
	public class DeadlockException : Exception
	{
		public const int MaxRetryCount = 5;
		public const int MinLogCount = 1;

		public DeadlockException()
		{
		}

		// for deserialization

		protected DeadlockException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		public DeadlockException(string msg, Exception ex)
			: base(msg, ex)
		{
		}
	}
}
