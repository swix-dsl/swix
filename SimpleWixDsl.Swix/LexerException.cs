using System;
using System.Runtime.Serialization;

namespace SimpleWixDsl.Swix
{
    [Serializable]
    public class LexerException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public LexerException()
        {
        }

        public LexerException(string message) : base(message)
        {
        }

        public LexerException(string message, Exception inner) : base(message, inner)
        {
        }

        protected LexerException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}