#region Using Statements

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

#endregion

namespace OAS.CloudStorage.Core {
	public interface ICloudStorageClient {
		Task<bool> ValidateCredentials( Action<Exception> exceptionLogger );
		Task<MetaDataBase> GetMetaData( );
		Task<MetaDataBase> GetMetaData( string path );

		//Task<List<MetaDataBase>> GetVersions( string path, int limit );

		Task<Stream> GetFile( string path );

		Task<MetaDataBase> UploadFile( string path, byte[ ] fileData );
		Task<MetaDataBase> UploadFile( string path, Stream stream );

		Task<MetaDataBase> Delete( string path );

		Task<MetaDataBase> Copy( string fromPath, string toPath );

		Task<MetaDataBase> Move( string fromPath, string toPath );

		Task<MetaDataBase> CreateFolder( string path );

		//Task<ShareResponse> GetShare( string path, bool shortUrl = true );

		//Task<ShareResponse> GetMedia( string path );

		//Task<Stream> GetThumbnail( IMetaData file );
		//Task<Stream> GetThumbnail( IMetaData file, ThumbnailSize size );
		//Task<Stream> GetThumbnail( string path );
		//Task<Stream> GetThumbnail( string path, ThumbnailSize size );

		//Task<DeltaPage> GetDelta( string cursor );

		string BuildAuthorizeUrl( string redirectUri );

	}
}