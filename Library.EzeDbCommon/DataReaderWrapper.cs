using System;
using System.Data;

namespace Libraries.EzeDbCommon
{
	/// <summary>
	///     A wrapper around the IDataReader returned by the db connector that works around some issues
	/// </summary>
	public abstract class DataReaderWrapper : IDataReader
	{
		protected readonly IDataReader _innerReader;

		public DataReaderWrapper(IDataReader innerReader)
		{
			_innerReader = innerReader;
		}

		public void Dispose()
		{
			_innerReader.Dispose();
		}

		public string GetName(int i)
		{
			return _innerReader.GetName(i);
		}

		public string GetDataTypeName(int i)
		{
			return _innerReader.GetDataTypeName(i);
		}

		public Type GetFieldType(int i)
		{
			Type fieldType = _innerReader.GetFieldType(i);
			if (fieldType == typeof(byte[]) && _innerReader.GetName(i).ToLower().Contains("guid"))
			{
				fieldType = typeof(Guid);
			}
			return fieldType;
		}

		public object GetValue(int i)
		{
			if (GetFieldType(i) == typeof(Guid))
			{
				return GetGuid(i);
			}
			return _innerReader.GetValue(i);
		}

		public int GetValues(object[] values)
		{
			int i;
			for (i = 0; i < _innerReader.FieldCount && i < values.Length; ++i)
			{
				values[i] = GetValue(i);
			}
			return i;
		}

		public int GetOrdinal(string name)
		{
			return _innerReader.GetOrdinal(name);
		}

		public bool GetBoolean(int i)
		{
			try
			{
				return _innerReader.GetBoolean(i);
			}
			catch (InvalidCastException)
			{
				return Convert.ToBoolean(_innerReader.GetValue(i));
			}
		}

		public byte GetByte(int i)
		{
			return _innerReader.GetByte(i);
		}

		public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			return _innerReader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
		}

		public char GetChar(int i)
		{
			return _innerReader.GetChar(i);
		}

