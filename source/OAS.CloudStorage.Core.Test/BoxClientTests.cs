#region Using Statements

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OAS.CloudStorage.Box;
using OAS.CloudStorage.Box.Models;

#endregion

namespace OAS.CloudStorage.Core.Test {
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public class BoxClientTests : ICloudStorageThumbnailProviderTests {
		public BoxClientTests( )
			: base( new BoxClient( TestVariables.BoxApiKey, TestVariables.BoxApiSecret, TestVariables.BoxSavedSessionAuthToken, TestVariables.BoxSavedSessionRefreshToken ) {
				SaveToken = session => {
					var msg = string.Format( "Token updated:\nAccessToken: {0}\nRefreshToken: {1}", session.AccessToken, session.RefreshToken );
					Debug.WriteLine( msg );
					Assert.Fail( msg );
				}
			},
			"/Test" ) {
		}

		protected override void AssertMetaDataTypeFolder( MetaDataBase metaData ) {
			Assert.IsTrue( metaData is BoxFolderMetaData );
		}

		protected override void AssertMetaDataTypeFile( MetaDataBase metaData ) {
			Assert.IsTrue( metaData is BoxFileMetaData );
		}

		private BoxClient boxClient {
			get { return (BoxClient) this._client; }
		}
	}
}