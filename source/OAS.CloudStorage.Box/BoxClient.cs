#region Using Statements

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using OAS.CloudStorage.Core;
using OAS.CloudStorage.Core.Exceptions;
using OAS.CloudStorage.Box.Models;
using OAS.CloudStorage.Box.Exceptions;

#endregion

namespace OAS.CloudStorage.Box {
	public class BoxClient : ICloudStorageThumbnailProvider {
		#region Public Properties

		/// <summary>
		/// Contains the Users Token and Secret
		/// </summary>
		public BoxUser UserLogin {
			get { return this._userLogin; }
			set {
				this._userLogin = value;
				this.SetAuthProviders( );
			}
		}

		#endregion
		private const string ApiBaseUrl = "https://api.box.com";
		private const string ApiAuthUrl = "https://www.box.com/api/oauth2";
		private const string ApiUploadUrl = "https://upload.box.com/api";
		private const string Version = "2.0";

		private readonly string _apiKey;
		private readonly string _appsecret;
		public Action<BoxUser> SaveToken;

		private BoxUser _userLogin;
		private HttpClient _client;

		private Dictionary<string, FileInfo> cached = new Dictionary<string, FileInfo>( );
		private enum BoxItemType {
			File,
			Folder,
			Unknown
		};

		private class FileInfo {
			public string Id { get; set; }
			public bool IsFolder { get; set; }
		}

		/// <summary>
		/// Box constructor
		/// </summary>
		/// <param name="apiKey">The Api Key to use for the Dropbox Requests</param>
		/// <param name="appSecret">The Api Secret to use for the Dropbox Requests</param>
		public BoxClient( string apiKey, string appSecret ) {
			Guard.NotNullOrEmpty( ( ) => apiKey, apiKey );
			Guard.NotNullOrEmpty( ( ) => appSecret, appSecret );

			this.LoadClient( );
			this._apiKey = apiKey;
			this._appsecret = appSecret;
			this.UserLogin = null;
		}

		/// <summary>
		/// Box constructor to resume authenticated session for the client
		/// </summary>
		/// <param name="apiKey">The Api Key to use for the Dropbox Requests</param>
		/// <param name="appSecret">The Api Secret to use for the Dropbox Requests</param>
		/// <param name="accessToken">The OAuth2 access token</param>
		/// <param name="refreshToken">The OAuth2 refresh token</param>
		/// <remarks>Session is valid for on hour</remarks>
		public BoxClient( string apiKey, string appSecret, string accessToken, string refreshToken )
			: this( apiKey, appSecret ) {
			this.UserLogin = new BoxUser( accessToken, refreshToken );
		}

		private void LoadClient( ) {
			var handler = new RefreshTokenOnUnauthorizeHandler( ( ) => {
				var newToken = this.refreshToken( );
				this.UserLogin = newToken;
				if( SaveToken != null ) {
					SaveToken.Invoke( newToken );
				}

				return this._client.DefaultRequestHeaders.Authorization;
			} );
			this._client = new HttpClient( handler );
		}

		private void SetAuthProviders( ) {
			if( this.UserLogin == null ) {
				this._client.DefaultRequestHeaders.Authorization = null;
			} else {
				this._client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", this.UserLogin.AccessToken );
			}
		}

		private BoxUser refreshToken( ) {
			var httpClient = new HttpClient( );
			httpClient.DefaultRequestHeaders.Authorization = null;
			var content = new FormUrlEncodedContent( new[ ] {
					new KeyValuePair<string, string>( "grant_type", "refresh_token" ),
					new KeyValuePair<string, string>( "refresh_token", this.UserLogin.RefreshToken ),
					new KeyValuePair<string, string>( "client_id", this._apiKey ),
					new KeyValuePair<string, string>( "client_secret", this._appsecret )
				} );
			var response = httpClient.PostAsync( string.Format( "{0}/token", ApiAuthUrl ), content ).Result;

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				default:
					throw new CloudStorageAuthorizationException( "Unable to refresh token. " + ( ( response.Content != null ) ? response.Content.ReadAsStringAsync( ).Result : string.Empty ) );
			}

			var result = response.Content.ReadAsAsync<BoxRefreshTokenResultInternal>( ).Result;

