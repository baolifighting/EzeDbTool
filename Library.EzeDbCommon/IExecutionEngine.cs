
namespace Libraries.EzeDbCommon
{
    public interface IExecutionEngine
    {
        bool ExecuteSql(string content);
		//bool ExecuteCSharp(string content);
		//bool ExecuteVisualBasic(string content);
		//bool ExecuteJs(string content);
		//bool ExecuteProcess(string fileLocation);
        string GetDbVersion();
        bool DoesTableExist(string table, string column);
    }
}

