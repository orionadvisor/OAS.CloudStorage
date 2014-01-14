using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OAS.CloudStorage.Core {
	public static class ExtensionMethods {
		public static string ToQueryString( this IEnumerable<KeyValuePair<string, string>> keys, bool alwaysIncludeQuestionMark = false ) {
			var array = keys.Select( k => string.Format( "{0}={1}",
				HttpUtility.UrlEncode( k.Key ),
				HttpUtility.UrlEncode( k.Value ) )
				).ToArray( );
			if( array.Any( ) || alwaysIncludeQuestionMark ) {
				return "?" + string.Join( "&", array );
			}
			return string.Empty;
		}

		public static string GetFileSizeInHumanReadableFormat( long sizeInBytes ) {
			double convertedSize = (double) sizeInBytes;
			string [] sizeNames = { "B", "KB", "MB", "GB", "TB", "PB" };
			int position;

			for( position = 0; ( convertedSize <= 1024 ) && ( position + 1 ) < sizeNames.Length; ++position )
				convertedSize /= 1024.0;

			return string.Format( "{0:0.##}{1}", convertedSize, sizeNames[ position ] );
		}
	}
}
