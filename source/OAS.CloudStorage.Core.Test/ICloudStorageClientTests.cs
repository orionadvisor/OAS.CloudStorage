#region Using Statements

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OAS.CloudStorage.Box;
using OAS.CloudStorage.Core.Exceptions;

#endregion

namespace OAS.CloudStorage.Core.Test {
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	public abstract class ICloudStorageClientTests {
		protected readonly ICloudStorageClient _client;
		protected readonly string TestFolder = "/Test";

		protected ICloudStorageClientTests( ICloudStorageClient client, string testFolder ) {
			this._client = client;
			this.TestFolder = testFolder;
		}

		protected abstract void AssertMetaDataTypeFolder( MetaDataBase metaData );
		protected abstract void AssertMetaDataTypeFile( MetaDataBase metaData );

		[TestMethod]
		public void Can_Get_File( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/" + localFile.Name, File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var stream = this._client.GetFile( path ).Result;
			Assert.IsNotNull( stream );

			try {
				var deleted = this._client.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Cannot_Get_File_that_doesnt_Exist( ) {
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
				var stream = this._client.GetFile( path ).Result;
				Assert.Fail( "Get should have thrown exception" );
			} catch( AggregateException ae ) {
				Assert.AreEqual( 1, ae.InnerExceptions.Count, "Wrong number of errors returend." );
				Assert.IsTrue( ae.InnerException is CloudStorageItemNotFoundException, "Did not throw expected exception" );
			} catch( Exception ) {
				Assert.Fail( );
			}
		}

		[TestMethod]
		public void Can_Get_File_And_Save( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/" + localFile.Name, File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}


			var stream = this._client.GetFile( path ).Result;

			Assert.IsNotNull( stream );
			using( var file = File.OpenWrite( @"C:\Temp\" + localFile.Name ) ) {
				stream.CopyTo( file );
			}

			var fi = new FileInfo( @"C:\Temp\" + localFile.Name );
			Assert.IsTrue( fi.Length > 0 );

			try {
				var deleted = this._client.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Get_File_Foreign_Language( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/" + "привет.txt", File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}


			var stream = this._client.GetFile( path ).Result;

			Assert.IsNotNull( stream );
			using( var file = File.OpenWrite( @"C:\Temp\привет.txt" ) ) {
				stream.CopyTo( file );
			}

			var fi = new FileInfo( @"C:\Temp\привет.txt" );
			Assert.IsTrue( fi.Length > 0 );

			try {
				var deleted = this._client.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Upload_File_Bytes( ) {
			FileInfo localFile = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			var uploaded = this._client.UploadFile( this.TestFolder + "/" + localFile.Name, File.ReadAllBytes( localFile.Name ) ).Result;

			Assert.IsNotNull( uploaded );
			Assert.AreEqual( this.TestFolder + "/" + localFile.Name, uploaded.Path, "File not uploaded to correct location" );

			try {
				File.Delete( localFile.FullName );
				var deleted = this._client.Delete( uploaded.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Upload_File_Stream( ) {
			FileInfo localFile = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			var uploaded = this._client.UploadFile( this.TestFolder + "/" + localFile.Name, File.OpenRead( localFile.Name ) ).Result;

			Assert.IsNotNull( uploaded );
			Assert.AreEqual( this.TestFolder + "/" + localFile.Name, uploaded.Path, "File not uploaded to correct location" );

			try {
				File.Delete( localFile.FullName );
				var deleted = this._client.Delete( uploaded.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Upload_File_To_New_Folder( ) {
			FileInfo localFile = null;
			string newFolderName = Guid.NewGuid( ).ToString( );
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			var uploaded = this._client.UploadFile( string.Format( "{0}/{1}/{2}", this.TestFolder, newFolderName, localFile.Name ), File.OpenRead( localFile.Name ) ).Result;

			Assert.IsNotNull( uploaded );
			Assert.AreEqual( string.Format( "{0}/{1}/{2}", this.TestFolder, newFolderName, localFile.Name ), uploaded.Path, "File not uploaded to correct location" );

			try {
				File.Delete( localFile.FullName );
				var deleted = this._client.Delete( string.Format( "{0}/{1}", this.TestFolder, newFolderName ) ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Upload_File_With_Special_Char( ) {
			FileInfo localFile = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			var uploaded = this._client.UploadFile( this.TestFolder + "/test&file's.txt", File.OpenRead( localFile.Name ) ).Result;

			Assert.IsNotNull( uploaded );
			Assert.AreEqual( this.TestFolder + "/test&file's.txt", uploaded.Path, "File not uploaded to correct location" );

			try {
				File.Delete( localFile.FullName );
				var deleted = this._client.Delete( uploaded.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Upload_File_With_Space( ) {
			FileInfo localFile = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			var uploaded = this._client.UploadFile( this.TestFolder + "/" + "test file.txt", File.OpenRead( localFile.Name ) ).Result;

			Assert.IsNotNull( uploaded );
			Assert.AreEqual( this.TestFolder + "/test file.txt", uploaded.Path, "File not uploaded to correct location" );

			try {
				File.Delete( localFile.FullName );
				var deleted = this._client.Delete( uploaded.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Upload_File_With_International_Char( ) {
			FileInfo localFile = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			var uploaded = this._client.UploadFile( this.TestFolder + "/testПр.txt", File.ReadAllBytes( localFile.Name ) ).Result;

			Assert.IsNotNull( uploaded );
			Assert.AreEqual( this.TestFolder + "/testПр.txt", uploaded.Path, "File not uploaded to correct location" );

			try {
				File.Delete( localFile.FullName );
				var deleted = this._client.Delete( uploaded.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Upload_1MB_File( ) {
			FileInfo localFile = null;
			try {
				localFile = FileFactory.MakeFile( length: 1048576 );
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			var uploaded = this._client.UploadFile( this.TestFolder + "/" + localFile.Name, File.OpenRead( localFile.Name ) ).Result;

			Assert.IsNotNull( uploaded );
			Assert.AreEqual( this.TestFolder + "/" + localFile.Name, uploaded.Path, "File not uploaded to correct location" );

			try {
				File.Delete( localFile.FullName );
				var deleted = this._client.Delete( uploaded.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}
		
		[TestMethod]
		public void Can_Upload_File_Over_Existing_File( ) {
			FileInfo localFile1 = null;
			FileInfo localFile2 = null;
			string path = null;
			try {
				localFile1 = FileFactory.MakeFile( extension: "txt" );
				var firstUpload = this._client.UploadFile( this.TestFolder + "/" + localFile1.Name, File.OpenRead( localFile1.Name ) ).Result;
				path = firstUpload.Path;

				localFile2 = FileFactory.MakeFile( extension: "txt" );
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile1.FullName );
			}

			var uploaded = this._client.UploadFile( path, File.OpenRead( localFile2.Name ) ).Result;

			Assert.IsNotNull( uploaded );
			Assert.AreEqual( path, uploaded.Path, "File not uploaded to correct location" );

			try {
				File.Delete( localFile2.FullName );
				var deleted = this._client.Delete( uploaded.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Delete_File( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/" + localFile.Name, File.ReadAllBytes( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var deleted = this._client.Delete( path ).Result;
			Assert.IsNotNull( deleted );
		}

		[TestMethod]
		public void Cannot_Delete_File_that_doesnt_Exist( ) {
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
				var deleted = this._client.Delete( path ).Result;
				Assert.Fail( "Delete should have thrown exception" );
			} catch( AggregateException ae ) {
				Assert.AreEqual( 1, ae.InnerExceptions.Count, "Wrong number of errors returend." );
				Assert.IsTrue( ae.InnerException is CloudStorageItemNotFoundException, "Did not throw expected exception" );
			} catch( Exception ) {
				Assert.Fail( );
			}
		}

		[TestMethod]
		public void Can_Delete_File_With_Special_Char( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/" + "test&file's.txt", File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var deleted = this._client.Delete( path ).Result;
			Assert.IsNotNull( deleted );
		}

		[TestMethod]
		public void Can_Copy_Folder( ) {
			string path = null;
			try {
				var metaData = this._client.CreateFolder( string.Format( this.TestFolder + "/TestFolder1{0:yyyyMMddhhmmss}", DateTime.Now ) ).Result;
				path = metaData.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			var copied = this._client.Copy( path, path.Replace( "TestFolder", "CopyFolder" ) ).Result;
			Assert.IsNotNull( copied );
			Assert.AreEqual( path.Replace( "TestFolder", "CopyFolder" ), copied.Path, "Folder not uploaded to correct location" );

			try {
				var deletedOrg = this._client.Delete( path ).Result;
				var deletedCopy = this._client.Delete( copied.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Copy_File( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/" + string.Format( "TestFile{0:yyyyMMddhhmmss}-Copy_File.txt", DateTime.Now ), File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var copied = this._client.Copy( path, path.Replace( "TestFile", "CopyFile" ) ).Result;
			Assert.IsNotNull( copied );
			Assert.AreEqual( path.Replace( "TestFile", "CopyFile" ), copied.Path, "File not uploaded to correct location" );

			try {
				var deletedOrg = this._client.Delete( path ).Result;
				var deletedCopy = this._client.Delete( copied.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Copy_File_With_Space( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/" + string.Format( "TestFile{0:yyyyMMddhhmmss}-Copy File.txt", DateTime.Now ), File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var copied = this._client.Copy( path, path.Replace( "TestFile", "CopyFile" ) ).Result;
			Assert.IsNotNull( copied );
			Assert.AreEqual( path.Replace( "TestFile", "CopyFile" ), copied.Path, "File not uploaded to correct location" );

			try {
				var deletedOrg = this._client.Delete( path ).Result;
				var deletedCopy = this._client.Delete( copied.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Cannot_Copy_File_that_doesnt_Exist( ) {
			string path = this.TestFolder + "/" + string.Format( "TestFile{0:yyyyMMddhhmmss}-Copy_File.txt", DateTime.Now );

			try {
				var copied = this._client.Copy( path, path.Replace( "TestFile", "CopyFile" ) ).Result;
				Assert.Fail( "Copy should have thrown exception" );
			} catch( AggregateException ae ) {
				ae = ae.Flatten( );
				Assert.AreEqual( 1, ae.InnerExceptions.Count, "Wrong number of errors returend." );
				Assert.IsTrue( ae.InnerException is CloudStorageItemNotFoundException, "Did not throw expected exception" );
			} catch( Exception ) {
				Assert.Fail( );
			}
		}

		[TestMethod]
		public void Can_Make_Many_Requests( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/" + localFile.Name, File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			for( var i = 0; i < 10; i++ ) {
				var metaData = this._client.GetMetaData( path ).Result;
				Assert.IsNotNull( metaData );
			}

			try {
				var deleted = this._client.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Move_File( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/" + string.Format( "TestFile{0:yyyyMMddhhmmss}-Move_File.txt", DateTime.Now ), File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var moved = this._client.Move( path, path.Replace( "TestFile", "MovedFile" ) ).Result;
			Assert.IsNotNull( moved );

			try {
				var deletedCopy = this._client.Delete( moved.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Move_Folder( ) {
			string path = null;
			try {
				var uploaded = this._client.CreateFolder( this.TestFolder + "/" + string.Format( "TestFolder{0:yyyyMMddhhmmss}-Move_Folder", DateTime.Now ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make folder" );
			}

			var moved = this._client.Move( path, path.Replace( "TestFolder", "Movedolder" ) ).Result;
			Assert.IsNotNull( moved );

			try {
				var deletedCopy = this._client.Delete( moved.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Cannot_Move_File_that_doesnt_Exist( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				path = this.TestFolder + "/" + string.Format( "TestFile{0:yyyyMMddhhmmss}-Move_File.txt", DateTime.Now );
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			try {
				var moved = this._client.Move( path, path.Replace( "TestFile", "MovedFile" ) ).Result;
				Assert.Fail( "Move should have thrown exception" );
			} catch( AggregateException ae ) {
				ae = ae.Flatten( );
				Assert.AreEqual( 1, ae.InnerExceptions.Count, "Wrong number of errors returend." );
				Assert.IsTrue( ae.InnerException is CloudStorageItemNotFoundException, "Did not throw expected exception" );
			} catch( Exception ) {
				Assert.Fail( );
			}
		}

		[TestMethod]
		public void Can_Get_MetaData_Folder( ) {
			var path = TestFolder;
			var metaData = this._client.GetMetaData( path ).Result;

			this.AssertIsFolder( metaData, path );
		}

		[TestMethod]
		public void Cannot_Get_MetaData_that_doesnt_Exist( ) {
			string path = null;
			var newFolder = Guid.NewGuid( ).ToString( );
			try {
				path = string.Format( "{0}/{1}", this.TestFolder, newFolder );
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			}

			try {
				var metaData = this._client.GetMetaData( path ).Result;
				Assert.Fail( "Metadata should have thrown exception" );
			} catch( AggregateException ae ) {
				Assert.AreEqual( 1, ae.InnerExceptions.Count, "Wrong number of errors returend." );
				Assert.IsTrue( ae.InnerException is CloudStorageItemNotFoundException, "Did not throw expected exception" );
			} catch( Exception ) {
				Assert.Fail( );
			}
		}

		[TestMethod]
		public void Can_Get_MetaData_Root( ) {
			var metaData = this._client.GetMetaData( ).Result;

			this.AssertIsFolder( metaData, "/" );
		}

		[TestMethod]
		public void Can_Get_MetaData_Null( ) {
			var metaData = this._client.GetMetaData( null ).Result;

			this.AssertIsFolder( metaData, "/" );
		}

		[TestMethod]
		public void Can_Get_MetaData_Empty( ) {
			var metaData = this._client.GetMetaData( string.Empty ).Result;

			this.AssertIsFolder( metaData, "/" );
		}

		[TestMethod]
		public void Can_Get_MetaData_File( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/" + localFile.Name, File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var metaData = this._client.GetMetaData( path ).Result;
			this.AssertIsFile( metaData, path );

			try {
				var deleted = this._client.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Get_MetaData_File_With_Space( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/new file.txt", File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var metaData = this._client.GetMetaData( path ).Result;
			this.AssertIsFile( metaData, path );

			try {
				var deleted = this._client.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Get_MetaData_File_With_Special_Char( ) {
			FileInfo localFile = null;
			string path = null;
			try {
				localFile = FileFactory.MakeFile( extension: "txt" );
				var uploaded = this._client.UploadFile( this.TestFolder + "/&Getting'Started.rtf", File.OpenRead( localFile.Name ) ).Result;
				path = uploaded.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make file" );
			} finally {
				File.Delete( localFile.FullName );
			}

			var metaData = this._client.GetMetaData( path ).Result;
			this.AssertIsFile( metaData, path );

			try {
				var deleted = this._client.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up files" );
			}
		}

		[TestMethod]
		public void Can_Create_Folder( ) {
			var metaData = this._client.CreateFolder( string.Format( this.TestFolder + "/TestFolder1{0:yyyyMMddhhmmss}", DateTime.Now ) ).Result;

			Assert.IsNotNull( metaData );

			try {
				var deleted = this._client.Delete( metaData.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up folder" );
			}
		}

		[TestMethod]
		public void Can_Create_Folder_Tree( ) {
			var metaData = this._client.CreateFolder( string.Format( "{0}/{1}/{2}", this.TestFolder, Guid.NewGuid( ).ToString( ), Guid.NewGuid( ).ToString( ) ) ).Result;

			Assert.IsNotNull( metaData );

			try {
				var deleted = this._client.Delete( metaData.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up folder" );
			}
		}

		[TestMethod]
		public void Can_Navigate_Inside_Folders( ) {
			string path = this.TestFolder;

			for( int i = 0; i < 10; ++i ) {
				path += "/" + Guid.NewGuid( );
			}

			var metaData = this._client.CreateFolder( path ).Result;
			FolderMetaDataBase currentItem;
			int currentIndex;
			path = path.Trim( '/' );
			var pathItems = path.Split( '/' );

			for( currentIndex = 0, currentItem = (FolderMetaDataBase) this._client.GetMetaData( ).Result, path = "/"; currentIndex < pathItems.Length; ++currentIndex ) {
				this.AssertIsFolder( currentItem, path );
				currentItem = currentItem.Folders.FirstOrDefault( x => x.Name == pathItems[ currentIndex ] );

				Assert.IsNotNull( currentItem, "Folder " + pathItems[ currentIndex ] + " was not found in " + path );
				path += currentItem.Name + "/";
				currentItem = (FolderMetaDataBase) this._client.GetMetaData( path ).Result;
			}
		}

		[TestMethod]
		public void Cannot_Create_Folder_that_exists( ) {
			var newFolder = Guid.NewGuid( ).ToString( );
			string path = null;
			try {
				var metaData = this._client.CreateFolder( string.Format( "{0}/{1}", this.TestFolder, newFolder ) ).Result;
				path = metaData.Path;
			} catch {
				Assert.Inconclusive( "Couldn't make folder" );
			}

			try {
				var metaData = this._client.CreateFolder( string.Format( "{0}/{1}", this.TestFolder, newFolder ) ).Result;
				Assert.Fail( "Create Folder should have thrown exception" );
			} catch( AggregateException ae ) {
				Assert.AreEqual( 1, ae.InnerExceptions.Count, "Wrong number of errors returend." );
				Assert.IsTrue( ae.InnerException is CloudStorageRequestFailedException, "Did not throw expected exception" );
				var csRequestException = ae.InnerException as CloudStorageRequestFailedException;
				Assert.AreEqual( "Request failed. Could not create folder.", csRequestException.Message, "error message was not what was expected" );
			} catch( Exception ) {
				Assert.Fail( );
			}

			try {
				var deleted = this._client.Delete( path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up folder" );
			}
		}

		[TestMethod]
		public void Can_Create_Folder_With_Special_Character( ) {
			var metaData = this._client.CreateFolder( string.Format( this.TestFolder + "/Test?Folder1{0:yyyyMMddhhmmss}", DateTime.Now ) ).Result;

			Assert.IsNotNull( metaData );

			try {
				var deleted = this._client.Delete( metaData.Path ).Result;
			} catch {
				Assert.Inconclusive( "Couldn't clean up folder" );
			}
		}

		#region Private utility methods
		private void AssertIsFile( MetaDataBase file, string expectedPath ) {
			if( expectedPath != "/" && expectedPath.EndsWith( "/" ) ) {
				expectedPath = expectedPath.Substring( 0, expectedPath.Length - 1 );
			}

			Assert.IsNotNull( file );
			Assert.IsFalse( file.IsFolder, "Expected to be a file" );
			Assert.AreEqual( expectedPath.Split( '/' ).Last( ), file.Name, "Wrong file name." );
			Assert.AreEqual( expectedPath, file.Path, "Wrong file path" );
			Assert.IsTrue( file is FileMetaDataBase, "Wrong object type returned" );
			this.AssertMetaDataTypeFile( file );
		}

		private void AssertIsFolder( MetaDataBase file, string expectedPath ) {
			if( expectedPath != "/" && expectedPath.EndsWith( "/" ) ) {
				expectedPath = expectedPath.Substring( 0, expectedPath.Length - 1 );
			}

			Assert.IsNotNull( file );
			Assert.IsTrue( file.IsFolder, "Expected to be a folder" );
			Assert.AreEqual( expectedPath.Split( '/' ).Last( ), file.Name, "Wrong folder name." );
			Assert.AreEqual( expectedPath, file.Path, "Wrong file path" );
			Assert.IsTrue( file is FolderMetaDataBase, "Wrong object type returned" );
			this.AssertMetaDataTypeFolder( file );
		}
		#endregion
	}
}