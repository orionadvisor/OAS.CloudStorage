namespace OAS.CloudStorage.DropBox.CoreApi.Models {
	public class DropBoxUser {
		public string Token { get; private set; }
		public string Uid { get; private set; }

		public DropBoxUser( string token, string uid ) {
			this.Token = token;
			this.Uid = uid;
		}
	}
}