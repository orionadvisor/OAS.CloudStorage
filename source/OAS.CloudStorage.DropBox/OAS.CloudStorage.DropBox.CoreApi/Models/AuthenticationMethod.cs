namespace OAS.CloudStorage.DropBox.CoreApi.Models {
	/// <summary>
	/// Dropbox supports versions 1 and 2 of the OAuth spec.
	/// </summary>
	public enum AuthenticationMethod {
		/// <summary>
		/// OAuth1 is the 'standard' authentication mode for Dropbox. For more information see https://www.dropbox.com/developers/core/docs#request-token
		/// </summary>
		//OAuth1,
		/// <summary>
		/// OAuth2 support in Dropbox was implemented in 2013. For more information see https://www.dropbox.com/developers/core/docs#oa2-authorize
		/// </summary>
		OAuth2
	}
}