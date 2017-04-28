using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RWT.Spritz;
using Microsoft.Extensions.CommandLineUtils;

namespace SpritzCmd {

sealed class CryptCmd {
	private readonly SemaphoreSlim semaphore;
	private readonly String password;

	CryptCmd(Int32 jobs, String pw) {
		semaphore = new SemaphoreSlim(jobs);
		password = pw;
	}

	private async Task LimitedReq(String fn, Func<String,Task> req) {
		await semaphore.WaitAsync().ConfigureAwait(false);

		try {
			await req(fn);
		} finally {
			semaphore.Release();
		}
		
	}

	private Task LimitedDecryptReq(String fn) => LimitedReq(fn, this.DecryptStreamAsync);
	private Task LimitedEncryptReq(String fn) => LimitedReq(fn, this.EncryptStreamAsync);
	private Task LimitedCheckReq(String fn) => LimitedReq(fn, this.CheckStreamAsync);

	private async Task CheckStreamAsync(String fn) {
		using(var ifl = new FileStream(fn, FileMode.Open, FileAccess.Read)) {
			try {
				var origFN = await Crypto.CheckAsync(password, ifl);
				Console.WriteLine("{0} ok. Original filename was: <{1}>",
					fn,
					origFN);
				
			} catch(ArgumentException e) {
				Console.WriteLine(fn + " error: " + e.Message);
			}
		}
	}

	private async Task DecryptStreamAsync(String fn) {
		var oflName = Path.GetFileName(fn) + ".decrypted";
		String origFN = "";

		using(var ifl = new FileStream(fn, FileMode.Open, FileAccess.Read)) {
			using(var ofl = new FileStream(oflName, FileMode.Create, FileAccess.Write)) {
				Console.WriteLine("{0} decrypting...", fn);
				try {
					origFN = await Crypto.DecryptAsync(password, ifl, ofl);
				} catch(ArgumentException e) {
					Console.WriteLine(fn + " error: " + e.Message);
				}
			}
		}

		if(origFN != "") {
			File.Move(oflName, origFN);
		}
	}

	private async Task EncryptStreamAsync(String fn) {
		using(var ifl = new FileStream(fn, FileMode.Open, FileAccess.Read)) {
			var iflName = Path.GetFileName(fn);
			using(var ofl = new FileStream(iflName + ".dat", FileMode.Create, FileAccess.Write)) {
				Console.WriteLine("{0} encrypting...", fn);
				await Crypto.EncryptAsync(password, iflName, ifl, ofl);
			}
		}
	}

	public static void Configure(CommandLineApplication app) {
		app.HelpOption("-h|--help|-?");
		var jobsOp = app.Option("-j|--jobs", 
			"The number of concurrent files to run (default 8)", 
			CommandOptionType.SingleValue);
		var passOp = app.Option("-p|--password",
			"The password to use",
			CommandOptionType.SingleValue);
		var chkOp = app.Option("-c|--check",
			"Only check the password of a file.",
			CommandOptionType.NoValue);
		var decOp = app.Option("-d|--decrypt",
			"Decrypt instead of encrypt",
			CommandOptionType.NoValue);
		app.OnExecute( () => {
			try {
				var jobs = jobsOp.HasValue()? Int32.Parse(jobsOp.Value()) : 8;
				var encrypting = !decOp.HasValue() && !chkOp.HasValue();
				var pw = passOp.HasValue() ? 
					passOp.Value() : 
					PasswordReader.Read("Password: ", encrypting);
				var c = new CryptCmd(jobs,pw);	

				Func<String,Task> taskFunc = c.LimitedEncryptReq;
				if(chkOp.HasValue()) {
					taskFunc = c.LimitedCheckReq;
				} else if(decOp.HasValue()) {
					taskFunc = c.LimitedDecryptReq;
				}
				
				var tasks = app.RemainingArguments
					.Select( name => taskFunc(name) )
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
