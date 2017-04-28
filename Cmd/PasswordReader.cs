using System;
using System.Text;

namespace SpritzCmd {

sealed class PasswordReader {
	internal static String Read(String prompt, bool repeat) {
		Console.Write(prompt);
		String pw = ReadOnePw();
		if(repeat) {
			Console.Write("[verify] " + prompt);
			String pw2 = ReadOnePw();
			if(pw != pw2) {
				Console.WriteLine("Passwords don't match!");
				return Read(prompt, repeat);
			}
		}
		return pw;
	}

	private static String ReadOnePw() {
		var password = new StringBuilder();
		ConsoleKeyInfo info = Console.ReadKey(true);
		while (info.Key != ConsoleKey.Enter) {
			if (info.Key != ConsoleKey.Backspace) {
				Console.Write("*");
				password.Append(info.KeyChar);
			}
			else if (info.Key == ConsoleKey.Backspace) {
				if (password.Length > 0) {
					// remove one character from the list of password characters
					password.Length = password.Length - 1;
					// get the location of the cursor
					int pos = Console.CursorLeft;
					// move the cursor to the left by one character
					Console.SetCursorPosition(pos - 1, Console.CursorTop);
					// replace it with space
					Console.Write(" ");
					// move the cursor to the left by one character again
					Console.SetCursorPosition(pos - 1, Console.CursorTop);
				}
			}
			info = Console.ReadKey(true);
		}

		// add a new line because user pressed enter at the end of their password
		Console.WriteLine();
		return password.ToString();
	}
}

} // end namespace