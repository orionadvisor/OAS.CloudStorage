#region Using Statements

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using OAS.CloudStorage.Core;
using OAS.CloudStorage.Core.Exceptions;
using OAS.CloudStorage.Egnyte.Models;

#endregion

namespace OAS.CloudStorage.Egnyte {
	public class EgnyteClient : ICloudStorageClient {
		#region Public Properties

		/// <summary>
		/// Contains the Users Token and Secret
		/// </summary>
		public EgnyteUser UserLogin {
			get { return this._userLogin; }
			set {
				this._userLogin = value;
				this.SetAuthProviders( );
			}
		}

		/// <summary>
		/// TODO:
		/// </summary>
		public Root Root { get; set; }

		private string _basePath;
		public string BasePath {
			get {
				return _basePath;
			}
			set {
				if( string.IsNullOrEmpty( value ) || value == "/" ) {
					_basePath = null;
				} else {
					if( !value.StartsWith( "/" ) ) {
						value = "/" + value;
					}
					if( value.EndsWith( "/" ) ) {
						value = value.Substring( 0, value.Length - 1 );
					}

					_basePath = value;
				}
			}
		}
		#endregion

		#region Private Properties

		private const string Version = "v1";

		private readonly string ApiBaseUrl = "https://{0}.egnyte.com";

		private readonly string _apiKey;

		private HttpClient _client;
		private EgnyteUser _userLogin;

		private string GetNameFromPath( string path ) {
			return path.Split( '/' ).Last( );
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Default Constructor for the EgnyteClient
		/// </summary>
		/// <param name="apiDomain">The Api Secret to use for the Egnyte Requests</param>
		/// <param name="apiKey">The Api Key to use for the Egnyte Requests</param>
		public EgnyteClient( string apiDomain, string apiKey ) {
			Guard.NotNullOrEmpty( ( ) => apiDomain, apiDomain );
			Guard.NotNullOrEmpty( ( ) => apiKey, apiKey );

			this.ApiBaseUrl = string.Format( this.ApiBaseUrl, apiDomain );
			this.LoadClient( );
			this._apiKey = apiKey;
			this.UserLogin = null;
		}

		/// <summary>
		/// Creates an instance of the EgnyteClient given an API Key/Secret and an OAuth2 Access Token
		/// </summary>
		/// <param name="apiDomain">The Api Secret to use for the Egnyte Requests</param>
		/// <param name="apiKey">The Api Key to use for the Egnyte Requests</param>
		/// <param name="accessToken">The OAuth2 access token</param>
		public EgnyteClient( string apiDomain, string apiKey, string accessToken )
			: this( apiDomain, apiKey ) {
			this.UserLogin = new EgnyteUser( apiDomain, accessToken );
		}

		private void LoadClient( ) {
			var handler = new RetryOnceOnErrorHandler( );
			this._client = new HttpClient( handler ) {
				BaseAddress = new Uri( this.ApiBaseUrl )
			};
		}

		private void SetAuthProviders( ) {
			if( this.UserLogin == null ) {
				this._client.DefaultRequestHeaders.Authorization = null;
			} else {
				this._client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", this.UserLogin.Token );
			}
		}

		#endregion

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
		/// Gets MetaData for the root folder.
		/// </summary>
		/// <returns></returns>
		public async Task<MetaDataBase> GetMetaData( ) {
			return await this.GetMetaData( null );
		}

		/// <summary>
		/// Gets MetaData for a File or Folder. For a folder this includes its contents. For a file, this includes details such as file size.
		/// </summary>
		/// <returns></returns>
		public async Task<MetaDataBase> GetMetaData( string path ) {
			return await this.GetMetaData( path, true );
		}

		/// <summary>
		/// Gets MetaData for a File or Folder. For a folder this includes its contents. For a file, this includes details such as file size.
		/// </summary>
		/// <param name="path">The path to the folder to create</param>
		/// <param name="listContent">if false then do not include contents of folder in response</param>
		/// <returns>MetaData of the newly created folder</returns>
		public async Task<MetaDataBase> GetMetaData( string path, bool listContent ) {
			path = FixPath( path );

			if( path != "/" && path.EndsWith( "/" ) ) {
				path = path.Substring( 0, path.Length - 1 );
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "list_contents", listContent.ToString( ) )
			};

			var response = await this._client.GetAsync( string.Format( "pubapi/{0}/fs{1}{2}", Version,
				path,
				queryParams.ToQueryString( ) )
				);

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}

			// Read response asynchronously as JsonValue and write out objects
			var md = await response.Content.ReadAsAsync<EgnyteMetaDataInternal>( );

