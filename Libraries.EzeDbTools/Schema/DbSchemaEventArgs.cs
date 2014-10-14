using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.EzeDbTools
{
    public class DbSchemaEventArgs : EventArgs
    {
        public bool Success;
        public int StepNumber;
        public string ModAuthor;
        public string ModDate;
        public string ModComment;
        public string ModToVersion;
        public string ModFromVersion;
        public string StepType;
        public string StepContent;

        public DbSchemaEventArgs Clone()
        {
            DbSchemaEventArgs clone = new DbSchemaEventArgs();
            clone.Success = this.Success;
            clone.StepNumber = this.StepNumber;
            clone.ModAuthor = this.ModAuthor;
            clone.ModDate = this.ModDate;
            clone.ModComment = this.ModComment;
            clone.ModToVersion = this.ModToVersion;
            clone.ModFromVersion = this.ModFromVersion;
            clone.StepType = this.StepType;
            clone.StepContent = this.StepContent;

            return clone;
        }
    }
}
