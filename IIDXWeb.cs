using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Sgml;
using Japanese.Text.Encoding;

namespace Tatsuya.IIDX
{
	public class IIDXWeb
	{
		private const string URL_LOGIN = "https://p.eagate.573.jp/gate/p/login.html";
		private const string URL_STATUS = "http://p.eagate.573.jp/game/2dx/21/p/djdata/status.html";
		private const string URL_BASE = "http://p.eagate.573.jp";

		private static readonly Regex REG_PLAY_COUNT = new Regex("^(?<cnt>[0-9]+) *回$");

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

		public static async Task<IIDXStatus> GetDjStatus( string _ID, string _Password )
		{
			var status = new IIDXStatus();
			var ENC = new SjisEncoding();
			var handler = new HttpClientHandler{ UseCookies = true, AllowAutoRedirect = false };

			using ( var httpClient = new HttpClient(handler){ BaseAddress = new Uri( URL_BASE ) }  )
			{
				var content = new FormUrlEncodedContent( new Dictionary<string, string>
					{	
						{"KID", _ID},
						{"pass", _Password},
						{"OTP", string.Empty},
					} );

				await httpClient.PostAsync( URL_LOGIN, content );
				var response = await httpClient.GetStreamAsync( URL_STATUS );

				using (var reader = new StreamReader(response, ENC))
				{
					int num;
					Match match;

					var xml = ParseHtml( reader );
					var ns = xml.Root.Name.Namespace;
					var xmlStatusDetails = xml
						.Descendants(ns + "table")
						.Where(x => x.Attribute("class").Value == "dj_data_table")
						.Descendants(ns + "tr")
						.ToArray();

					Func<string, string> GetStatusDetails = setting =>
					{
						var element = xmlStatusDetails.Where(x => x.Element(ns + "th").Value == setting).FirstOrDefault();

						return element != default( XElement ) ? element.Element( ns + "td" ).Value : null;
					};

					status.DjName = GetStatusDetails("DJ NAME");
					status.AffiliationState = GetStatusDetails("所属都道府県");
					status.IIDXID = GetStatusDetails("IIDX ID");
					status.AffiliationStore = GetStatusDetails("所属店舗");
					if (int.TryParse (GetStatusDetails ("所持デラー"), out num)) {
						status.PossessionDeller = num;
					}
					match = REG_PLAY_COUNT.Match (GetStatusDetails ("プレー回数"));
					if (match.Success) {
						if (int.TryParse(match.Groups ["cnt"].Value, out num )) {
							status.PlayCount = num;
						}
					}

					var xmlDjStatus = xml
						.Descendants(ns + "table")
						.Where(x => x.Attribute("class").Value == "status_type1")
						.Descendants(ns + "tr")
						.ToArray();

					Func<string, Func<XElement, bool>, int, string[]> GetDjStatus = (setting, selector, skipNum ) =>
					{
						return xmlDjStatus
								.Where(x => x.Element(ns + "th").Value == setting)
								.First()
								.Elements(ns + "td")
								.Where(selector)
								.Skip(skipNum)
								.Select(x => x.Value)
								.ToArray();
					};

					var djPoints = GetDjStatus("DJ POINT", x => x.Attribute("class").Value == "point", 1);

					status.DjPointSP = djPoints[0];
					status.DjPointDP = djPoints[1];

					var djRanks = GetDjStatus("段位認定", x => x.Attribute("class").Value == "point", 0);

					status.DjRankSP = djRanks[0];
					status.DjRankDP = djRanks[1];

					var djPilgrimages = GetDjStatus("行脚王", x => true, 0);

					status.Areas = djPilgrimages[0];
					status.Stores = djPilgrimages[1];
					status.Statnds = djPilgrimages[2];

					status.Comment = xml
						.Descendants(ns + "table")
						.Where(x => x.Attribute("class").Value == "status_type1 table_style1")
						.Descendants(ns + "tr")
						.Descendants(ns + "td")
						.Where(x => x != null && x.Attribute("class").Value == "comment_Box")
						.Select(x => x.Value)
						.First();

					var qproPath = xml
						.Descendants(ns + "table")
						.Where(x => x.Attribute("class").Value == "dj_qpro_table")
						.Descendants(ns + "tr")
						.Descendants(ns + "td")
						.Descendants(ns + "a")
						.Select(x => x.Element(ns + "img").Attribute("src").Value)
						.First();
					status.Qpro = await httpClient.GetByteArrayAsync(new Uri(URL_BASE + qproPath));
				}
			}

			return status;
		}
	}
}

