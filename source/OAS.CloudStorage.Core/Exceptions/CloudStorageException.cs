using System;

namespace OAS.CloudStorage.Core.Exceptions {
	public class CloudStorageException : Exception {
		public CloudStorageException( ) : base( ) { }
		public CloudStorageException( string message ) : base( message ) { }
		public CloudStorageException( string message, Exception ex ) : base( message, ex ) { }
	}
}
