#region Using Statements

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using OAS.CloudStorage.Core;
using OAS.CloudStorage.Core.Exceptions;
using OAS.CloudStorage.DropBox.CoreApi.Models;

#endregion

namespace OAS.CloudStorage.DropBox.CoreApi {
	public class DropBoxClient : ICloudStorageThumbnailProvider {
		#region Public Properties

		/// <summary>
		/// Contains the Users Token and Secret
		/// </summary>
		public DropBoxUser UserLogin {
			get { return this._userLogin; }
			set {
				this._userLogin = value;
				this.SetAuthProviders( );
			}
		}

		/// <summary>
		/// To use Dropbox API in sandbox mode (app folder access) set to true
		/// </summary>
		public Root Root { get; set; }

		#endregion

		#region Private Properties

		private const string ApiBaseUrl = "https://api.dropbox.com";
		private const string ApiContentBaseUrl = "https://api-content.dropbox.com";
		private const string Version = "1";

		private const string SandboxRoot = "sandbox";
		private const string DropboxRoot = "dropbox";

		private readonly string _apiKey;
		private readonly string _appsecret;
		private readonly AuthenticationMethod _authenticationMethod;
		private readonly List<MediaTypeFormatter> _mediaTypeFormatters;

		private HttpClient _client;
		private DropBoxUser _userLogin;

