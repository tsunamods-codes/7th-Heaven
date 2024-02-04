using System;
using System.Runtime.Serialization;

namespace _7thHeaven.Code
{
    [Serializable]
    public class DuplicateModException : Exception
    {
        public DuplicateModException()
        {
        }

        public DuplicateModException(string message) : base(message)
        {
        }

        public DuplicateModException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}