			string concat = path != null && path.EndsWith( "/" ) ? "{0}{1}" : "{0}/{1}";

			if( md.Is_Folder ) {
				return new EgnyteFolder {
					Path = path,
					Name = md.Name,
					IsFolder = md.Is_Folder,
					FolderId = md.Folder_Id,
					Folders = md.Folders == null ? null : md.Folders.Select( f => (FolderMetaDataBase) new EgnyteFolder {
						Path = string.Format( concat, path, f.Name ),
						Name = f.Name,
						IsFolder = f.Is_Folder,
						FolderId = f.Folder_Id
					} ).ToList( ),
					Files = md.Files == null ? null : md.Files.Select( f => (FileMetaDataBase) new EgnyteFile {
						Path = string.Format( concat, path, f.Name ),
						Name = f.Name,
						IsFolder = f.Is_Folder,
						EntryId = f.Entry_Id,
						Checksum = f.Checksum,
						LastModified = f.Last_Modified,
						UploadedBy = f.Uploaded_By,
						NumberOfBytes = f.Size
					} ).ToList( )
				};
			} else {
				return new EgnyteFile {
					Path = path,
					Name = md.Name,
					IsFolder = md.Is_Folder,
					EntryId = md.Entry_Id,
					Checksum = md.Checksum,
					LastModified = md.Last_Modified,
					UploadedBy = md.Uploaded_By,
					NumberOfBytes = md.Size,
					Versions = md.Versions == null ? null : md.Versions.Select( v => (FileVersionMetaDataBase) new EgnyteFileVersion {
						IsFolder = v.Is_Folder,
						EntryId = v.Entry_Id,
						Checksum = v.Checksum,
						LastModified = v.Last_Modified,
						UploadedBy = v.Uploaded_By,
						NumberOfBytes = v.Size
					} ).ToList( )
				};
			}
		}

		/// <summary>
		/// Downloads a File from egnyte given the path
		/// </summary>
		/// <param name="path">The path of the file to download</param>
		/// <returns>The files raw bytes</returns>
		public async Task<Stream> GetFile( string path ) {
			path = FixPath( path );

			var response = await this._client.GetAsync( string.Format( "pubapi/{0}/fs-content{1}", Version,
				path
				) );

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}