		/// <summary>
		/// Gets the directory root for the requests (full or sandbox mode)
		/// </summary>
		private string RootString {
			get {
				switch( this.Root ) {
					case Root.Dropbox:
						return DropboxRoot;
					default:
						return SandboxRoot;
				}
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Default Constructor for the DropboxClient
		/// </summary>
		/// <param name="apiKey">The Api Key to use for the Dropbox Requests</param>
		/// <param name="appSecret">The Api Secret to use for the Dropbox Requests</param>
		/// <param name="authenticationMethod">The authentication method to use.</param>
		public DropBoxClient( string apiKey, string appSecret, AuthenticationMethod authenticationMethod = AuthenticationMethod.OAuth2 ) {
			Guard.NotNullOrEmpty( ( ) => apiKey, apiKey );
			Guard.NotNullOrEmpty( ( ) => appSecret, appSecret );
			
			this.LoadClient( );
			this._apiKey = apiKey;
			this._appsecret = appSecret;
			this._authenticationMethod = authenticationMethod;
			this.UserLogin = null;

			var js = new JsonMediaTypeFormatter( );
			js.SupportedMediaTypes.Add( new MediaTypeHeaderValue( "text/javascript" ) );

			this._mediaTypeFormatters = new List<MediaTypeFormatter> {
				js
			};
		}

		/// <summary>
		/// Creates an instance of the DropBoxClient given an API Key/Secret and an OAuth2 Access Token
		/// </summary>
		/// <param name="apiKey">The Api Key to use for the Dropbox Requests</param>
		/// <param name="appSecret">The Api Secret to use for the Dropbox Requests</param>
		/// <param name="accessToken">The OAuth2 access token</param>
		/// <param name="uid">The OAuth2 uid</param>
		public DropBoxClient( string apiKey, string appSecret, string accessToken, string uid )
			: this( apiKey, appSecret, AuthenticationMethod.OAuth2 ) {
			this.UserLogin = new DropBoxUser( accessToken, uid );
		}

		private void LoadClient( ) {
			this._client = new HttpClient( );


			//Default to full access
			this.Root = Root.Dropbox;
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
		/// This starts the OAuth 2.0 authorization flow. This isn't an API call—it's the web page that lets the user sign in to Egnyte and authorize your app. The user must be redirected to the page over HTTPS and it should be presented to the user through their web browser. After the user decides whether or not to authorize your app, they will be redirected to the URL specified by the 'redirectUri'.
		/// </summary>
		/// <param name="redirectUri">Where to redirect the user after authorization has completed. A redirect URI is required for a token flow.</param>
		/// <returns>A URL to which your app should redirect the user for authorization.  After the user authorizes your app, they will be sent to your redirect URI. The type of response varies based on the 'oauth2AuthorizationFlow' argument.  .</returns>
		public string BuildAuthorizeUrl( string redirectUri ) {
			return BuildAuthorizeUrl( OAuth2AuthorizationFlow.Token, redirectUri, null );
		}

		/// <summary>
		/// This starts the OAuth 2.0 authorization flow. This isn't an API call—it's the web page that lets the user sign in to Dropbox and authorize your app. The user must be redirected to the page over HTTPS and it should be presented to the user through their web browser. After the user decides whether or not to authorize your app, they will be redirected to the URL specified by the 'redirectUri'.
		/// </summary>
		/// <param name="oAuth2AuthorizationFlow">The type of authorization flow to use.  See the OAuth2AuthorizationFlow enum documentation for more information.</param>
		/// <param name="redirectUri">Where to redirect the user after authorization has completed. This must be the exact URI registered in the app console (https://www.dropbox.com/developers/apps), though localhost and 127.0.0.1 are always accepted. A redirect URI is required for a token flow, but optional for code. If the redirect URI is omitted, the code will be presented directly to the user and they will be invited to enter the information in your app.</param>
		/// <param name="state">Arbitrary data that will be passed back to your redirect URI. This parameter can be used to track a user through the authorization flow in order to prevent cross-site request forgery (CRSF) attacks.</param>
		/// <returns>A URL to which your app should redirect the user for authorization.  After the user authorizes your app, they will be sent to your redirect URI. The type of response varies based on the 'oauth2AuthorizationFlow' argument.  .</returns>
		public string BuildAuthorizeUrl( OAuth2AuthorizationFlow oAuth2AuthorizationFlow, string redirectUri, string state = null ) {
			if( string.IsNullOrWhiteSpace( redirectUri ) ) {
				throw new ArgumentNullException( "redirectUri" );
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "response_type", Enum.GetName( typeof( OAuth2AuthorizationFlow ), oAuth2AuthorizationFlow ).ToLower( ) ),
				new KeyValuePair<string, string>( "client_id", this._apiKey ),
				new KeyValuePair<string, string>( "redirect_uri", redirectUri )
			};
			if( !string.IsNullOrWhiteSpace( state ) ) {
				queryParams.Add( new KeyValuePair<string, string>( "state", state ) );
			}

			return string.Format( "{0}/{1}/oauth2/authorize{2}", ApiBaseUrl, Version,
				queryParams.ToQueryString( )
				);
		}

		#region User

		/// <summary>
		///     Acquire an OAuth2 bearer token once the user has authorized the app.  This endpoint only applies to apps using the AuthorizationFlow.Code flow. This will only work once per code.
		/// </summary>
		/// <param name="code">The authorization code provided by Dropbox when the user was redirected back to your site.</param>
		/// <param name="redirectUri">The redirect Uri for your site. This is only used to validate that it matches the original /oauth2/authorize; the user will not be redirected again.</param>
		/// <returns>An OAuth2 bearer token.</returns>
		public async Task<DropBoxUser> GetAccessToken( string code, string redirectUri ) {
			var keys = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "code", code ),
				new KeyValuePair<string, string>( "grant_type", "authorization_code" ),
				new KeyValuePair<string, string>( "client_id", this._apiKey ),
				new KeyValuePair<string, string>( "client_secret", this._appsecret ),
				new KeyValuePair<string, string>( "redirect_uri", redirectUri ),
			};
			var content = new FormUrlEncodedContent( keys );

			var response = await this._client.PostAsync( string.Format( "{0}/{1}/oauth2/token", ApiBaseUrl, Version ), content );

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				default:
					throw new CloudStorageAuthorizationException( );
			}

			// Read response asynchronously as JsonValue and write out objects
			var token = await response.Content.ReadAsAsync<OAuth2AccessToken>( this._mediaTypeFormatters );
			this.UserLogin = new DropBoxUser( token.Access_Token, token.Uid );
			return this.UserLogin;
		}

		public async Task<AccountInfo> AccountInfo( ) {
			var response = await this._client.GetAsync( string.Format( "{0}/{1}/account/info", ApiBaseUrl, Version ) );

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				default:
					throw new CloudStorageAuthorizationException( );
			}

