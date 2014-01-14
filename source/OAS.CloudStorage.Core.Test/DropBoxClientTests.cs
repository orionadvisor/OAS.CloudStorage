#region Using Statements

using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OAS.CloudStorage.DropBox.CoreApi;
using OAS.CloudStorage.DropBox.CoreApi.Models;

#endregion

namespace OAS.CloudStorage.Core.Test {
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public class DropBoxClientTests : ICloudStorageThumbnailProviderTests {
		public DropBoxClientTests( )
			: base( new DropBoxClient( TestVariables.DropBoxApiKey, TestVariables.DropBoxApiSecret, TestVariables.DropBoxToken, TestVariables.DropBoxUid ), "/Test" ) {
		}

		protected override void AssertMetaDataTypeFolder( MetaDataBase metaData ) {
			Assert.IsTrue( metaData is DropBoxFolder );
		}

		protected override void AssertMetaDataTypeFile( MetaDataBase metaData ) {
			Assert.IsTrue( metaData is DropBoxFile );
		}

		private DropBoxClient dropboxClient {
			get { return (DropBoxClient) this._client; }
		}

		[TestMethod]
		public void Search( ) {
			var result = this.dropboxClient.Search( "Getting", string.Empty ).Result;
			Assert.IsNotNull( result );
			Assert.IsTrue( result.Count > 0, "List is empty" );
		}

		[TestMethod]
		public void Can_Get_File_Range( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this.dropboxClient.UploadFile( this.TestFolder + "/" + localFile.Name, File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}


			var stream = this.dropboxClient.GetFile( path, new Range<long> {
				Start = 0,
				End = 1
			} ).Result;

			Assert.IsNotNull( stream );
			using( var file = File.OpenWrite( @"C:\Temp\привет.txt" ) ) {
				stream.CopyTo( file );
			}

			var fi = new FileInfo( @"C:\Temp\привет.txt" );
			Assert.IsTrue( fi.Length > 0 );

			try {
				var deleted = this.dropboxClient.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Create_CopyRef( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this.dropboxClient.UploadFile( this.TestFolder + "/" + string.Format( "TestFile{0:yyyyMMddhhmmss}-Create_CopyRef.txt", DateTime.Now ), File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var copyRef = this.dropboxClient.GetCopyRef( path ).Result;
			Assert.IsNotNull( copyRef );


			try {
				var deletedCopy = this.dropboxClient.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Create_File_From_CopyRef( ) {
			FileInfo localFile = null;
			string path = null;
			CopyRefResponse copyRef = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this.dropboxClient.UploadFile( this.TestFolder + "/" + string.Format( "TestFile{0:yyyyMMddhhmmss}-Copy_CopyRef.txt", DateTime.Now ), File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
				copyRef = this.dropboxClient.GetCopyRef( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var copied = this.dropboxClient.CopyFromCopyRef( copyRef.Copy_Ref, path.Replace( "TestFile", "CopyFile" ) ).Result;
			Assert.IsNotNull( copied );


			try {
				var deletedOrg = this.dropboxClient.Delete( path ).Result;
				var deletedCopy = this.dropboxClient.Delete( copied.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Shares( ) {
			string path = null;
			try {
				var localFile = FileFactory.MakeFile( );
				var uploaded = this.dropboxClient.UploadFile( this.TestFolder + "/" + localFile.Name, File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			var shareResponse = this.dropboxClient.GetShare( path ).Result;

			Assert.IsNotNull( shareResponse );
			Assert.IsNotNull( shareResponse.Url );

			try {
				var deleted = this.dropboxClient.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up file" );
			}
		}

		[TestMethod]
		public void Can_Shares_Long( ) {
			string path = null;
			try {
				var localFile = FileFactory.MakeFile( );
				var uploaded = this.dropboxClient.UploadFile( this.TestFolder + "/" + localFile.Name, File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			var shareResponse = this.dropboxClient.GetShare( path, false ).Result;

			Assert.IsNotNull( shareResponse );
			Assert.IsNotNull( shareResponse.Url );

			try {
				var deleted = this.dropboxClient.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up file" );
			}
		}

		[TestMethod]
		public void Can_Get_Media( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "mp4" );
				var uploaded = this.dropboxClient.UploadFile( this.TestFolder + "/" + string.Format( "TestFile{0:yyyyMMddhhmmss}-Media.mp4", DateTime.Now ), File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var mediaLink = this.dropboxClient.GetMedia( path ).Result;

			Assert.IsNotNull( mediaLink );
			Assert.IsNotNull( mediaLink.Expires );
			Assert.IsNotNull( mediaLink.Url );

			try {
				var deleted = this.dropboxClient.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Get_Delta( ) {
			var delta = this.dropboxClient.GetDelta( "" ).Result;

			Assert.IsNotNull( delta );
		}

		[TestMethod]
		public void Can_Chunk_Upload_File( ) {
			FileInfo localFile = null;
			try {
				localFile = FileFactory.MakeFile( length: 10485760 );
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			var data = File.ReadAllBytes( localFile.Name );
			var chunks = data.Split( 10 ).ToList( );

			var chunk = this.dropboxClient.StartChunkedUpload( chunks.First( ).ToArray( ) ).Result;
			for( var i = 1; i < chunks.Count( ); i++ ) {
				chunk = this.dropboxClient.AppendChunkedUpload( chunk, chunks[ i ].ToArray( ) ).Result;
			}
			var chunked = this.dropboxClient.CommitChunkedUpload( chunk, localFile.Name ).Result;

			Assert.IsNotNull( chunked );

			try {
				File.Delete( localFile.FullName );
				var deleted = this.dropboxClient.Delete( chunked.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}
	}
}