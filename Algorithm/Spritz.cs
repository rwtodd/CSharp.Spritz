using System;
using System.Collections.Generic;

namespace RWTodd.Spritz.Algorithm
{

public sealed class Cipher {
	private Byte i, j, k, z, a, w;
	private Byte[] mem;

	public Cipher() {
		mem = new Byte[256];
		Reset();
	}

	public void Reset() {
		i = 0;
		j = 0;
		k = 0;
		z = 0;
		a = 0;
		w = 1;
		for(var v = 0; v < 256; ++v) { mem[v] = (byte)v; }
	}

	private void MemSwap(int i, int j) {
		var tmp = mem[i];
		mem[i] = mem[j];
		mem[j] = tmp;
	}

	private void Crush() {
		for(var v = 0; v < 128; v++) {
			if(mem[v] > mem[255-v]) {
				MemSwap(v,255-v);
			}
		}
	}

	private void Update(int times) {
		// make local copies of state (seems to help optimizers...)
		Byte mi = i;
		Byte mj = j;
		Byte mk = k;
		Byte mw = w;

		while(times-- > 0) {
			mi += mw;
			mj = (byte)(mk + mem[ (mj + mem[mi]) & 0xFF ]);
			mk = (byte)(mi + mk + mem[mj]);
			MemSwap(mi,mj);
		}

		// store the final values back into our state
		i = mi;
		j = mj;
		k = mk;
	}

	private void Whip(int amt) {
		Update(amt);
		w += 2;
	}


	private void Shuffle() {
		Whip(512); Crush();
		Whip(512); Crush();
		Whip(512); 
		a = 0;
	}


	private void AbsorbNibble(int n) {
		if(a == 128) { Shuffle(); }
		MemSwap(a, 128+n);
		++a;
	}

	internal void Absorb(Byte b) {
		AbsorbNibble(b & 0x0f);
		AbsorbNibble(b >> 4);
	}

	internal void AbsorbStop() {
		if(a == 128) { Shuffle(); }
		++a;
	}

	internal void AbsorbNumber(int n) {
		if(n > 255) { AbsorbNumber(n >> 8); }
		Absorb((byte)n);
	}

	public void Soak(Byte[] src, Int32 len) {
		for(int n = 0; n < len; ++n) {
			Absorb(src[n]);
		}
	}

	public void Soak(Byte[] src) => Soak(src, src.Length);

	public void Soak(IEnumerable<Byte> src) {
		foreach(Byte b in src) {
			Absorb(b);
		}
	}

	private Byte Drip() {
		// NOTE! if a isn't zero the caller must call Shuffle first!
		Update(1);
		z = mem[ (j + mem[ (i + mem[ (z + k) & 0xFF ]) & 0xFF ]) & 0xFF ];
		return z;
	}

	internal void Skip(Int32 amt) {
		if(a > 0) { Shuffle(); }
		for(var v = 0; v < amt; ++v) {
			Drip();
		}
	}

	public void Squeeze(Byte[] tgt) {
		if(a > 0) { Shuffle(); }
		for(var v = 0; v < tgt.Length; ++v) {
			tgt[v] = Drip();
		}
	}

	public void SqueezeXOR(Byte[] tgt, Int32 offs, Int32 len) {
		if(a > 0) { Shuffle(); }
		for(var v = offs; v < (offs+len); ++v) {
			tgt[v] ^= Drip();
		}
	}

	public void SqueezeXOR(Byte[] tgt, Int32 len) {
		if(a > 0) { Shuffle(); }
		for(var v = 0; v < len; ++v) {
			tgt[v] ^= Drip();
		}
	}

	public void SqueezeXOR(Byte[] tgt) => SqueezeXOR(tgt, tgt.Length);

}

} // end namespace