			return new BoxUser( result.access_token, result.refresh_token );
		}

		/// <summary>
		/// Authenticate token received when user granted app access to Box account
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task<BoxUser> AuthenticateClientToken( string token ) {
			var httpClient = new HttpClient( );
			httpClient.DefaultRequestHeaders.Authorization = null;
			var content = new FormUrlEncodedContent( new[ ] {
				new KeyValuePair<string, string>( "grant_type", "authorization_code" ),
				new KeyValuePair<string, string>( "code", token ),
				new KeyValuePair<string, string>( "client_id", this._apiKey ),
				new KeyValuePair<string, string>( "client_secret", this._appsecret )
			} );
			var response = httpClient.PostAsync( string.Format( "{0}/token", ApiAuthUrl ), content ).Result;

			if( response.StatusCode != HttpStatusCode.OK ) {
				throw new CloudStorageAuthorizationException( "Unable to obtain authorization token. " + ( ( response.Content != null ) ? response.Content.ReadAsStringAsync( ).Result : string.Empty ) );
			}

			var result = await response.Content.ReadAsAsync<BoxRefreshTokenResultInternal>( );

			var newSession = new BoxUser( result.access_token, result.refresh_token );

			return newSession;
		}

		/// <summary>
		/// This starts the OAuth 2.0 authorization flow. This isn't an API call—it's the web page that lets the user sign in to Egnyte and authorize your app. The user must be redirected to the page over HTTPS and it should be presented to the user through their web browser. After the user decides whether or not to authorize your app, they will be redirected to the URL specified by the 'redirectUri'.
		/// </summary>
		/// <param name="redirectUri">Where to redirect the user after authorization has completed. A redirect URI is required for a token flow.</param>
		/// <returns>A URL to which your app should redirect the user for authorization.  After the user authorizes your app, they will be sent to your redirect URI. The type of response varies based on the 'oauth2AuthorizationFlow' argument.  .</returns>
		public string BuildAuthorizeUrl( string redirectUri ) {
			return string.Format( "{0}/authorize?response_type=code&client_id={1}&state=authenticated&redirect_uri={2}",
				ApiAuthUrl,
				this._apiKey,
				System.Net.WebUtility.UrlEncode( redirectUri )
			);
		}

