#region Using Statements

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

#endregion

namespace OAS.CloudStorage.Core {
	public interface ICloudStorageThumbnailProvider : ICloudStorageClient {
		Task<Stream> GetThumbnail( MetaDataBase file );
		Task<Stream> GetThumbnail( MetaDataBase file, ThumbnailSize size );
		Task<Stream> GetThumbnail( string path );
		Task<Stream> GetThumbnail( string path, ThumbnailSize size );
	}
}