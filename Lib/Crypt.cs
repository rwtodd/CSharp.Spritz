using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RWT.Spritz
{

public class Crypto {
	public static async Task EncryptAsync(
			String pw, 
			String origFn, 
			Stream instr,
			Stream outstr) {

		if(!instr.CanRead) {
			throw new ArgumentException("Can't read instream");
		}
		if(!outstr.CanWrite) {
			throw new ArgumentException("Can't write outstream");
		}

		var h = new Header();
		Byte[] hdrBytes = null;
		await Task.Run( () => hdrBytes = h.Generate(pw) );
		await outstr.WriteAsync(hdrBytes,0,h.Length);

		var c = h.MakeCipher();

		var fnLen = new Byte[1] { (byte)origFn.Length };
		c.SqueezeXOR(fnLen);

		await outstr.WriteAsync(fnLen, 0, 1);
		if(origFn.Length > 0) {
			var fnBytes = Encoding.UTF8.GetBytes(origFn);
			c.SqueezeXOR(fnBytes);
			await outstr.WriteAsync(fnBytes, 0, fnBytes.Length);
		}

		var buff = new Byte[8192];
		var buff2 = new Byte[8192];
		Task<int> theRead = instr.ReadAsync(buff, 0, 8192);
		Task theWrite = null;
		while(true) {
			var amt = await theRead;
			if(amt == 0) { break; }

			var tmp = buff;
			buff = buff2;
			buff2 = tmp;

			if(theWrite != null) { await theWrite; }
			theRead = instr.ReadAsync(buff, 0, 8192);

			c.SqueezeXOR(buff2,0,amt);
			theWrite = outstr.WriteAsync(buff2,0,amt);
		}
		if(theWrite != null) { await theWrite; }

	}

	private static async Task ReadFullyAsync(Stream s, Byte[] arr) {
		var remains = arr.Length;
		
		while(remains > 0) {
			var amt  = await s.ReadAsync(arr, arr.Length - remains, remains);
			if(amt == 0) throw new Exception("File too short");
			remains -= amt;
		}
	}

	public static async Task<String> CheckAsync(
			String pw, 
			Stream instr) {
		if(!instr.CanRead) {
			throw new ArgumentException("Can't read instream");
		}
		var origFn = "";

		var h = new Header();
		var hdrBytes = new Byte[h.Length];
		await ReadFullyAsync(instr, hdrBytes);
		await Task.Run( () =>  h.Parse(pw, hdrBytes) );

		var c = h.MakeCipher();

		var fname = new Byte[1];
		await instr.ReadAsync(fname,0,1);
		c.SqueezeXOR(fname);
		if(fname[0] != 0) {
			fname = new Byte[fname[0]];
			await ReadFullyAsync(instr,fname);
			c.SqueezeXOR(fname);
			origFn = Encoding.UTF8.GetString(fname);
		}

		return origFn;
	}

	/// <summary>Change the password on the encrypted
	/// stream.</summary>
	public static async Task RepassAsync(
			String oldpw,
			String newpw,
			Stream iostr) {
		if(!(iostr.CanRead && iostr.CanWrite && iostr.CanSeek)) {
			throw new ArgumentException("stream must be readable, writable, and seekable");
		}

		// now... read the header...
		var h = new Header();
		var hdrBytes = new Byte[h.Length];
		await ReadFullyAsync(iostr, hdrBytes);
		await Task.Run( () => { 
				// parse the original header, with he oldpw
				h.Parse(oldpw, hdrBytes); 
				// regenerate the header, with the same key but newpw
				h.IV = null; // force a new IV because why not?
				hdrBytes = h.Generate(newpw);
			} );

		iostr.Seek(0, SeekOrigin.Begin);
		await iostr.WriteAsync(hdrBytes,0,h.Length);
	}

	public static async Task<String> DecryptAsync(
			String pw, 
			Stream instr,
			Stream outstr) {
		if(!instr.CanRead) {
			throw new ArgumentException("Can't read instream");
		}
		if(!outstr.CanWrite) {
			throw new ArgumentException("Can't write outstream");
		}
		var origFn = "";

		var h = new Header();
		var hdrBytes = new Byte[h.Length];
		await ReadFullyAsync(instr, hdrBytes);
		await Task.Run( () =>  h.Parse(pw, hdrBytes) );

		var c = h.MakeCipher();

		var fname = new Byte[1];
		await instr.ReadAsync(fname,0,1);
		c.SqueezeXOR(fname);
		if(fname[0] != 0) {
			fname = new Byte[fname[0]];
			await ReadFullyAsync(instr,fname);
			c.SqueezeXOR(fname);
			origFn = Encoding.UTF8.GetString(fname);
		}

		var buff = new Byte[8192];
		var buff2 = new Byte[8192];
		Task<int> theRead = instr.ReadAsync(buff, 0, 8192);
		Task theWrite = null;
		while(true) {
			var amt = await theRead;
			if(amt == 0) { break; }

			var tmp = buff;
			buff = buff2;
			buff2 = tmp;

			if(theWrite != null) { await theWrite; }
			theRead = instr.ReadAsync(buff, 0, 8192);

			c.SqueezeXOR(buff2,0,amt);
			theWrite = outstr.WriteAsync(buff2,0,amt);
		}
		if(theWrite != null) { await theWrite; }

		return origFn;
	}
}

} // End namespace