		/// <summary>
		/// Returns true if the user credentials are valid and false if they are invalid.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> ValidateCredentials( Action<Exception> exceptionLogger ) {
			try {
				var md = await this.GetMetaData( null );
				return true;
			} catch( Exception ex ) {
				if( exceptionLogger != null ) {
					exceptionLogger.Invoke( ex );
				}
				return false;
			}
		}

		/// <summary>
		/// Get information for file at provided path. This function allows to create all the substructure for items that does not exist.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="errorIfNotFound"></param>
		/// <returns></returns>
		public async Task<BoxFileMetaData> GetFileMetaDataByPath( string path, bool errorIfNotFound ) {
			var result = await this.getItemByPath( path, BoxItemType.File, errorIfNotFound );

			if( result.Type.ToLowerInvariant( ) == "file" ) {
				return new BoxFileMetaData( result );
			} else {
				throw new BoxItemNotFoundException( "Item is not a file." );
			}
		}

		private async Task<BoxFolderInternal> GetFolderInformationAsync( string id ) {
			var response = await _client.GetAsync( string.Format( "{0}/{1}/folders/{2}", ApiBaseUrl, Version, id ) );
			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( "Could not find folder." );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}
			var requestedFolder = await response.Content.ReadAsAsync<BoxFolderInternal>( );
			return requestedFolder;
		}

		private async Task<ItemCollectionInternal> GetFolderItemsAsync( string id, int limit, int offset ) {
			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "limit", limit.ToString() ),
				new KeyValuePair<string, string>( "offset", offset.ToString() ),
			};
			var response = await _client.GetAsync( string.Format( "{0}/{1}/folders/{2}/items{3}", ApiBaseUrl, Version, id,
					queryParams.ToQueryString( )
			) );

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( "Could not find folder." );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}
			var requestedFolderItems = await response.Content.ReadAsAsync<ItemCollectionInternal>( );
			return requestedFolderItems;
		}

		private async Task<BoxFileInternal> GetFileInformationAsync( string id ) {
			var response = await _client.GetAsync( string.Format( "{0}/{1}/files/{2}", ApiBaseUrl, Version, id ) );
			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( "Could not find file." );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}
			var requestedFile = await response.Content.ReadAsAsync<BoxFileInternal>( );
			return requestedFile;
		}

		/// <summary>
		/// Get information about root folder
		/// </summary>
		/// <returns></returns>
		public async Task<MetaDataBase> GetMetaData( ) {
			return new BoxFolderMetaData( await this.GetFolderInformationAsync( "0" ) );
		}

		/// <summary>
		/// Get information about an item
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public async Task<MetaDataBase> GetMetaData( string path ) {
			if( string.IsNullOrEmpty( path ) || ( path == "/" ) ) {
				return await this.GetMetaData( );
			}

			var result = await this.getItemByPath( path, BoxItemType.Unknown, true );

			if( result.Type.ToLowerInvariant( ) == "folder" ) {
				if( result.Item_Collection.Total_Count != result.Item_Collection.Entries.Count ) {
					List<BoxItemInternal> contents = await this.getAllItemsForFolder( result.Id, result.Item_Collection.Entries.Count );
					result.Item_Collection.Entries.AddRange( contents );
				}
				return new BoxFolderMetaData( result );
			} else if( result.Type.ToLowerInvariant( ) == "file" ) {
				var versions = this.getVersions( (BoxFileInternal) result ).Result;
				return new BoxFileMetaData( result, versions );
			} else {
				throw new BoxItemNotFoundException( "Item is not a file or a folder." );
			}
		}

		private async Task<ItemCollectionInternal> getVersions( BoxFileInternal fileEntry ) {
			var response = await _client.GetAsync( string.Format( "{0}/{1}/files/{2}/versions", ApiBaseUrl, Version, fileEntry.Id ) );
			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.Unauthorized:
					var msg = await response.Content.ReadAsStringAsync( );
					throw new CloudStorageAuthorizationException( msg );
				case HttpStatusCode.Forbidden:
					return null;
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}
			return await response.Content.ReadAsAsync<ItemCollectionInternal>( );
		}

		public async Task<Stream> GetFile( string path ) {
			var fileEntry = await this.getFileByPath( path, true );

			var response = await _client.GetAsync( string.Format( "{0}/{1}/files/{2}/content", ApiBaseUrl, Version, fileEntry.Id ) );
			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.Unauthorized:
					var msg = await response.Content.ReadAsStringAsync( );
					throw new CloudStorageAuthorizationException( msg );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}
			return await response.Content.ReadAsStreamAsync( );
		}

		public async Task<MetaDataBase> UploadFile( string path, byte[ ] fileData ) {
			return await uploadFile( path, new ByteArrayContent( fileData ) );
		}

		public async Task<MetaDataBase> UploadFile( string path, Stream stream ) {
			return await uploadFile( path, new StreamContent( stream ) );
		}

		private async Task<MetaDataBase> uploadFile( string path, HttpContent dataContent ) {
			if( !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var parts = extractParentDirectoryAndItemNameFromFullPath( path );

			var targetFolder = await this.getFolderByPath( parts.Item1, false );

			var fileEntry = targetFolder.Item_Collection.Entries.FirstOrDefault( e => e.Name == parts.Item2 );
			HttpResponseMessage response;

			if( fileEntry != null ) {
				//file already exists and needs to be updated
				var multipartContent = new MultipartFormDataContent( );
				multipartContent.Headers.TryAddWithoutValidation( "If-Match", fileEntry.ETag );

				dataContent.Headers.ContentDisposition = new ContentDispositionHeaderValue( "form-data" ) {
					Name = "\"file\"",
					FileName = string.Format( "\"{0}\"", parts.Item2 )
				};
				dataContent.Headers.ContentType = MediaTypeHeaderValue.Parse( "application/octet-stream" );

				multipartContent.Add( dataContent );

				response = await this._client.PostAsync( string.Format( "{0}/{1}/files/{2}/content", ApiUploadUrl, Version, fileEntry.Id ), multipartContent );
			} else {
				//file is new
				var multipartContent = new MultipartFormDataContent( );

				dataContent.Headers.ContentDisposition = new ContentDispositionHeaderValue( "form-data" ) {
					Name = "\"file\"",
					FileName = string.Format( "\"{0}\"", parts.Item2 )
				};
				dataContent.Headers.ContentType = MediaTypeHeaderValue.Parse( "application/octet-stream" );

				var metadata = new {
					parent = new {
						id = targetFolder.Id
					},
					name = parts.Item2
				};

				var metadataContent = new ObjectContent( metadata.GetType( ), metadata, new JsonMediaTypeFormatter( ) );
				metadataContent.Headers.ContentDisposition = new ContentDispositionHeaderValue( "form-data" ) {
					Name = "\"metadata\""
				};

				multipartContent.Add( dataContent );
				multipartContent.Add( metadataContent );
				response = await this._client.PostAsync( string.Format( "{0}/{1}/files/content", ApiUploadUrl, Version ), multipartContent );
			}

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
				case HttpStatusCode.Created:
					break;
				default:
					throw new CloudStorageRequestFailedException( "Unable to upload file." );
			}

			// Read response asynchronously as JsonValue and write out tweet texts
			var col = await response.Content.ReadAsAsync<ItemCollectionInternal>( );

			var file = new BoxFileInternal( );
			mergeItemToFile( col.Entries.First( ), file );
			return new BoxFileMetaData( file );
		}

		public async Task<MetaDataBase> Delete( string path ) {
			return await Delete( path, true );
		}

		public async Task<MetaDataBase> Delete( string path, bool recursive ) {
			var item = await this.getItemByPath( path, BoxItemType.Unknown, true );

			if( item.Type.ToLowerInvariant( ) == "folder" ) {
				var queryParams = new List<KeyValuePair<string, string>> {
						new KeyValuePair<string, string>( "recursive", recursive.ToString().ToLower() )
					};

				var message = new HttpRequestMessage( HttpMethod.Delete, string.Format( "{0}/{1}/folders/{2}{3}", ApiBaseUrl, Version, item.Id,
					queryParams.ToQueryString( )
				) );

				var response = await _client.SendAsync( message );
				switch( response.StatusCode ) {
					case HttpStatusCode.OK:
					case HttpStatusCode.NoContent:
						break;
					default:
						throw new CloudStorageRequestFailedException( "Unable to delete folder." );
				}

				return new BoxFolderMetaData( item ) {
					Path = response.Content.Headers.ContentLocation.ToString( ),
					IsDeleted = true
				};
			} else {
				var message = new HttpRequestMessage( HttpMethod.Delete, string.Format( "{0}/{1}/files/{2}", ApiBaseUrl, Version, item.Id ) );
				message.Headers.TryAddWithoutValidation( "If-Match", item.ETag );

				var response = await _client.SendAsync( message );
				switch( response.StatusCode ) {
					case HttpStatusCode.OK:
					case HttpStatusCode.NoContent:
						break;
					default:
						throw new CloudStorageRequestFailedException( "Unable to delete file." );
				}
				return new BoxFileMetaData( item ) {
					Path = response.Content.Headers.ContentLocation.ToString( ),
					IsDeleted = true
				};
			}
		}

		public async Task<MetaDataBase> Copy( string fromPath, string toPath ) {
			Tuple<string, string> parentDirectoryAndTargetItemName = this.extractParentDirectoryAndItemNameFromFullPath( toPath );
			var gmdTask = this.getItemByPath( fromPath, BoxItemType.Unknown, true );
			var createDestinationParentFolderTask = this.getFolderByPath( parentDirectoryAndTargetItemName.Item1, false );

			gmdTask.Wait( );
			createDestinationParentFolderTask.Wait( );

			var sourceItem = gmdTask.Result;
			var destinationParent = createDestinationParentFolderTask.Result;

			if( sourceItem.Type.ToLowerInvariant( ) == "folder" ) {
				var response = await _client.PostAsJsonAsync( string.Format( "{0}/{1}/folders/{2}/copy", ApiBaseUrl, Version, sourceItem.Id ), new {
					id = sourceItem.Id,
					name = parentDirectoryAndTargetItemName.Item2,
					parent = new {
						id = destinationParent.Id
					}
				} );

				switch( response.StatusCode ) {
					case HttpStatusCode.OK:
					case HttpStatusCode.Created:
						break;
					case HttpStatusCode.Unauthorized:
						var msg = await response.Content.ReadAsStringAsync( );
						throw new CloudStorageAuthorizationException( msg );
					default:
						throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
				}
				return new BoxFolderMetaData( await response.Content.ReadAsAsync<BoxFolderInternal>( ) );

			} else if( sourceItem.Type.ToLowerInvariant( ) == "file" ) {
				var response = await _client.PostAsJsonAsync( string.Format( "{0}/{1}/files/{2}/copy", ApiBaseUrl, Version, sourceItem.Id ), new {
					id = sourceItem.Id,
					name = parentDirectoryAndTargetItemName.Item2,
					parent = new {
						id = destinationParent.Id
					}
				} );

				switch( response.StatusCode ) {
					case HttpStatusCode.OK:
					case HttpStatusCode.Created:
						break;
					case HttpStatusCode.Unauthorized:
						var msg = await response.Content.ReadAsStringAsync( );
						throw new CloudStorageAuthorizationException( msg );
					default:
						throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
				}
				return new BoxFileMetaData( await response.Content.ReadAsAsync<BoxFileInternal>( ) );
			} else {
				throw new NotImplementedException( "Cannot handle " + sourceItem.Type + " file system type." );
			}
		}

		public async Task<MetaDataBase> Move( string fromPath, string toPath ) {
			Tuple<string, string> parentDirectoryAndTargetItemName = this.extractParentDirectoryAndItemNameFromFullPath( toPath );
			var gmdTask = this.getItemByPath( fromPath, BoxItemType.Unknown, true );
			var createDestinationParentFolderTask = this.getFolderByPath( parentDirectoryAndTargetItemName.Item1, false );

			gmdTask.Wait( );
			createDestinationParentFolderTask.Wait( );

			var sourceItem = gmdTask.Result;
			var destinationParent = createDestinationParentFolderTask.Result;

			if( sourceItem.Type.ToLowerInvariant( ) == "folder" ) {
				var response = await _client.PutAsJsonAsync( string.Format( "{0}/{1}/folders/{2}", ApiBaseUrl, Version, sourceItem.Id ), new {
					parent = new {
						id = destinationParent.Id
					}
				} );

				switch( response.StatusCode ) {
					case HttpStatusCode.OK:
					case HttpStatusCode.Created:
						break;
					case HttpStatusCode.Unauthorized:
						var msg = await response.Content.ReadAsStringAsync( );
						throw new CloudStorageAuthorizationException( msg );
					default:
						throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
				}
				return new BoxFolderMetaData( await response.Content.ReadAsAsync<BoxFolderInternal>( ) );

			} else if( sourceItem.Type.ToLowerInvariant( ) == "file" ) {
				var response = await _client.PutAsJsonAsync( string.Format( "{0}/{1}/files/{2}", ApiBaseUrl, Version, sourceItem.Id ), new {
					parent = new {
						id = destinationParent.Id
					}
				} );

				switch( response.StatusCode ) {
					case HttpStatusCode.OK:
					case HttpStatusCode.Created:
						break;
					case HttpStatusCode.Unauthorized:
						var msg = await response.Content.ReadAsStringAsync( );
						throw new CloudStorageAuthorizationException( msg );
					default:
						throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
				}
				return new BoxFileMetaData( await response.Content.ReadAsAsync<BoxFileInternal>( ) );
			} else {
				throw new NotImplementedException( "Cannot handle " + sourceItem.Type + " file system type." );
			}
		}

		public async Task<MetaDataBase> CreateFolder( string path ) {
			try {
				await this.getFolderByPath( path, true );

				//if we get here the folder already exists
				throw new CloudStorageRequestFailedException( "Could not create folder." );
			} catch( BoxItemNotFoundException ) { }

			return new BoxFolderMetaData( await this.getFolderByPath( path, false ) );
		}

		public async Task<Stream> GetThumbnail( BoxFileMetaData file ) {
			return await getThumbnail( file.Id );
		}

		public async Task<Stream> GetThumbnail( BoxFileMetaData file, ThumbnailSize size ) {
			int px = convertThumbnailSize2Pixels( size );
			return await getThumbnail( file.Id, maxHeight: px, maxWidth: px );
		}

		/// <summary>
		/// Gets the thumbnail of an image given its MetaData
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public async Task<Stream> GetThumbnail( MetaDataBase file ) {
			return await GetThumbnail( file.Path );
		}

		/// <summary>
		/// Gets the thumbnail of an image given its MetaData
		/// </summary>
		/// <param name="file"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public async Task<Stream> GetThumbnail( MetaDataBase file, ThumbnailSize size ) {
			return await GetThumbnail( file.Path, size );
		}

		public async Task<Stream> GetThumbnail( string path ) {
			var fileEntry = ( await this.getFileByPath( path, true ) );

			return await getThumbnail( fileEntry.Id );
		}

		public async Task<Stream> GetThumbnail( string path, ThumbnailSize size ) {
			int px = convertThumbnailSize2Pixels( size );
			var fileEntry = ( await this.getFileByPath( path, true ) );

			return await getThumbnail( fileEntry.Id, maxHeight: px, maxWidth: px );
		}

		private async Task<Stream> getThumbnail( string fileEntryId, int? minHeight = null, int? minWidth = null, int? maxHeight = null, int? maxWidth = null ) {
			var queryParams = new List<KeyValuePair<string, string>>( );
			if( minHeight.HasValue ) {
				queryParams.Add( new KeyValuePair<string, string>( "min_height", minHeight.Value.ToString( ) ) );
			}
			if( minWidth.HasValue ) {
				queryParams.Add( new KeyValuePair<string, string>( "min_width", minWidth.Value.ToString( ) ) );
			}
			if( maxHeight.HasValue ) {
				queryParams.Add( new KeyValuePair<string, string>( "max_height", maxHeight.Value.ToString( ) ) );
			}
			if( maxWidth.HasValue ) {
				queryParams.Add( new KeyValuePair<string, string>( "max_width", maxWidth.Value.ToString( ) ) );
			}

			var response = await _client.GetAsync( string.Format( "{0}/{1}/files/{2}/thumbnail.png{3}", ApiBaseUrl, Version, fileEntryId,
				queryParams.ToQueryString( )
			) );

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
				case HttpStatusCode.Accepted:
					break;
				case HttpStatusCode.Unauthorized:
					var msg = await response.Content.ReadAsStringAsync( );
					throw new CloudStorageAuthorizationException( msg );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}
			return await response.Content.ReadAsStreamAsync( );
		}

		private static int convertThumbnailSize2Pixels( ThumbnailSize size ) {
			switch( size ) {
				case ThumbnailSize.Small:
					return 32;
				case ThumbnailSize.MediumSmall:
					return 64;
				case ThumbnailSize.Medium:
					return 128;
				case ThumbnailSize.MediumLarge:
					return 256;
				case ThumbnailSize.Large:
					return 640;
				case ThumbnailSize.ExtraLarge:
					return 1024;
				default:
					throw new NotImplementedException( "Specified size is not supported by Box." );
			}
		}

		/// <summary>
		/// Function to get information about an item in the file system. 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="itemType"></param>
		/// <param name="errorIfNotFound"></param>
		/// <returns></returns>
		/// <remarks>If item does not exist and we know what type we are looking for and we set as errorIfNotFound to false then function will create an item.</remarks>
		private async Task<BoxItemInternal> getItemByPath( string path, BoxItemType itemType, bool errorIfNotFound ) {
			if( ( itemType == BoxItemType.Unknown ) && !errorIfNotFound ) {
				throw new CloudStorageException( "Parameter validation: cannot create an item of unknown type." );
			}
			BoxItemInternal targetItem;
			path = path.Trim( '/' );

			if( cached.ContainsKey( path ) && cached[ path ] != null ) {
				var c = cached[ path ];
				if( c.IsFolder ) {
					targetItem = await this.GetFolderInformationAsync( c.Id );
				} else {
					targetItem = await this.GetFileInformationAsync( c.Id );
				}

			} else {
				var pathItems = path.Split( '/' );
				Tuple<FileInfo, bool, int> lastExistingItemInThePathInfo = await this.walkFileSystemTreeToGetLastItemExistingInThePath( pathItems );

				if( !lastExistingItemInThePathInfo.Item2 ) {
					//The whole path doesn't exist. It needs to be created
					if( errorIfNotFound ) {
						throw new BoxItemNotFoundException( );
					}

					if( itemType == BoxItemType.Folder ) {
						targetItem = await this.createFolderForRemainingPath( lastExistingItemInThePathInfo.Item1.Id, pathItems, lastExistingItemInThePathInfo.Item3 );
					} else {
						throw new NotImplementedException( "The type " + itemType.ToString( ) + " was not expected by GetItemByPath." );
					}
				} else {
					if( lastExistingItemInThePathInfo.Item1.IsFolder ) {
						targetItem = await this.GetFolderInformationAsync( lastExistingItemInThePathInfo.Item1.Id );
					} else {
						targetItem = await this.GetFileInformationAsync( lastExistingItemInThePathInfo.Item1.Id );
					}
				}
			}
			return targetItem;
		}

		/// <summary>
		/// Get information for file at provided path. This function allows to create all the substructure for items that does not exist.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="errorIfNotFound"></param>
		/// <returns></returns>
		private async Task<BoxFileInternal> getFileByPath( string path, bool errorIfNotFound ) {
			var result = await this.getItemByPath( path, BoxItemType.File, errorIfNotFound );

			if( result.Type.ToLowerInvariant( ) == "file" ) {
				return (BoxFileInternal) result;
			} else {
				throw new BoxItemNotFoundException( "Item is not a file." );
			}
		}

		/// <summary>
		/// Get information for file at provided path. This function allows to create all the substructure for items that does not exist.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="errorIfNotFound"></param>
		/// <returns></returns>
		private async Task<BoxFolderInternal> getFolderByPath( string path, bool errorIfNotFound ) {
			var result = await this.getItemByPath( path, BoxItemType.Folder, errorIfNotFound );

			if( result.Type.ToLowerInvariant( ) == "folder" ) {
				return (BoxFolderInternal) result;
			} else {
				throw new BoxItemNotFoundException( "Item is not a folder." );
			}
		}

		private Tuple<string, string> extractParentDirectoryAndItemNameFromFullPath( string path ) {
			path = path.TrimEnd( '/' );
			int lastFSDivider = path.LastIndexOf( '/' );
			return new Tuple<string, string>( path.Substring( 0, lastFSDivider ), path.Substring( lastFSDivider + 1 ) );
		}

		/// <summary>
		/// Method to locate item by path or find the most further existing item in the path.
		/// </summary>
		/// <param name="pathItems"></param>
		/// <returns></returns>
		/// <remarks>Because of the limitations of the Box API we are recursively walking file system.</remarks>
		private async Task<Tuple<FileInfo, bool, int>> walkFileSystemTreeToGetLastItemExistingInThePath( string[ ] pathItems ) {
			FileInfo currentResult = null;
			int currentItemIndex;
			string currentItemPath = string.Empty;
			string workingPath = string.Empty;


			for( currentItemIndex = 0; currentItemIndex < pathItems.Length; ++currentItemIndex ) {
				if( workingPath == string.Empty ) {
					workingPath = pathItems[ currentItemIndex ];
				} else {
					workingPath = string.Format( "{0}/{1}", workingPath, pathItems[ currentItemIndex ] );
				}

				if( cached.ContainsKey( workingPath ) ) {
					var cachedResult = cached[ workingPath ];
					if( cachedResult != null ) {
						currentResult = cachedResult;
					} else {
						//return the last item we found
						break;
					}
				} else {
					BoxItemInternal result = null;

					List<BoxItemInternal> contents = await this.getAllItemsForFolder( currentResult == null ? "0" : currentResult.Id, 0 );

					for( int i = 0; i < contents.Count; ++i ) {
						var entry = contents[ i ];
						var fi = new FileInfo {
							Id = entry.Id
						};
						switch( entry.Type.ToLowerInvariant( ) ) {
							case "folder":
								fi.IsFolder = true;
								break;
							case "file":
								fi.IsFolder = false;
								break;
							default:
								throw new NotImplementedException( "The type " + entry.Type + " was not expected by GetItemByPath." );
						}
						string path;

						if( currentItemPath == string.Empty ) {
							path = entry.Name;
						} else {
							path = string.Format( "{0}/{1}", currentItemPath, entry.Name );
						}
						if( !cached.ContainsKey( path ) ) {
							cached.Add( path, fi );
						}
					}
					if( cached.ContainsKey( workingPath ) ) {
						currentResult = cached[ workingPath ];
					} else {
						cached.Add( workingPath, null );
						//return the last item we found
						break;
					}
				}
				currentItemPath = workingPath;
			}

			return new Tuple<FileInfo, bool, int>( currentResult, currentItemIndex == pathItems.Length, currentItemIndex );
		}

		private async Task<List<BoxItemInternal>> getAllItemsForFolder( string id, int initailOffset ) {
			const int limit = 100;
			int offset = initailOffset;
			List<BoxItemInternal> contents = new List<BoxItemInternal>( );
			int totalCount;

			do {
				ItemCollectionInternal currentFolderItems = await this.GetFolderItemsAsync( id, limit, offset );
				contents.AddRange( currentFolderItems.Entries );
				totalCount = currentFolderItems.Total_Count;
				offset += limit;
			} while( contents.Count + initailOffset < totalCount );

			return contents;
		}
		private async Task<BoxItemInternal> createFolderForRemainingPath( string parentId, string[ ] pathItems, int startIndex ) {
			BoxFolderInternal currentFolder = null;
			string path = string.Empty;

			for( int i = 0; i < startIndex; ++i ) {
				if( path == string.Empty ) {
					path = pathItems[ i ];
				} else {
					path = string.Format( "{0}/{1}", path, pathItems[ i ] );
				}
			}

			for( int i = startIndex; i < pathItems.Length; ++i ) {
				path = string.Format( "{0}/{1}", path, pathItems[ i ] );
				currentFolder = await this.createFolder( parentId, pathItems[ i ] );

				if( cached.ContainsKey( path ) ) {
					cached[ path ] = new FileInfo { Id = currentFolder.Id, IsFolder = true };
				} else {
					cached.Add( path, new FileInfo { Id = currentFolder.Id, IsFolder = true } );
				}

				parentId = currentFolder.Id;
			}

			return currentFolder;
		}

		private async Task<BoxFolderInternal> createFolder( string parentId, string name ) {
			var response = await _client.PostAsJsonAsync( string.Format( "{0}/{1}/folders", ApiBaseUrl, Version ), new {
				name = name,
				parent = new {
					id = parentId
				}
			} );
			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
				case HttpStatusCode.Created:
					break;
				case HttpStatusCode.Unauthorized:
					var msg = await response.Content.ReadAsStringAsync( );
					throw new CloudStorageAuthorizationException( msg );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}
			return await response.Content.ReadAsAsync<BoxFolderInternal>( );
		}

		private void mergeItemToFile( BoxItemInternal boxItem, BoxFileInternal file ) {
			file.Type = boxItem.Type;
			file.Id = boxItem.Id;
			file.Sequence_Id = boxItem.Sequence_Id;
			file.ETag = boxItem.ETag;
			file.SHA1 = boxItem.SHA1;
			file.Name = boxItem.Name;
			file.Description = boxItem.Description;
			file.Size = boxItem.Size;
			file.Path_Collection = boxItem.Path_Collection;
			file.Created_At = boxItem.Created_At;
			file.Modified_At = boxItem.Modified_At;
			file.Trashed_At = boxItem.Trashed_At;
			file.Purged_At = boxItem.Purged_At;
			file.Content_Created_At = boxItem.Content_Created_At;
			file.Content_Modified_At = boxItem.Content_Modified_At;
			file.Created_By = boxItem.Created_By;
			file.Modified_By = boxItem.Modified_By;
			file.Owned_By = boxItem.Owned_By;
			//file.Shared_Link = boxItem.Shared_Link;
			file.Parent = boxItem.Parent;
			file.Item_Status = boxItem.Item_Status;
		}
	}
}