using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
namespace Libraries.EzeDbCommon
{
	class SqlServerDataReaderWrapperImpl : DataReaderWrapper
	{
		public SqlServerDataReaderWrapperImpl(IDataReader innerReader)
			: base(innerReader)
		{
		}

		public override short GetInt16(int i)
		{
			var value = _innerReader.GetValue(i);
			return short.Parse(value.ToString());
		}

		public override int GetInt32(int i)
		{
			var value = _innerReader.GetValue(i);
			return int.Parse(value.ToString());
		}

		public override long GetInt64(int i)
		{
			var value = _innerReader.GetValue(i);
			return long.Parse(value.ToString());
		}

		public override float GetFloat(int i)
		{
			var value = _innerReader.GetValue(i);
			return float.Parse(value.ToString());
		}

		public override double GetDouble(int i)
		{
			var value = _innerReader.GetValue(i);
			return double.Parse(value.ToString());
		}

		public override string GetString(int i)
		{
			return _innerReader.GetValue(i).ToString();
		}
	}
}