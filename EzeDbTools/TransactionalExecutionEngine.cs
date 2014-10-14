using Libraries.EzeDbCommon;
using System;

namespace EzeDbTools
{
	public class TransactionalExecutionEngine : SimpleExecutionEngine
	{
		#region Variables
		private bool __existingTransaction;
		private bool _mergeTransactions = true;

#if TRANSACTIONDEBUGGING
        string _transactionDebugging;
#endif

		#endregion

		#region Properties

		public Exception LastException
		{
			get
			{
				return _lastException;
			}
		}

		private bool _existingTransaction
		{
			get
			{
				return __existingTransaction;
			}
			set
			{
#if TRANSACTIONDEBUGGING
                _transactionDebugging = value ? "no transaction" : new StackTrace().ToString();
#endif
				__existingTransaction = value;
			}
		}

		#endregion

		#region Lifetime

		public TransactionalExecutionEngine()
			: this(true)
		{
		}

		public TransactionalExecutionEngine(bool mergeTransactions)
			: base(new DataReader(false))
		{
			_mergeTransactions = mergeTransactions;
			_existingTransaction = false;
		}

		public void Dispose()
		{
			if (_dbAccess != null)
			{
				if (_existingTransaction)
				{
					_dbAccess.Rollback();
					_existingTransaction = false;
				}
				_dbAccess.Dispose();
			}
		}

		#endregion

		#region Methods

		public void NewConnection()
		{
			NewConnection(new DataReader(false));
		}

		public void CommitExistingTransaction()
		{
			if (_existingTransaction)
			{
				_dbAccess.Commit();
				_existingTransaction = false;
			}
		}

		public override bool ExecuteSql(string content)
		{
			if (!_existingTransaction)
			{
#if TRANSACTIONDEBUGGING
                try
                {
#endif
				_dbAccess.BeginTransaction();
#if TRANSACTIONDEBUGGING
                }
                catch (InvalidOperationException ioe)
                {
                    throw new InvalidOperationException(string.Format("{0}{1}Old transaction state: {2}", ioe.Message, Environment.NewLine, _transactionDebugging), ioe);
                }
#endif
				_existingTransaction = true;
			}
			bool success = true;
			if (!string.IsNullOrEmpty(content))
			{
				success = base.ExecuteSql(content);
			}

			if (success)
			{
				if (_existingTransaction && !_mergeTransactions)
				{
					_dbAccess.Commit();
					_existingTransaction = false;
				}
			}
			else
			{
				if (_existingTransaction)
				{
					_dbAccess.Rollback();
					_existingTransaction = false;
				}
			}

			return success;
		}

//		public override bool ExecuteCSharp(string content)
//		{
//			if (!_existingTransaction)
//			{
//#if TRANSACTIONDEBUGGING
//				try
//				{
//#endif
//				_dbAccess.BeginTransaction();
//#if TRANSACTIONDEBUGGING
//				}
//				catch (InvalidOperationException ioe)
//				{
//					throw new InvalidOperationException(string.Format("{0}{1}Old transaction state: {2}", ioe.Message, Environment.NewLine, _transactionDebugging), ioe);
//				}
//#endif
//				_existingTransaction = true;
//			}

//			bool success = base.ExecuteCSharp(content);

//			if (success)
//			{
//				if (_existingTransaction && !_mergeTransactions)
//				{
//					_dbAccess.Commit();
//					_existingTransaction = false;
//				}
//			}
//			else
//			{
//				if (_existingTransaction)
//				{
//					_dbAccess.Rollback();
//					_existingTransaction = false;
//				}
//			}

//			return success;
//		}

		//public override bool ExecuteVisualBasic(string content)
		//{
		//	if (_existingTransaction)
		//	{
		//		_dbAccess.Commit();
		//		_existingTransaction = false;
		//	}

		//	return base.ExecuteVisualBasic(content);
		//}

		//public override bool ExecuteJs(string content)
		//{
		//	if (_existingTransaction)
		//	{
		//		_dbAccess.Commit();
		//		_existingTransaction = false;
		//	}

		//	return base.ExecuteJs(content);
		//}

		//public override bool ExecuteProcess(string fileLocation)
		//{
		//	if (_existingTransaction)
		//	{
		//		_dbAccess.Commit();
		//		_existingTransaction = false;
		//	}

		//	return base.ExecuteProcess(fileLocation);
		//}

		#endregion
	}
}
