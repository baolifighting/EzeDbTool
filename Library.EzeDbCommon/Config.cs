using System;

namespace Libraries.EzeDbCommon
{
	public class Config
	{
		public static string ToDateTimeStamp(DateTime dateTime)
		{
			return dateTime.ToString("yyyy-M-d H:mm:ss");
		}

		public static string ToDateTimeStamp(DateTimeOffset dateTime)
		{
			return dateTime.ToString("yyyy-M-d H:mm:ss");
		}

		public static string ToTime(TimeSpan span)
		{
			//Just return ToString() here. We may need to extend this later.
			return span.ToString();
		}

		public static string ToMilliseconds(DateTimeOffset dateTime)
		{
			return dateTime.ToString("fff");
		}
	}
}