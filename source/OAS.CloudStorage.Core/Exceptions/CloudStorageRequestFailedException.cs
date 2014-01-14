using System;

namespace OAS.CloudStorage.Core.Exceptions {
	public class CloudStorageRequestFailedException : CloudStorageException {
		public CloudStorageRequestFailedException( ) : base( "Request failed." ) { }
		public CloudStorageRequestFailedException( string message ) : base( "Request failed. " + message ) { }
		public CloudStorageRequestFailedException( string message, Exception ex ) : base( "Request failed. " + message, ex ) { }
	}
}
