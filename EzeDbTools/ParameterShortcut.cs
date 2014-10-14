using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzeDbTools
{
    internal class ParameterShortcut<T> : Argument<T>, IParameterShortcut
    {
        private readonly string _parameterName;
        private readonly ParameterMergeDirection _mergeDirection;

        public ParameterShortcut(T value, string parameterName, ParameterMergeDirection direction)
            : base(value)
        {
            _parameterName = parameterName;
            _mergeDirection = direction;
        }

        public string ParameterName
        {
            get { return _parameterName; }
        }

        public ParameterMergeDirection MergeDirection
        {
            get { return _mergeDirection; }
        }
    }
}