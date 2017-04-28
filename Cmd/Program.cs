using System;
using Microsoft.Extensions.CommandLineUtils;

namespace SpritzCmd {

class Program {
	static void Main(string[] args) {
		var cmd  = new CommandLineApplication(throwOnUnexpectedArg: true);

		cmd.HelpOption("-h|--help|-?");
		cmd.Command("hash", HashCmd.Configure, false);
		cmd.Command("crypt", CryptCmd.Configure, false);
		cmd.Command("repass", RepassCmd.Configure, false);
		Int32 rval = 0;
		try {
			rval = cmd.Execute(args);
		} catch (Exception e) {
			Console.WriteLine(e);
			rval = 1;
		}

		if(rval != 0) {
			Environment.Exit(rval);
		}

	}
}

}  // end namespace
