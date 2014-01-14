#region Using Statements

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace OAS.CloudStorage.Egnyte {
	internal class RetryOnceOnErrorHandler : HttpClientHandler {
		protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken ) {
			var retries = 0;

			HttpResponseMessage response = null;
			while( retries <= 1 ) {
				response = await base.SendAsync( request, cancellationToken );
				if( response.StatusCode == HttpStatusCode.Forbidden ) {
					var error = await response.Content.ReadAsStringAsync( );
					if( error == "<h1>Developer Over Qps</h1>" ) {
						//sleep for a second and try once more
						Thread.Sleep( 1000 );
						retries++;
						continue;
					}
				}

				break;
			}

			return response;
		}
	}
}