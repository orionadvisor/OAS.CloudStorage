#region Using Statements

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace OAS.CloudStorage.Core.Test {
	internal static class FileFactory {
		public static FileInfo MakeFile( string extension = null, long? length = null ) {
			var rand = new Random( );
			if( length == null ) {
				length = rand.Next( 50, 1048576 );
			}

			var filename = Guid.NewGuid( ).ToString( );
			if( extension != null ) {
				if( extension.StartsWith( "." ) ) {
					filename += extension;
				} else {
					filename += "." + extension;
				}
			}
			var localFile = new FileInfo( filename );

			var data = new byte[ length.Value ];

			rand.NextBytes( data );

			File.WriteAllBytes( localFile.FullName, data );

			var fInfo = new FileInfo( localFile.FullName );

			Assert.IsTrue( fInfo.Exists );

			return fInfo;
		}

		public static FileInfo MakeImage( ImageFormat imageFormat, int? width = null, int? height = null ) {
			var rand = new Random( );
			if( !width.HasValue ) {
				width = rand.Next( 10, 50 );
			}
			if( !height.HasValue ) {
				height = rand.Next( 10, 50 );
			}

			var bmp = new Bitmap( width.Value, height.Value );
			for( int i = 0; i < bmp.Height; i++ ) {
				for( int j = 0; j < bmp.Width; j++ ) {
					// Randomly add color
					bmp.SetPixel( j, i, rand.NextColor( ) );
				}
			}

			var filename = Guid.NewGuid( ).ToString( );
			var localFile = new FileInfo( filename );

			bmp.Save( localFile.FullName, imageFormat );

			var fInfo = new FileInfo( localFile.FullName );

			Assert.IsTrue( fInfo.Exists );

			return fInfo;
		}

		public static Color NextColor( this Random random ) {
			return Color.FromArgb( random.Next( 0, 255 ), random.Next( 0, 255 ), random.Next( 0, 255 ) );
		}

		/// <summary>
		/// Splits any generic array (that implements <see cref="IEnumerable{T}"/>) into a generic array of generic arrays.
		/// </summary>
		/// <typeparam name="T">The type of the array.</typeparam>
		/// <param name="source">The source array.</param>
		/// <param name="partitions">The number of partitions to create.</param>
		/// <returns>A generic array of generic arrays.</returns>
		/// <remarks>adapted from http://stackoverflow.com/questions/438188/split-a-collection-into-n-parts-with-linq </remarks>
		public static IEnumerable<IEnumerable<T>> Split<T>( this IEnumerable<T> source, int partitions ) {
			int i = 0;
			int total = source.Count( );
			var splits = from name in source
				group name by Math.Floor( (double) i++ / total * partitions )
				into part
				select part.AsEnumerable( );
			return splits;
		}
	}
}