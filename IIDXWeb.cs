using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Sgml;
using Japanese.Text.Encoding;

namespace Tatsuya.IIDX
{
	public class IIDXWeb
	{
		public IIDXWeb ()
		{
		}

		private static XDocument ParseHtml( TextReader _Reader )
		{
			using ( var sgmlReader = new SgmlReader {
				DocType = "HTML",
				CaseFolding = CaseFolding.ToLower,
				InputStream = _Reader, } )
			{
				return XDocument.Load( sgmlReader );
			}
		}

		public static async Task<Tuple<string, string>> Test( string _ID, string _Password )
		{
			const string URL_RED = @"?___REDIRECT=1";
			const string URL_LOGIN = @"https://p.eagate.573.jp/gate/p/login.html";
			const string URL_STATUS = @"http://p.eagate.573.jp/game/2dx/21/p/djdata/status.html";
			const string URL_MUSIC = @"http://p.eagate.573.jp/game/2dx/21/p/djdata/music.html?list=20&play_style=0&rival=&s=1&page=1#musiclist";

			Tuple<string, string> result = null;
			string djName = null;
			string param = string.Format( @"?KID={0}&pass={1}&OTP=", _ID, _Password );

			Encoding ENC = new SjisEncoding();

			try {
				using( var httpClient = new HttpClient() )
				{
					await httpClient.GetStreamAsync( URL_LOGIN + param );
					var status = await httpClient.GetStreamAsync( URL_STATUS );
					var music = await httpClient.GetStreamAsync( URL_MUSIC );

					using ( var reader = new StreamReader( status, ENC ) )
					{
						var xml = ParseHtml( reader );
						var ns = xml.Root.Name.Namespace;
						var res = xml
							.Descendants( ns + "table" )
							.Where( x => x.Attribute( "class" ).Value == "dj_data_table" )
							.Descendants( ns + "tr" )
							.Where( x => x.Element( ns + "th" ).Value == "DJ NAME" )
							.FirstOrDefault()
							;

						if ( res != default( XElement ) )
						{
							djName = res.Element( ns + "td" ).Value;
						}

						result = Tuple.Create( xml.ToString(), djName );
					}
				}
			} catch(Exception) {
			}

			return result;
		}
	}
}
