using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzeDbTools
{
    public class Argument<T> : IArgument
    {
        #region Variables

        #endregion Variables

        #region Lifetime

        public Argument()
        {
            Value = default(T);
        }

        public Argument(T value)
        {
            Value = value;
        }

        #endregion Lifetime

        #region Properties

        public T Value { get; set; }

        #endregion Properties

        #region Operators

        public static implicit operator Argument<T>(T value)
        {
            return new Argument<T>(value);
        }

        public static implicit operator T(Argument<T> value)
        {
            return value == null ? default(T) : value.Value;
        }

        #endregion Operators

        #region IArgument Members

        Type IArgument.ArgumentType
        {
            get { return typeof(T); }
        }

        object IArgument.Value
        {
            get { return Value; }
            set { Value = (T)value; }
        }

        #endregion
    }
}