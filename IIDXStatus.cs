using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tatsuya.IIDX
{
	public class IIDXStatus
	{
		public string DjName { get; internal set; }
		public string AffiliationState { get; internal set; }
		public string IIDXID { get; internal set; }
		public string AffiliationStore { get; internal set; }
		public int PossessionDeller { get; internal set; }
		public int PlayCount { get; internal set; }
		public string DjPointSP { get; internal set; }
		public string DjPointDP { get; internal set; }
		public string DjRankSP { get; internal set; }
		public string DjRankDP { get; internal set; }
		public string Areas { get; internal set; }
		public string Stores { get; internal set; }
		public string Statnds { get; internal set; }
		public string Comment { get; internal set; }
		public byte[] Qpro { get; internal set; }
	}
}
