namespace Libraries.EzeDbCommon.Content
{
    public class XmlColumnAlias
    {
        public readonly string DbColumnName;     
        public readonly string DbColumnName2;    
        public readonly string DbColumnEqual;    
        public readonly string DbTableEqual;     

        /// <summary>
        /// An xml column alias that refers to a column in some other table and maps to a different column in this table
        /// </summary>
        /// <param name="dbColumnName">the db column that needs to update</param>
        /// <param name="dbColumnEqual">the db column that the alias refers to</param>
        /// <param name="dbTableEqual">the table that contains dbColumnEqual</param>
        /// <param name="dbColumnName2">the db column in dbTableEqual that is the same as dbColumnName (usu. named the same too)</param>
        public XmlColumnAlias(string dbColumnName, string dbColumnEqual, string dbTableEqual, string dbColumnName2)
        {
            DbColumnName = dbColumnName;
            DbColumnEqual = dbColumnEqual;
            DbTableEqual = dbTableEqual;
            DbColumnName2 = dbColumnName2;
        }
    }
}