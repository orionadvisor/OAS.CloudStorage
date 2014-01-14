#region Using Statements

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OAS.CloudStorage.Egnyte;
using OAS.CloudStorage.Egnyte.Models;

#endregion

namespace OAS.CloudStorage.Core.Test {
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public class EgnyteClientTests : ICloudStorageClientTests {
		public EgnyteClientTests( )
			: base( new EgnyteClient( TestVariables.EgnyteApiDomain, TestVariables.EgnyteApiKey, TestVariables.EgnyteToken ), "/Shared/Test" ) {
		}

		protected override void AssertMetaDataTypeFolder( MetaDataBase metaData ) {
			Assert.IsTrue( metaData is EgnyteFolder );
		}

		protected override void AssertMetaDataTypeFile( MetaDataBase metaData ) {
			Assert.IsTrue( metaData is EgnyteFile );
		}

		private EgnyteClient egnyteClient {
			get { return (EgnyteClient) this._client; }
		}
	}
}