			// Read response asynchronously as JsonValue and write out objects
			return await response.Content.ReadAsAsync<AccountInfo>( this._mediaTypeFormatters );
		}

		#endregion

		#region Files

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
		/// <param name="path">The path of the file or folder</param>
		/// <returns></returns>
		public async Task<MetaDataBase> GetMetaData( string path ) {
			if( string.IsNullOrEmpty( path ) || !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var response = await this._client.GetAsync( string.Format( "{0}/{1}/metadata/{2}{3}", ApiBaseUrl, Version, this.RootString,
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

			// Read response asynchronously as JsonValue and write out objects
			var i = await response.Content.ReadAsAsync<DropBoxMetaDataInternal>( this._mediaTypeFormatters );

			if( !i.Is_Dir ) {
				return await GetVersions( i, path, 100 );
			}

			return this.ConvertInternalToMetaData( i );
		}

		/// <summary>
		/// Downloads a File from dropbox given the path
		/// </summary>
		/// <param name="path">The path of the file to download</param>
		/// <returns>The files raw bytes</returns>
		public async Task<Stream> GetFile( string path ) {
			return await this.GetFile( path, null, null );
		}

		/// <summary>
		/// Uploads a File to Dropbox given the raw data. 
		/// </summary>
		/// <param name="path">The full path of file to be uploaded</param>
		/// <param name="fileData">The file data</param>
		/// <returns>True on success</returns>
		public async Task<MetaDataBase> UploadFile( string path, byte[ ] fileData ) {
			return await this.uploadFilePUT( path, new ByteArrayContent( fileData ) );
		}

		/// <summary>
		/// Uploads a File to Dropbox given the raw data. 
		/// </summary>
		/// <param name="path">The full path of file to be uploaded</param>
		/// <param name="stream">The file data</param>
		/// <returns>True on success</returns>
		public async Task<MetaDataBase> UploadFile( string path, Stream stream ) {
			return await this.uploadFilePUT( path, new StreamContent( stream ) );
		}

		/// <summary>
		/// Deletes the file or folder from dropbox with the given path
		/// </summary>
		/// <param name="path">The Path of the file or folder to delete.</param>
		/// <returns></returns>
		public async Task<MetaDataBase> Delete( string path ) {
			if( !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "path", path ),
				new KeyValuePair<string, string>( "root", this.RootString )
			};

			var response = await this._client.GetAsync( string.Format( "{0}/{1}/fileops/delete{2}", ApiBaseUrl, Version,
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
			var i = await response.Content.ReadAsAsync<DropBoxMetaDataInternal>( this._mediaTypeFormatters );
			return this.ConvertInternalToMetaData( i );
		}

		/// <summary>
		/// Copies a file or folder on Dropbox
		/// </summary>
		/// <param name="fromPath">The path to the file or folder to copy</param>
		/// <param name="toPath">The path to where the file or folder is getting copied</param>
		/// <returns>True on success</returns>
		public async Task<MetaDataBase> Copy( string fromPath, string toPath ) {
			if( !fromPath.StartsWith( "/" ) ) {
				fromPath = "/" + fromPath;
			}

			if( !toPath.StartsWith( "/" ) ) {
				toPath = "/" + toPath;
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "from_path", fromPath ),
				new KeyValuePair<string, string>( "to_path", toPath ),
				new KeyValuePair<string, string>( "root", this.RootString )
			};
			var response = await this._client.GetAsync( string.Format( "{0}/{1}/fileops/copy{2}", ApiBaseUrl, Version,
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
			var i = await response.Content.ReadAsAsync<DropBoxMetaDataInternal>( this._mediaTypeFormatters );
			return this.ConvertInternalToMetaData( i );
		}

		/// <summary>
		/// Moves a file or folder on Dropbox
		/// </summary>
		/// <param name="fromPath">The path to the file or folder to move</param>
		/// <param name="toPath">The path to where the file or folder is getting moved</param>
		/// <returns>True on success</returns>
		public async Task<MetaDataBase> Move( string fromPath, string toPath ) {
			if( !fromPath.StartsWith( "/" ) ) {
				fromPath = "/" + fromPath;
			}

			if( !toPath.StartsWith( "/" ) ) {
				toPath = "/" + toPath;
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "from_path", fromPath ),
				new KeyValuePair<string, string>( "to_path", toPath ),
				new KeyValuePair<string, string>( "root", this.RootString )
			};

			var response = await this._client.GetAsync( string.Format( "{0}/{1}/fileops/move{2}", ApiBaseUrl, Version,
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
			var i = await response.Content.ReadAsAsync<DropBoxMetaDataInternal>( this._mediaTypeFormatters );
			return this.ConvertInternalToMetaData( i );
		}

		private async Task<MetaDataBase> GetVersions( DropBoxMetaDataInternal internalObj, string path, int limit ) {
			if( string.IsNullOrEmpty( path ) || !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "rev_limit", limit.ToString( ) )
			};

			var response = await this._client.GetAsync( string.Format( "{0}/{1}/revisions/{2}{3}{4}", ApiBaseUrl, Version, this.RootString,
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
			var l = await response.Content.ReadAsAsync<List<DropBoxMetaDataInternal>>( this._mediaTypeFormatters );

			var ret = (DropBoxFile) this.ConvertInternalToMetaData( internalObj );
			ret.Versions = l.Select( i => (FileVersionMetaDataBase) this.ConvertInternalToVersionedMetaData( i ) ).ToList( );

			return ret;
		}

		/// <summary>
		/// Gets list of metadata for search string
		/// </summary>
		/// <param name="searchString">The search string </param>
		public async Task<List<MetaDataBase>> Search( string searchString ) {
			return await this.Search( searchString, string.Empty );
		}

		/// <summary>
		/// Gets list of metadata for search string
		/// </summary>
		/// <param name="searchString">The search string </param>
		/// <param name="path">The path of the file or folder</param>
		public async Task<List<MetaDataBase>> Search( string searchString, string path ) {
			if( string.IsNullOrEmpty( path ) || !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "query", searchString )
			};
			var response = await this._client.GetAsync( string.Format( "{0}/{1}/search/{2}{3}{4}", ApiBaseUrl, Version, this.RootString,
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
			var l = await response.Content.ReadAsAsync<List<DropBoxMetaDataInternal>>( this._mediaTypeFormatters );

			return l.Select( i => (MetaDataBase) this.ConvertInternalToMetaData( i ) ).ToList( );
		}

		/// <summary>
		/// Downloads a File from dropbox given the path
		/// </summary>
		/// <param name="path">The path of the file to download</param>
		/// <param name="range">Optional: if provided only the files raw butes between the start and end specified will be returned..</param>
		/// <param name="rev">Revision string as featured by <code>MetaData.Rev</code></param>
		/// <returns>The files raw bytes</returns>
		public async Task<Stream> GetFile( string path, Range<long>? range = null, string rev = null ) {
			if( !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var request = new HttpRequestMessage {
				Method = HttpMethod.Get
			};

			if( range.HasValue ) {
				request.Headers.Add( "Range", "bytes=" + range.Value.Start + "-" + range.Value.End );
			}

			var queryParams = new List<KeyValuePair<string, string>> { };
			if( !string.IsNullOrWhiteSpace( rev ) ) {
				queryParams.Add( new KeyValuePair<string, string>( "rev", rev ) );
			}

			request.RequestUri = new Uri( string.Format( "{0}/{1}/files/{2}{3}{4}", ApiContentBaseUrl, Version, this.RootString,
				path,
				queryParams.ToQueryString( ) )
				);

			var response = await this._client.SendAsync( request );

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
				case HttpStatusCode.PartialContent:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}

			// Read response asynchronously as JsonValue and write out objects
			return await response.Content.ReadAsStreamAsync( );
		}

		private async Task<MetaDataBase> uploadFilePUT( string path, HttpContent dataContent, bool? overwrite = null, int? parent_rev = null ) {
			if( !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var queryParams = new List<KeyValuePair<string, string>> { };
			if( overwrite.HasValue ) {
				queryParams.Add( new KeyValuePair<string, string>( "overwrite", overwrite.ToString( ) ) );
			}
			if( parent_rev.HasValue ) {
				queryParams.Add( new KeyValuePair<string, string>( "parent_rev", parent_rev.ToString( ) ) );
			}

			var response = await this._client.PutAsync( string.Format( "{0}/{1}/files_put/{2}{3}{4}", ApiContentBaseUrl, Version, this.RootString,
				 path,
				queryParams.ToQueryString( )
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

			// Read response asynchronously as JsonValue and write out objects
			var i = await response.Content.ReadAsAsync<DropBoxMetaDataInternal>( this._mediaTypeFormatters );

			return this.ConvertInternalToMetaData( i );
		}

		/// <summary>
		/// Starts a chunked upload to Dropbox given a byte array.
		/// </summary>
		/// <param name="fileData">The file data</param>
		/// <returns>A object representing the chunked upload on success</returns>
		public async Task<ChunkedUpload> StartChunkedUpload( byte[ ] fileData ) {
			var dataContent = new ByteArrayContent( fileData );
			//dataContent.Headers.ContentType = MediaTypeHeaderValue.Parse( "file" );

			var response = await this._client.PostAsync( string.Format( "{0}/{1}/chunked_upload", ApiContentBaseUrl, Version ), dataContent );

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}

			// Read response asynchronously as JsonValue and write out objects
			return await response.Content.ReadAsAsync<ChunkedUpload>( this._mediaTypeFormatters );
		}

		/// <summary>
		/// Add data to a chunked upload given a byte array.
		/// </summary>
		/// <param name="upload">A ChunkedUpload object received from the StartChunkedUpload method</param>
		/// <param name="fileData">The file data</param>
		/// <returns>A object representing the chunked upload on success</returns>
		public async Task<ChunkedUpload> AppendChunkedUpload( ChunkedUpload upload, byte[ ] fileData ) {
			var dataContent = new ByteArrayContent( fileData );
			//dataContent.Headers.ContentType = MediaTypeHeaderValue.Parse( "file" );

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "upload_id", upload.Upload_Id ),
				new KeyValuePair<string, string>( "offset", upload.Offset.ToString( ) )
			};
			var response = await this._client.PutAsync( string.Format( "{0}/{1}/chunked_upload{2}", ApiContentBaseUrl, Version,
				queryParams.ToQueryString( ) ), dataContent
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
			return await response.Content.ReadAsAsync<ChunkedUpload>( this._mediaTypeFormatters );
		}

		/// <summary>
		/// Commit a completed chunked upload
		/// </summary>
		/// <param name="upload">A ChunkedUpload object received from the StartChunkedUpload method</param>
		/// <param name="path">The full path of the file to upload to</param>
		/// <param name="overwrite">Specify wether the file upload should replace an existing file.</param>
		/// <returns>A object representing the chunked upload on success</returns>
		public async Task<MetaDataBase> CommitChunkedUpload( ChunkedUpload upload, string path, bool overwrite = true ) {
			if( !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var keys = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "overwrite", overwrite.ToString( ) ),
				new KeyValuePair<string, string>( "upload_id", upload.Upload_Id )
			};
			var content = new FormUrlEncodedContent( keys );

			var response = await this._client.PostAsync( string.Format( "{0}/{1}/commit_chunked_upload/{2}{3}", ApiContentBaseUrl, Version, this.RootString,
				path ), content
				);

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
				case HttpStatusCode.Created:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}

			// Read response asynchronously as JsonValue and write out objects
			var i = await response.Content.ReadAsAsync<DropBoxMetaDataInternal>( this._mediaTypeFormatters );

			return this.ConvertInternalToMetaData( i );
		}

		/// <summary>
		/// Creates and returns a copy_ref to a file.
		/// 
		/// This reference string can be used to copy that file to another user's Dropbox by passing it in as the from_copy_ref parameter on /fileops/copy.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public async Task<CopyRefResponse> GetCopyRef( string path ) {
			if( !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var response = await this._client.GetAsync( string.Format( "{0}/{1}/copy_ref/{2}{3}", ApiBaseUrl, Version, this.RootString,
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

			// Read response asynchronously as JsonValue and write out objects
			return await response.Content.ReadAsAsync<CopyRefResponse>( this._mediaTypeFormatters );
		}

		/// <summary>
		/// Copies a file or folder on Dropbox using a copy_ref as the source.
		/// </summary>
		/// <param name="fromCopyRef">Specifies a copy_ref generated from a previous /copy_ref call</param>
		/// <param name="toPath">The path to where the file or folder is getting copied</param>
		/// <returns>True on success</returns>
		public async Task<MetaDataBase> CopyFromCopyRef( string fromCopyRef, string toPath ) {
			if( !toPath.StartsWith( "/" ) ) {
				toPath = "/" + toPath;
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "from_copy_ref", fromCopyRef ),
				new KeyValuePair<string, string>( "to_path", toPath ),
				new KeyValuePair<string, string>( "root", this.RootString )
			};

			var response = await this._client.GetAsync( string.Format( "{0}/{1}/fileops/copy{2}", ApiBaseUrl, Version,
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
			var i = await response.Content.ReadAsAsync<DropBoxMetaDataInternal>( this._mediaTypeFormatters );

			return this.ConvertInternalToMetaData( i );
		}

		/// <summary>
		/// Creates a folder on Dropbox
		/// </summary>
		/// <param name="path">The path to the folder to create</param>
		/// <returns>MetaData of the newly created folder</returns>
		public async Task<MetaDataBase> CreateFolder( string path ) {
			if( !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "path", path ),
				new KeyValuePair<string, string>( "root", this.RootString )
			};

			var response = await this._client.GetAsync( string.Format( "{0}/{1}/fileops/create_folder{2}", ApiBaseUrl, Version,
				queryParams.ToQueryString( ) )
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

			// Read response asynchronously as JsonValue and write out objects
			var i = await response.Content.ReadAsAsync<DropBoxMetaDataInternal>( this._mediaTypeFormatters );

			return this.ConvertInternalToMetaData( i );
		}

		/// <summary>
		/// Creates and returns a shareable link to files or folders.
		/// Note: Links created by the /shares API call expire after thirty days.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="shortUrl"></param>
		/// <returns></returns>
		public async Task<ShareResponse> GetShare( string path, bool shortUrl = true ) {
			if( !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "short_url", shortUrl.ToString( ) )
			};

			var response = await this._client.GetAsync( string.Format( "{0}/{1}/shares/{2}{3}{4}", ApiBaseUrl, Version, this.RootString,
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
			return await response.Content.ReadAsAsync<ShareResponse>( this._mediaTypeFormatters );
		}

		/// <summary>
		/// Returns a link directly to a file.
		/// Similar to /shares. The difference is that this bypasses the Dropbox webserver, used to provide a preview of the file, so that you can effectively stream the contents of your media.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public async Task<ShareResponse> GetMedia( string path ) {
			if( !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var response = await this._client.GetAsync( string.Format( "{0}/{1}/media/{2}{3}", ApiBaseUrl, Version, this.RootString,
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

			// Read response asynchronously as JsonValue and write out objects
			return await response.Content.ReadAsAsync<ShareResponse>( this._mediaTypeFormatters );
		}

		/// <summary>
		/// Gets the thumbnail of an image given its MetaData
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public async Task<Stream> GetThumbnail( MetaDataBase file ) {
			return await GetThumbnail( file.Path, ThumbnailSize.Small );
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

		/// <summary>
		/// Gets the thumbnail of an image given its path
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public async Task<Stream> GetThumbnail( string path ) {
			return await GetThumbnail( path, ThumbnailSize.Small );
		}

		/// <summary>
		/// Gets the thumbnail of an image given its path
		/// </summary>
		/// <param name="path">The path to the picture</param>
		/// <param name="size">The size to return the thumbnail</param>
		/// <returns></returns>
		public async Task<Stream> GetThumbnail( string path, ThumbnailSize size ) {
			return await GetThumbnail( path, size, ThumbnailFormat.Jpeg );
		}

		/// <summary>
		/// Gets the thumbnail of an image given its path
		/// </summary>
		/// <param name="path">The path to the picture</param>
		/// <param name="size">The size to return the thumbnail</param>
		/// <param name="format">The format to return the thumbnail as.</param>
		/// <returns></returns>
		public async Task<Stream> GetThumbnail( string path, ThumbnailSize size, ThumbnailFormat format ) {
			if( !path.StartsWith( "/" ) ) {
				path = "/" + path;
			}

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "size", this.ThumbnailSizeString( size ) ),
				new KeyValuePair<string, string>( "format", this.ThumbnailFormatString( format ) )
			};

			var response = await this._client.GetAsync( string.Format( "{0}/{1}/thumbnails/{2}{3}{4}", ApiContentBaseUrl, Version, this.RootString,
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
			return await response.Content.ReadAsStreamAsync( );
		}

		/// <summary>
		/// Gets the deltas for a user's folders and files.
		/// </summary>
		/// <param name="cursor">The value returned from the prior call to GetDelta or an empty string</param>
		/// <returns></returns>
		public async Task<DeltaPage> GetDelta( string cursor ) {
			var keys = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "cursor", cursor )
			};
			var content = new FormUrlEncodedContent( keys );
			var response = await this._client.PostAsync( string.Format( "{0}/{1}/delta", ApiBaseUrl, Version ), content );

			switch( response.StatusCode ) {
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.NotFound:
					throw new CloudStorageItemNotFoundException( );
				default:
					throw new HttpException( (int) response.StatusCode, response.Content.ReadAsStringAsync( ).Result );
			}

			// Read response asynchronously as JsonValue and write out objects
			var deltaResponse = await response.Content.ReadAsAsync<DeltaPageInternal>( this._mediaTypeFormatters );

			return new DeltaPage {
				Cursor = deltaResponse.Cursor,
				HasMore = deltaResponse.Has_More,
				Reset = deltaResponse.Reset,
				Entries = deltaResponse.Entries.Select( objList => new DeltaEntry {
					Path = (string) objList[ 0 ],
					MetaData = this.ConvertInternalToMetaData( ( (JObject) objList[ 1 ] ).ToObject<DropBoxMetaDataInternal>( ) )
				} ).ToList( )
			};
		}

		#endregion

		#region Privates

		private MetaDataBase ConvertInternalToVersionedMetaData( DropBoxMetaDataInternal i ) {
			return new DropBoxFileVersion( ) {
				Path = i.Path,
				Name = i.Path.Split( '/' ).Last( ),
				IsFolder = i.Is_Dir,
				Checksum = i.Hash,
				ThumbExists = i.Thumb_Exists,
				NumberOfBytes = i.Bytes,
				LastModified = i.Modified,
				IsDeleted = i.Is_Deleted,
				Size = i.Size,
				Root = i.Root,
				Icon = i.Icon,
				Revision = i.Revision,
			};
		}

		private MetaDataBase ConvertInternalToMetaData( DropBoxMetaDataInternal i ) {
			if( i.Is_Dir ) {
				return new DropBoxFolder {
					Path = i.Path,
					Name = i.Path.Split( '/' ).Last( ),
					IsFolder = i.Is_Dir,
					Checksum = i.Hash,
					ThumbExists = i.Thumb_Exists,
					NumberOfBytes = i.Bytes,
					LastModified = i.Modified,
					IsDeleted = i.Is_Deleted,
					Size = i.Size,
					Root = i.Root,
					Icon = i.Icon,
					Revision = i.Revision,
					Files = i.Contents == null ? null : i.Contents.Where( c => c.Is_Dir == false ).Select( c => (FileMetaDataBase) this.ConvertInternalToMetaData( c ) ).ToList( ),
					Folders = i.Contents == null ? null : i.Contents.Where( c => c.Is_Dir ).Select( c => (FolderMetaDataBase) this.ConvertInternalToMetaData( c ) ).ToList( )
				};
			}
			return new DropBoxFile( ) {
				Path = i.Path,
				Name = i.Path.Split( '/' ).Last( ),
				IsFolder = i.Is_Dir,
				Checksum = i.Hash,
				ThumbExists = i.Thumb_Exists,
				NumberOfBytes = i.Bytes,
				LastModified = i.Modified,
				IsDeleted = i.Is_Deleted,
				Size = i.Size,
				Root = i.Root,
				Icon = i.Icon,
				Revision = i.Revision
			};
		}

		private string ThumbnailSizeString( ThumbnailSize size ) {
			switch( size ) {
				case ThumbnailSize.Small:
					return "xs";
				case ThumbnailSize.MediumSmall:
					return "s";
				case ThumbnailSize.Medium:
					return "m";
				case ThumbnailSize.MediumLarge:
				case ThumbnailSize.Large:
					return "l";
				case ThumbnailSize.ExtraLarge:
					return "xl";
				default:
					return "s";
			}
		}

		private string ThumbnailFormatString( ThumbnailFormat format ) {
			switch( format ) {
				case ThumbnailFormat.Jpeg:
					return "jpeg";
				case ThumbnailFormat.Png:
					return "png";
				default:
					return "jpeg";
			}
		}

		#endregion
	}
}