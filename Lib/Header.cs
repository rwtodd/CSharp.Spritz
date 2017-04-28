using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace RWT.Spritz
{

public class Header {
	// The header's information content is:
	public Byte[] IV { get; set; }
	public Byte[] Key { get; set; }

	public Header( ) { }

	// Obtain the length of the header, given the data 
	public Int32 Length {
		get {
			return 76;
		}
	}

	public Cipher MakeCipher() {
		var c = new Cipher();
		c.Soak(Key);
		c.Skip(2048 + Key[3]);	
		return c;
	}

	public void Parse(String pw, Byte[] b) { 
		if(b.Length != 76) { 
			throw new ArgumentException("header must be 76 bytes long");
		}

		// get the IV..
		IV = new Byte[4];
		Array.Copy(b,IV,4);
		var pwBytes = Encoding.UTF8.GetBytes(pw);
		var ivMask = new Byte[4];
		Hash.OfBytes(ivMask, pwBytes);
		Combine4(IV, ivMask);

		// set up the cipher to decrypt the header
		var headerCipher = new Cipher();
		headerCipher.Soak(KeyGen(pwBytes, IV));
		headerCipher.SqueezeXOR(b,4,4);
		headerCipher.Skip(b[7]);
		headerCipher.SqueezeXOR(b,8,68);

		// check the token...	
		var token = new ArraySegment<Byte>(b,4,4);
		var hashedToken = new Byte[4];
		Hash.OfEnumerable(hashedToken,token);
		if(!hashedToken.SequenceEqual(new ArraySegment<Byte>(b,8,4))) {
			throw new ArgumentException("bad password or corrupted stream");
		}
		
		// get the actual key...
		Key = new Byte[64];
		Array.Copy(b,12,Key,0,64);
	}

	/// <summary>Write the header to a freshly-allocated byte array</summary>
	public Byte[] Generate(String pw) {
		var result = new Byte[76];
	
		var rnd = new Random();
		if(IV == null) {
			IV = new Byte[4];
			rnd.NextBytes(IV);
		}

		if(Key == null) {
			Key = new Byte[64];
			rnd.NextBytes(Key);
		}		

		// set up the cipher
		var pwBytes = Encoding.UTF8.GetBytes(pw);
		var headerCipher = new Cipher();
		headerCipher.Soak(KeyGen(pwBytes, IV));

		// generate a hash of the pw, to mask the IV...
		var maskedIV = new Byte[4];
		Hash.OfBytes(maskedIV, pwBytes);
		Combine4(maskedIV, IV);

		// generate a random token, and its hash...
		var checkToken = new Byte[4];
		rnd.NextBytes(checkToken);
		var checkHash = new Byte[4];
		Hash.OfBytes(checkHash,checkToken);

		Int32 toSkip = checkToken[3]; // random skip amount...

		// encrypt the token and hash...
		headerCipher.SqueezeXOR(checkToken);
		headerCipher.Skip(toSkip);
		headerCipher.SqueezeXOR(checkHash);

		// encrypt the key...
		var copiedKey = new Byte[64];
		Array.Copy(Key,copiedKey, 64); 
		headerCipher.SqueezeXOR(copiedKey);

		// ok, now write everything out...
		Array.Copy(maskedIV,result,4);
		Array.Copy(checkToken,0,result,4,4);
		Array.Copy(checkHash,0,result,8,4);
		Array.Copy(copiedKey,0,result,12,64);

		return result; 
	}

	private static void Combine4(Byte[] tgt, Byte[] src) {
		tgt[0] ^= src[0];
		tgt[1] ^= src[1];
		tgt[2] ^= src[2];
		tgt[3] ^= src[3];
	}

	private static Byte[] KeyGen(Byte[] pwBytes, Byte[] iv) {
		var keyBytes = new Byte[64];
		Hash.OfBytes(keyBytes, pwBytes);

		var spritz = new Cipher();
		var iv2 = new Byte[4]; 
		Array.Copy(iv, iv2, 4);  // we will be mutating the IV...

		Int32 iterations = 20000 + iv[3];
		for(Int32 i = 0; i < iterations; ++i) {
			spritz.Reset();
			spritz.Soak(iv2);
			spritz.AbsorbStop();
			spritz.Soak(keyBytes);
			spritz.Squeeze(keyBytes);
			if(++iv2[0] == 0) {
				if(++iv2[1] == 0) {
					if(++iv2[2] == 0) {
						++iv2[3];
					}
				}
			} 
		}
		return keyBytes;
	}

}

} // End namespace
