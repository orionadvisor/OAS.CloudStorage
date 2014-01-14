#region Using Statements

using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OAS.CloudStorage.Core.Exceptions;
using OAS.CloudStorage.DropBox.CoreApi;
using OAS.CloudStorage.DropBox.CoreApi.Models;

#endregion

namespace OAS.CloudStorage.Core.Test {
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public abstract class ICloudStorageThumbnailProviderTests : ICloudStorageClientTests {
		protected ICloudStorageThumbnailProviderTests( ICloudStorageClient client, string testFolder )
			: base( client, testFolder ) {
		}

		private ICloudStorageThumbnailProvider ICloudStorageThumbnailProviderClient {
			get { return (ICloudStorageThumbnailProvider) this._client; }
		}

		[TestMethod]
		public void Can_Get_Thumbnail( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeImage( ImageFormat.Png );
				var uploaded = this.ICloudStorageThumbnailProviderClient.UploadFile( this.TestFolder + "/" + string.Format( "TestFile{0:yyyyMMddhhmmss}-Thumbnail.png", DateTime.Now ), File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var stream = this.ICloudStorageThumbnailProviderClient.GetThumbnail( path ).Result;

			Assert.IsNotNull( stream );

			using( var file = File.OpenWrite( @"C:\Temp\test.png" ) ) {
				stream.CopyTo( file );
			}

			var fi = new FileInfo( @"C:\Temp\test.png" );
			Assert.IsTrue( fi.Length > 0 );

			try {
				var deleted = this.ICloudStorageThumbnailProviderClient.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Get_Thumbnail_Small( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeImage( ImageFormat.Png );
				var uploaded = this.ICloudStorageThumbnailProviderClient.UploadFile( this.TestFolder + "/" + string.Format( "TestFile{0:yyyyMMddhhmmss}-Thumbnail.png", DateTime.Now ), File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var stream = this.ICloudStorageThumbnailProviderClient.GetThumbnail( path, ThumbnailSize.Small ).Result;

			Assert.IsNotNull( stream );

			using( var file = File.OpenWrite( @"C:\Temp\test.png" ) ) {
				stream.CopyTo( file );
			}

			var fi = new FileInfo( @"C:\Temp\test.png" );
			Assert.IsTrue( fi.Length > 0 );

			try {
				var deleted = this.ICloudStorageThumbnailProviderClient.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Get_Thumbnail_ExtraLarge( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeImage( ImageFormat.Png );
				var uploaded = this.ICloudStorageThumbnailProviderClient.UploadFile( this.TestFolder + "/" + string.Format( "TestFile{0:yyyyMMddhhmmss}-Thumbnail.png", DateTime.Now ), File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var stream = this.ICloudStorageThumbnailProviderClient.GetThumbnail( path, ThumbnailSize.ExtraLarge ).Result;

			Assert.IsNotNull( stream );

			using( var file = File.OpenWrite( @"C:\Temp\test.png" ) ) {
				stream.CopyTo( file );
			}

			var fi = new FileInfo( @"C:\Temp\test.png" );
			Assert.IsTrue( fi.Length > 0 );

			try {
				var deleted = this.ICloudStorageThumbnailProviderClient.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Cannot_Get_Thumbnail_that_doesnt_Exist( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				path = this.TestFolder + "/" + localFile.Name;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			try {
				var stream = this.ICloudStorageThumbnailProviderClient.GetThumbnail( path ).Result;
				Assert.Fail( "Get should have thrown exception" );
			} catch( AggregateException ae ) {
				Assert.AreEqual( 1, ae.InnerExceptions.Count, "Wrong number of errors returend." );
				Assert.IsTrue( ae.InnerException is CloudStorageItemNotFoundException, "Did not throw expected exception" );
			} catch( Exception ) {
				Assert.Fail( );
			}
		}
	}
}