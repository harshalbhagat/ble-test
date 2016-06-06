using System;
// ReSharper disable UnusedMember.Global

namespace BTLE.Exceptions
    {
    public class JawboneException : Exception
        {
        public JawboneException( JawboneErrorCodes errorCode )
            {
            ErrorCode = errorCode;
            }

        private JawboneException( string message ) : base( message )
            {
            }

        public JawboneException( JawboneErrorCodes errorCode, string message ) : this( message )
            {
            ErrorCode = errorCode;
            }

        private JawboneErrorCodes ErrorCode
            {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            get; set;
            }
        }
    }