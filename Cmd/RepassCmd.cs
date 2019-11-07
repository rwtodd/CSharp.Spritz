using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RWTodd.Spritz.Algorithm;
using Microsoft.Extensions.CommandLineUtils;

namespace RWTodd.Spritz.Cmd {

sealed class RepassCmd {
	private readonly SemaphoreSlim semaphore;
	private readonly String password;
	private readonly String newpassword;

	RepassCmd(Int32 jobs, String pw, String npw) {
		semaphore = new SemaphoreSlim(jobs);
		password = pw;
		newpassword = npw;
	}

	private async Task LimitedReq(String fn) {
		await semaphore.WaitAsync().ConfigureAwait(false);

		try {
			await RepassAsync(fn);
		} finally {
			semaphore.Release();
		}
		
	}

	private async Task RepassAsync(String fn) {
		using(var ifl = new FileStream(fn, FileMode.Open, FileAccess.ReadWrite)) {
			try {
				await Crypto.RepassAsync(password, newpassword, ifl);
				Console.WriteLine("{0} ok.", fn);
			} catch(ArgumentException e) {
				Console.WriteLine(fn + " error: " + e.Message);
			}
		}
	}

	public static void Configure(CommandLineApplication app) {
		app.HelpOption("-h|--help|-?");
		var jobsOp = app.Option("-j|--jobs", 
			"The number of concurrent files to run (default 8)", 
			CommandOptionType.SingleValue);
		var opassOp = app.Option("-op|--oldpassword",
			"The old password",
			CommandOptionType.SingleValue);
		var npassOp = app.Option("-np|--newpassword",
			"The new password to use",
			CommandOptionType.SingleValue);
		app.OnExecute( () => {
			try {
				
				var jobs = jobsOp.HasValue()? Int32.Parse(jobsOp.Value()) : 8;
				var opw = opassOp.HasValue() ? 
					opassOp.Value() : 
					PasswordReader.Read("Old Password: ", false);
				var npw = npassOp.HasValue() ? 
					npassOp.Value() : 
					PasswordReader.Read("New Password: ", true);
				var c = new RepassCmd(jobs,opw,npw);

				var tasks = app.RemainingArguments
					.Select( name => c.LimitedReq(name) )
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
