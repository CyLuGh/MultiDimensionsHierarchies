using System;

namespace MultiDimensionsHierarchies.Core
{
    public class InvalidKeySelectorException : ArgumentException
    {
        public InvalidKeySelectorException() : base()
        {
        }

        public InvalidKeySelectorException( string message ) : base( message )
        {
        }

        public InvalidKeySelectorException( string message , Exception innerException ) : base( message , innerException )
        {
        }

        public InvalidKeySelectorException( string message , string paramName ) : base( message , paramName )
        {
        }

        public InvalidKeySelectorException( string message , string paramName , Exception innerException ) : base( message , paramName , innerException )
        {
        }
    }
}