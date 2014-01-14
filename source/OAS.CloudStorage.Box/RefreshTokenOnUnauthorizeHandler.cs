#region Using Statements

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace OAS.CloudStorage.Box {
	internal class RefreshTokenOnUnauthorizeHandler : HttpClientHandler {
		private readonly Func<AuthenticationHeaderValue> _refreshToken;

		public RefreshTokenOnUnauthorizeHandler( Func<AuthenticationHeaderValue> refrestToken ) {
			_refreshToken = refrestToken;
		}

		protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken ) {
			var retries = 0;

			HttpResponseMessage response = null;
			while( retries <= 1 ) {
				response = await base.SendAsync( request, cancellationToken );
				if( response.StatusCode == HttpStatusCode.Unauthorized ) {
					var error = await response.Content.ReadAsStringAsync( );
					
					var newAuthentication = _refreshToken.Invoke( );
					request.Headers.Authorization = newAuthentication;

					retries++;
					continue;
				}

				break;
			}

			return response;
		}
	}
}