		public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			return _innerReader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
		}

		public Guid GetGuid(int i)
		{
			Guid guid;
			TryGetGuid(i, out guid);
			return guid;

			//object value = GetValue(i);
			//if (value is Guid)
			//{
			//    return (Guid)value;
			//}
			//if (value is string)
			//{
			//    return new Guid(value as string);
			//}
			//if (value is byte[])
			//{
			//    byte[] array = (byte[])value;
			//    if (array.Length == 16)
			//    {
			//        return new Guid(array);
			//    }
			//}
			//return Guid.Empty;
		}

		public virtual short GetInt16(int i)
		{
			var value = _innerReader.GetValue(i);
			return short.Parse(value.ToString());
			//return _innerReader.GetInt16(i);
		}

		public virtual int GetInt32(int i)
		{
			var value = _innerReader.GetValue(i);
			return int.Parse(value.ToString());
			//return _innerReader.GetInt32(i);
		}

		public virtual long GetInt64(int i)
		{
			var value = _innerReader.GetValue(i);
			return long.Parse(value.ToString());
			//return _innerReader.GetInt64(i);
		}

		public virtual float GetFloat(int i)
		{
			var value = _innerReader.GetValue(i);
			return float.Parse(value.ToString());
			//return _innerReader.GetFloat(i);
		}

		public virtual double GetDouble(int i)
		{
			var value = _innerReader.GetValue(i);
			return double.Parse(value.ToString());
			//return _innerReader.GetDouble(i);
		}

		public virtual string GetString(int i)
		{
			return _innerReader.GetString(i);
		}

		public decimal GetDecimal(int i)
		{
			return _innerReader.GetDecimal(i);
		}

		public DateTime GetDateTime(int i)
		{
			return _innerReader.GetDateTime(i);
		}

		public IDataReader GetData(int i)
		{
			return _innerReader.GetData(i);
		}
		public object GetValue(string name)
		{
			if (GetFieldType(name) == typeof(Guid))
			{
				return GetGuid(name);
			}
			return _innerReader.GetValue(GetOrdinal(name));
		}
		public Type GetFieldType(string name)
		{
			Type fieldType = _innerReader.GetFieldType(GetOrdinal(name));
			if (fieldType == typeof(byte[]) && String.Compare(name, "guid", StringComparison.OrdinalIgnoreCase) == 0)
			{
				fieldType = typeof(Guid);
			}
			return fieldType;
		}

		public bool GetBoolean(string name)
		{
			return GetBoolean(GetOrdinal(name));
		}

		public byte GetByte(string name)
		{
			return GetByte(GetOrdinal(name));
		}

		public char GetChar(string name)
		{
			return GetChar(GetOrdinal(name));
		}

		public Guid GetGuid(string name)
		{
			return GetGuid(GetOrdinal(name));
		}

		public short GetInt16(string name)
		{
			return GetInt16(GetOrdinal(name));
		}

		public int GetInt32(string name)
		{
			return GetInt32(GetOrdinal(name));
		}

		public long GetInt64(string name)
		{
			return GetInt64(GetOrdinal(name));
		}

		public float GetFloat(string name)
		{
			return GetFloat(GetOrdinal(name));
		}

		public double GetDouble(string name)
		{
			return GetDouble(GetOrdinal(name));
		}

		public string GetString(string name)
		{
			return GetString(GetOrdinal(name));
		}

		public decimal GetDecimal(string name)
		{
			return GetDecimal(GetOrdinal(name));
		}

		public DateTime GetDateTime(string name)
		{
			return GetDateTime(GetOrdinal(name));
		}

		public IDataReader GetData(string name)
		{
			return GetData(GetOrdinal(name));
		}

		public bool IsDBNull(int i)
		{
			if (GetFieldType(i) == typeof(Guid))
			{
				Guid guid;
				return !TryGetGuid(i, out guid);
			}
			return _innerReader.IsDBNull(i);
		}

		public int FieldCount
		{
			get { return _innerReader.FieldCount; }
		}

		public object this[int i]
		{
			get { return GetValue(i); }
		}

		public object this[string name]
		{
			get { return GetValue(GetOrdinal(name)); }
		}

		public void Close()
		{
			_innerReader.Close();
		}

		public DataTable GetSchemaTable()
		{
			return _innerReader.GetSchemaTable();
		}

		public bool NextResult()
		{
			return _innerReader.NextResult();
		}

		public bool Read()
		{
			return _innerReader.Read();
		}

		public int Depth
		{
			get { return _innerReader.Depth; }
		}

		public bool IsClosed
		{
			get { return _innerReader.IsClosed; }
		}

		public int RecordsAffected
		{
			get { return _innerReader.RecordsAffected; }
		}

		private bool TryGetGuid(int i, out Guid guid)
		{
			if (_innerReader.IsDBNull(i))
			{
				guid = Guid.Empty;
				return false;
			}

			//if (DbFactory.Instance.Schema == 2)
			//{
			//	try
			//	{
			//		var guidBytes = new byte[16];
			//		long length = GetBytes(i, 0, guidBytes, 0, 16);
			//		for (long index = length; index < 16; ++index)
			//		{
			//			guidBytes[index] = 0x20; // 4.1 versions of mysql strip trailing 0x20 bytes from binary columns
			//		}
			//		guid = new Guid(guidBytes);
			//		return true;
			//	}
			//	catch (NullReferenceException)
			//	{
			//		//sadly, the IsDBNull call using the mysql connector calls "GetValue" which tries to make the binary
			//		// values into a Guid structure and can throw excpetions because of how mysql 4.1 tries spaces in binary columns.
			//		// this means there's no way to check to see if we have a dbnull, we can only try to get the bytes and catch the exception.
			//		guid = Guid.Empty;
			//		return false;
			//	}
			//}

			guid = new Guid(GetString(i));
			return true;
		}
	}
}