#region Using Statements

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OAS.CloudStorage.DropBox.CoreApi;
using OAS.CloudStorage.DropBox.CoreApi.Models;

#endregion

namespace OAS.CloudStorage.Core.Test {
	[TestClass]
	public class DropBoxClientTests_User {
		private readonly DropBoxClient _client;

		public DropBoxClientTests_User( ) {
			this._client = new DropBoxClient( TestVariables.DropBoxApiKey, TestVariables.DropBoxApiSecret );
		}

		[TestMethod]
		public void Test_CanGetAccessToken( ) {
			var response = this._client.GetAccessToken( TestVariables.DropBoxUserCode, "http://localhost" ).Result;

			Assert.IsNotNull( response );
			Assert.IsNotNull( response.Token );
			Assert.IsNotNull( response.Uid );
		}

		[TestMethod]
		public void Test_BuildAutorizeUrl_ThrowNullException( ) {
			try {
				this._client.BuildAuthorizeUrl( string.Empty );

				Assert.Fail( );
			} catch( ArgumentNullException ane ) {
				Assert.IsNotNull( ane );
			} catch( Exception ) {
				Assert.Fail( );
			}
		}

		[TestMethod]
		public void Can_Get_AccountInfo( ) {
			this._client.UserLogin = new DropBoxUser( TestVariables.DropBoxToken, TestVariables.DropBoxUid );

			var accountInfo = this._client.AccountInfo( ).Result;

			Assert.IsNotNull( accountInfo );
			Assert.IsNotNull( accountInfo.display_name );
			Assert.IsNotNull( accountInfo.uid );
		}

		[TestMethod]
		public void BuildOAuth2AuthorizationUrl_RedirectUriIsRequired( ) {
			try {
				this._client.BuildAuthorizeUrl( OAuth2AuthorizationFlow.Code, null );
				Assert.Fail( );
			} catch( ArgumentNullException ane ) {
				Assert.IsNotNull( ane );
			} catch( Exception ) {
				Assert.Fail( );
			}
		}

		[TestMethod]
		public void BuildOAuth2AuthorizationUrl_CodeFlow_NoState( ) {
			this.TestOAuth2AuthorizationUrl( OAuth2AuthorizationFlow.Code, "code" );
		}

		[TestMethod]
		public void BuildOAuth2AuthorizationUrl_CodeFlow_WithState( ) {
			this.TestOAuth2AuthorizationUrl( OAuth2AuthorizationFlow.Code, "code", "foobar" );
		}

		[TestMethod]
		public void BuildOAuth2AuthorizationUrl_TokenFlow_NoState( ) {
			this.TestOAuth2AuthorizationUrl( OAuth2AuthorizationFlow.Token, "token" );
		}

		[TestMethod]
		public void BuildOAuth2AuthorizationUrl_TokenFlow_WithState( ) {
			this.TestOAuth2AuthorizationUrl( OAuth2AuthorizationFlow.Token, "token", "foobar" );
		}

		private void TestOAuth2AuthorizationUrl( OAuth2AuthorizationFlow oAuth2AuthorizationFlow, string expectedResponseType, string state = null ) {
			var actual = this._client.BuildAuthorizeUrl( oAuth2AuthorizationFlow, "http://example.com", state );
			Assert.IsNotNull( actual );


			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "response_type", expectedResponseType ),
				new KeyValuePair<string, string>( "client_id", TestVariables.DropBoxApiKey ),
				new KeyValuePair<string, string>( "redirect_uri", "http://example.com" )
			};

			if( !string.IsNullOrWhiteSpace( state ) ) {
				queryParams.Add( new KeyValuePair<string, string>( "state", state ) );
			}
			const string expectedFormat = "https://api.dropbox.com/1/oauth2/authorize";
			var expected = expectedFormat + queryParams.ToQueryString( );
			Assert.AreEqual( expected, actual );
		}
	}
}