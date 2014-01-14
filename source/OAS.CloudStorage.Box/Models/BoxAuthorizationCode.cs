namespace OAS.CloudStorage.Box.Models {
	public class BoxAuthorizationCode {
		public string Code { get; private set; }

		public BoxAuthorizationCode( string code ) {
			this.Code = code;
		}
	}
}
