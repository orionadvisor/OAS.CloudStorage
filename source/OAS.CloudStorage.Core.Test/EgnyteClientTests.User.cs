#region Using Statements

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OAS.CloudStorage.Egnyte;

#endregion

namespace OAS.CloudStorage.Core.Test {
	[TestClass]
	public class EgnyteClientTests_User {
		private readonly EgnyteClient _client;

		public EgnyteClientTests_User( ) {
			this._client = new EgnyteClient( TestVariables.EgnyteApiDomain, TestVariables.EgnyteApiKey );
		}

		/*[TestMethod]
		public void Test_CanBuildAutorizeUrl( ) {
			var authorizeUrl = this._client.BuildAuthorizeUrl( new UserLogin {
				Secret = TestVariables.Secret,
				Token = TestVariables.Token
			} );

			Assert.IsNotNull( authorizeUrl );
		}*/

		[TestMethod]
		public void Test_BuildAutorizeUrl_ThrowNullException( ) {
			try {
				this._client.BuildAuthorizeUrl( null );

				Assert.Fail( );
			} catch( ArgumentNullException ane ) {
				Assert.IsNotNull( ane );
			} catch( Exception ) {
				Assert.Fail( );
			}
		}

		/*[TestMethod]
		public void Can_Get_AccountInfo( ) {
			this._client.UserLogin = new UserLogin {
				Token = TestVariables.Token,
				Secret = TestVariables.Secret
			};

			var accountInfo = this._client.AccountInfo( ).Result;

			Assert.IsNotNull( accountInfo );
			Assert.IsNotNull( accountInfo.display_name );
			Assert.IsNotNull( accountInfo.uid );
		}*/

		[TestMethod]
		public void BuildOAuth2AuthorizationUrl_RedirectUriIsRequired( ) {
			try {
				this._client.BuildAuthorizeUrl( null );
				Assert.Fail( );
			} catch( ArgumentNullException ane ) {
				Assert.IsNotNull( ane );
			} catch( Exception ) {
				Assert.Fail( );
			}
		}

		[TestMethod]
		public void BuildOAuth2AuthorizationUrl_TokenFlow( ) {
			this.TestOAuth2AuthorizationUrl( );
		}

		[TestMethod]
		public void BuildOAuth2AuthorizationUrl_TokenFlow_Mobile( ) {
			this.TestOAuth2AuthorizationUrl( true );
		}

		[TestMethod]
		public void BuildOAuth2AuthorizationUrl_TokenFlow_NotMobile( ) {
			this.TestOAuth2AuthorizationUrl( false );
		}

		private void TestOAuth2AuthorizationUrl( bool? mobile = null ) {
			var actual = this._client.BuildAuthorizeUrl( "http://example.com", mobile );
			Assert.IsNotNull( actual );

			var queryParams = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>( "client_id", TestVariables.EgnyteApiKey ),
				new KeyValuePair<string, string>( "redirect_uri", "http://example.com" ),
			};
			if( mobile.HasValue ) {
				queryParams.Add( new KeyValuePair<string, string>( "mobile", ( mobile.Value ? 1 : 0 ).ToString( ) ) );
			}

			const string expectedFormat = "https://{0}.egnyte.com/puboauth/token{1}";
			var expected = string.Format( expectedFormat, TestVariables.EgnyteApiDomain, queryParams.ToQueryString( ) );
			Assert.AreEqual( expected, actual );
		}
	}
}