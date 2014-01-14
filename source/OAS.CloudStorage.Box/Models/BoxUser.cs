namespace OAS.CloudStorage.Box.Models {
	public class BoxUser {
		public string AccessToken { get; private set; }
		public string RefreshToken { get; private set; }

		public BoxUser( string accessToken, string refreshToken ) {
			this.AccessToken = accessToken;
			this.RefreshToken = refreshToken;
		}
	}
}
