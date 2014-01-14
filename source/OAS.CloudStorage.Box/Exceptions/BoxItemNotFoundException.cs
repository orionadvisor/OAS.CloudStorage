using System;
using OAS.CloudStorage.Core.Exceptions;

namespace OAS.CloudStorage.Box.Exceptions {
	public class BoxItemNotFoundException : CloudStorageItemNotFoundException{
		public BoxItemNotFoundException( ) : base( "No such file or directory." ) {}

		public BoxItemNotFoundException( string message ) : base( "No such file or directory. " + message ) {}

		public BoxItemNotFoundException( string message, Exception ex ) : base( "No such file or directory. " + message, ex ) { }
	}
}
