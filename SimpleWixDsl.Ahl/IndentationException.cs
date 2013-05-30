using System;
using System.Runtime.Serialization;

namespace SimpleWixDsl.Ahl
{
    [Serializable]
    public class IndentationException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public IndentationException()
        {
        }

        public IndentationException(string message) : base(message)
        {
        }

        public IndentationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected IndentationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}