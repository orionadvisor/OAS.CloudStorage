using System;

namespace OAS.CloudStorage.Core.Exceptions {
	public class CloudStorageAuthorizationException : CloudStorageException {
		public CloudStorageAuthorizationException( ) : base( "Authorization failed." ) { }
		public CloudStorageAuthorizationException( string message ) : base( "Authorization failed. " + message ) { }
		public CloudStorageAuthorizationException( string message, Exception ex ) : base( "Authorization failed. " + message, ex ) { }
	}
}
