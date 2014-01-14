using System;

namespace OAS.CloudStorage.Core.Exceptions {
	public class CloudStorageItemNotFoundException : CloudStorageException {
		public CloudStorageItemNotFoundException( ) : base( "No such file or directory." ) {}
		public CloudStorageItemNotFoundException( string message ) : base( "No such file or directory. " + message ) {}
		public CloudStorageItemNotFoundException( string message, Exception ex ) : base( "No such file or directory. " + message, ex ) {}
	}
}
