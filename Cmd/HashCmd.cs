using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RWT.Spritz;
using Microsoft.Extensions.CommandLineUtils;

namespace SpritzCmd {

enum HashFormat { BASE64, HEX };

sealed class HashCmd {
	private readonly SemaphoreSlim semaphore;
	private readonly Int32 hashSize;
	private readonly HashFormat fmt;

	HashCmd(Int32 jobs, Int32 szBits, HashFormat hf) {
		semaphore = new SemaphoreSlim(jobs);
		hashSize = (szBits + 7) / 8;
		fmt = hf;
	}

	private async Task LimitedHashReq(String fn) {
		await semaphore.WaitAsync().ConfigureAwait(false);
		try {
			await HashStreamAsync(fn);
		} finally {
			semaphore.Release();
		}
	}

	private String DisplayHash(Byte[] hash) {
		switch(fmt) {
			case HashFormat.BASE64:
				return Convert.ToBase64String(hash);
			case HashFormat.HEX:
				var sb = new StringBuilder(hash.Length*2);
				foreach(var b in hash) {
					sb.AppendFormat("{0:x2}",b);
				}				
				return sb.ToString();
		}
		throw new Exception("BOGUS");
	}

	private async Task HashStreamAsync(String fn) {
		var result = new Byte[hashSize];
		using( var fl = new FileStream(fn, FileMode.Open, FileAccess.Read)) {
			await Hash.OfStreamAsync(result,fl);
			Console.WriteLine("{0}: {1}", fn, DisplayHash(result));
		}
	}

	public static void Configure(CommandLineApplication app) {
		app.HelpOption("-h|--help|-?");
		var jobsOp = app.Option("-j|--jobs", 
			"The number of concurrent hashes to compute (default 8)", 
			CommandOptionType.SingleValue);
		var szOp = app.Option("-s|--size",
			"The size of the hash in bits (default 256)",
			CommandOptionType.SingleValue);
		var fmtOp = app.Option("--hex",
			"Display output in hex digits (default base64)",
			CommandOptionType.NoValue);
		app.OnExecute( () => {
			try {
				var jobs = jobsOp.HasValue()? Int32.Parse(jobsOp.Value()) : 8;
				var szBits = szOp.HasValue()? Int32.Parse(szOp.Value()) : 256;
				var fmt = fmtOp.HasValue() ? HashFormat.HEX : HashFormat.BASE64;
				var h = new HashCmd(jobs,szBits,fmt);	

				var tasks = app.RemainingArguments
					.Select( name => h.LimitedHashReq(name) )
					.ToArray();
				Task.WaitAll(tasks);

			} catch(Exception e) {
				Console.WriteLine(e);
				return 1;
			}
			return 0;
		});
	}
}

} // end namespace