			// Read response asynchronously as JsonValue and write out objects
			return await response.Content.ReadAsStreamAsync( );
		}

		/// <summary>
		/// Uploads a File to egnyte given the raw data. 
		/// </summary>
		/// <param name="path">The full path of file to be uploaded</param>
		/// <param name="fileData">The file data</param>
		/// <returns>True on success</returns>
		public async Task<MetaDataBase> UploadFile( string path, byte[ ] fileData ) {
			return await this.UploadFile( path, new ByteArrayContent( fileData ) );
		}

		/// <summary>
		/// Uploads a File to egnyte given the raw data. 
		/// </summary>
		/// <param name="path">The full path of file to be uploaded</param>
		/// <param name="stream">The file data</param>
		/// <returns>True on success</returns>
		public async Task<MetaDataBase> UploadFile( string path, Stream stream ) {
			return await this.UploadFile( path, new StreamContent( stream ) );
		}

		private async Task<MetaDataBase> UploadFile( string path, HttpContent dataContent ) {
			path = FixPath( path );

			var response = await this._client.PostAsync( string.Format( "pubapi/{0}/fs-content{1}", Version,
				path
			), dataContent );

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
				case HttpStatusCode.Created:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}

			return new EgnyteMetaData {
				Path = path,
				IsFolder = false,
				Name = this.GetNameFromPath( path )
			};
		}

		/// <summary>
		/// Deletes the file or folder from egnyte with the given path
		/// </summary>
		/// <param name="path">The Path of the file or folder to delete.</param>
		/// <returns></returns>
		public async Task<MetaDataBase> Delete( string path ) {
			path = FixPath( path );

			var response = await this._client.DeleteAsync( string.Format( "pubapi/{0}/fs{1}", Version,
				path )
				);

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}

			return new EgnyteMetaData {
				Path = path,
				IsFolder = false,
				Name = this.GetNameFromPath( path )
			};
		}

		/// <summary>
		/// Copies a file or folder on egnyte
		/// </summary>
		/// <param name="fromPath">The path to the file or folder to copy</param>
		/// <param name="toPath">The path to where the file or folder is getting copied</param>
		/// <returns>True on success</returns>
		public async Task<MetaDataBase> Copy( string fromPath, string toPath ) {
			fromPath = FixPath( fromPath );
			toPath = FixPath( toPath );

			var response = await this._client.PostAsJsonAsync( string.Format( "pubapi/{0}/fs{1}", Version,
				fromPath ), new {
					action = "copy",
					destination = toPath
				}
				);

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}

			return new EgnyteMetaData {
				Path = toPath,
				IsFolder = false,
				Name = this.GetNameFromPath( toPath )
			};
		}

		/// <summary>
		/// Moves a file or folder on egnyte
		/// </summary>
		/// <param name="fromPath">The path to the file or folder to move</param>
		/// <param name="toPath">The path to where the file or folder is getting moved</param>
		/// <returns>True on success</returns>
		public async Task<MetaDataBase> Move( string fromPath, string toPath ) {
			fromPath = FixPath( fromPath );
			toPath = FixPath( toPath );

			var response = await this._client.PostAsJsonAsync( string.Format( "pubapi/{0}/fs{1}", Version,
				fromPath ), new {
					action = "move",
					destination = toPath
				}
				);

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}

			return new EgnyteMetaData {
				Path = toPath,
				IsFolder = false,
				Name = this.GetNameFromPath( toPath )
			};
		}

		/// <summary>
		/// Creates a folder on egnyte
		/// </summary>
		/// <param name="path">The path to the folder to create</param>
		/// <returns>MetaData of the newly created folder</returns>
		public async Task<MetaDataBase> CreateFolder( string path ) {
			path = FixPath( path );

			var response = await this._client.PostAsJsonAsync( string.Format( "pubapi/{0}/fs{1}", Version,
				path ),
				new {
					action = "add_folder"
				}
				);

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
				case HttpStatusCode.Created:
					break;
				case HttpStatusCode.Forbidden:
					throw new CloudStorageRequestFailedException( "Could not create folder." );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}

			return new EgnyteMetaData {
				Path = path,
				IsFolder = false,
				Name = this.GetNameFromPath( path )
			};
		}

		/// <summary>
		/// This starts the OAuth 2.0 authorization flow. This isn't an API call—it's the web page that lets the user sign in to Egnyte and authorize your app. The user must be redirected to the page over HTTPS and it should be presented to the user through their web browser. After the user decides whether or not to authorize your app, they will be redirected to the URL specified by the 'redirectUri'.
		/// </summary>
		/// <param name="redirectUri">Where to redirect the user after authorization has completed. A redirect URI is required for a token flow.</param>
		/// <returns>A URL to which your app should redirect the user for authorization.  After the user authorizes your app, they will be sent to your redirect URI. The type of response varies based on the 'oauth2AuthorizationFlow' argument.  .</returns>
		public string BuildAuthorizeUrl( string redirectUri ) {
			return BuildAuthorizeUrl( redirectUri, null );
		}

		/// <summary>
		/// This starts the OAuth 2.0 authorization flow. This isn't an API call—it's the web page that lets the user sign in to Egnyte and authorize your app. The user must be redirected to the page over HTTPS and it should be presented to the user through their web browser. After the user decides whether or not to authorize your app, they will be redirected to the URL specified by the 'redirectUri'.
		/// </summary>
		/// <param name="redirectUri">Where to redirect the user after authorization has completed. A redirect URI is required for a token flow.</param>
		/// <param name="mobile">If mobile=1 then we will serve up a mobile version of the OAuth authorization page. Set it to 0 or omit the parameter entirely if you are not integrating into a mobile application.</param>
		/// <returns>A URL to which your app should redirect the user for authorization.  After the user authorizes your app, they will be sent to your redirect URI. The type of response varies based on the 'oauth2AuthorizationFlow' argument.  .</returns>
		public string BuildAuthorizeUrl( string redirectUri, bool? mobile ) {
			if( string.IsNullOrWhiteSpace( redirectUri ) ) {
				throw new ArgumentNullException( "redirectUri" );
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "client_id", this._apiKey ),
				new KeyValuePair<string, string>( "redirect_uri", redirectUri ),
			};
			if( mobile.HasValue ) {
				queryParams.Add( new KeyValuePair<string, string>( "mobile", ( mobile.Value ? 1 : 0 ).ToString( ) ) );
			}
			return string.Format( "{0}/puboauth/token{1}", this.ApiBaseUrl, queryParams.ToQueryString( ) );
		}

		private string FixPath( string path ) {
			if( string.IsNullOrEmpty( path ) || !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			if( !string.IsNullOrEmpty( this.BasePath ) ) {
				path = this.BasePath + path;
			}

			return path;
		}
	}
}