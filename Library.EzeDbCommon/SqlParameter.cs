using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Libraries.EzeDbCommon
{
	public class SqlParameter
	{
		public readonly string Name;
		public readonly object Value;
		public SqlParameter(string name, object value)
		{
			Name = name;
			Value = PrepareForDatabase(value);
		}
		private static object PrepareForDatabase(object value)
		{
			if (value is DateTimeOffset)
			{
				var dateTimeOffset = (DateTimeOffset)value;

				if (dateTimeOffset > DateTimeOffset.MinValue && dateTimeOffset < DateTimeOffset.MaxValue)
				{
					return dateTimeOffset.ToUniversalTime();
				}
			}
			return value ?? DBNull.Value;
		}

		public static string ArrayAsString(IEnumerable array)
		{
			//return ArrayAsString(array, DbFactory.Instance.Schema);
			return null;
		}

		public static string ArrayAsString(IEnumerable array, int schemaVersion)
		{
			var sb = new StringBuilder("(");

			bool first = true;
			foreach (object obj in array)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					sb.Append(", ");
				}

				sb.Append(EncodeForSql(obj, schemaVersion));
			}

			sb.Append(")");

			return sb.ToString();
		}

		public static string EncodeForSql(object value, int schemaVersion)
		{
			if (value == null)
			{
				return "NULL";
			}

			if (value is string)
			{
				return WrapString(value as string);
			}

			if (value is DateTimeOffset)
			{
				return ((DateTimeOffset)value).ToUniversalTime().ToString("\"'\"yyyy-M-d H:mm:ss\"'\"");
			}

			if (value is DateTime)
			{
				return ((DateTime)value).ToUniversalTime().ToString("\"'\"yyyy-M-d H:mm:ss\"'\"");
			}

			if (value is Guid)
			{
				var guid = (Guid)value;

				if (schemaVersion == 2)
				{
					return WrapGuid(guid);
				}

				return WrapString(guid.ToString());
			}

			if (value.GetType().IsEnum)
			{
				return WrapString(((Enum)value).ToString("D"));
			}

			return value.ToString();
		}

		/// <summary>
		///     A utility function to wrap string in single quotes and escape or remove
		///     any nasty characters
		/// </summary>
		/// <param name="source">The original string</param>
		/// <returns>The quoted, cleaned string</returns>
		public static string WrapString(string source)
		{
			//return source.EscapeSpecialCharacter();
			return null;
		}

		private static string WrapGuid(Guid guid)
		{
			byte[] guidBytes = guid.ToByteArray();

			int stringSize = (guidBytes.Length * 2) + 2;
			var sb = new StringBuilder(stringSize, stringSize);
			int trailingSpace = 0;
			// mysql 4.1 strips trailing spaces, so when we query (like for concurrency checks), we need to strip them too.
			sb.Append("0x");
			foreach (byte b in guidBytes)
			{
				if (b == 0x20)
				{
					++trailingSpace;
				}
				else
				{
					trailingSpace = 0;
				}
				sb.Append(b.ToString("X2"));
			}

			return sb.ToString(0, sb.Length - (trailingSpace * 2));
		}

		public static string ArrayAsPairs(string keyParam, IEnumerable<string> valueParams)
		{
			IEnumerable<string> pairs = valueParams.Select(value => string.Format("({0}, {1})", keyParam, value));

			var sb = new StringBuilder();

			bool first = true;

			foreach (string obj in pairs)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					sb.Append(", ");
				}

				sb.Append(obj);
			}

			return sb.ToString();
		}
	}
}