using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace RWTodd.Spritz.Algorithm {

public class Hash {
	public static void OfCipher(Byte[] tgt, Cipher src) {
		src.AbsorbStop();
		src.AbsorbNumber(tgt.Length);
		src.Squeeze(tgt);		
	}

	public static void OfBytes(Byte[] tgt, Byte[] src) {
		var c = new Cipher();
		c.Soak(src);
		OfCipher(tgt,c);
	}

	public static void OfEnumerable(Byte[] tgt, IEnumerable<Byte> src) {
		var c = new Cipher();
		c.Soak(src);
		OfCipher(tgt,c);
	}

	public static void OfUTF8String(Byte[] tgt, String src) =>
		OfBytes(tgt, System.Text.Encoding.UTF8.GetBytes(src));		


	public static async Task OfStreamAsync(Byte[] tgt, Stream strm) {
		var c = new Cipher();
		var buff = new Byte[8192];
		var buff2 = new Byte[8192];
		Task<int> theRead = strm.ReadAsync(buff,0,8192);
		while(true) {
			var amt = await theRead;
			if(amt == 0) break;
			Byte[] tmp = buff;
			buff = buff2;
			buff2 = tmp;
			theRead = strm.ReadAsync(buff, 0, 8192);
			c.Soak(buff2, amt);
		}
		OfCipher(tgt,c);
	}
}

} // end namespace
