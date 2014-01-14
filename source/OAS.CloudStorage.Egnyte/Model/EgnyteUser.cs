namespace OAS.CloudStorage.Egnyte.Models {
	public class EgnyteUser {
		public string Domain { get; private set; }
		public string Token { get; private set; }

		public EgnyteUser( string domain, string token ) {
			this.Domain = domain;
			this.Token = token;
		}
	}
}