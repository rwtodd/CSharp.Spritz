using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using RWTodd.Spritz.Algorithm;

namespace RWTodd.Spritz.Tests
{

[TestClass]
public class HashTests {

	[TestMethod]
	public void TestArcFour() {
		var result = new Byte[32];
		Hash.OfUTF8String(result, "arcfour");
		Assert.AreEqual("/4zyaAlMh7lfdM5v7p0wA6X5/mlEZTzVDma/GJxj9pk=",
			Convert.ToBase64String(result)); 
	}

	[TestMethod]
	public void TestABC123() {
		var result = new Byte[32];
		Hash.OfUTF8String(result, "ABC123\r\n");
		Assert.AreEqual("ISaOr4XRgrfDX1onBfKggeNnisNTE4kkKB8oI9BNFJ0=", 
			Convert.ToBase64String(result));
	}

	[TestMethod]
	public void TestHash1024() {
		var result = new Byte[1024/8];
		Hash.OfUTF8String(result, "test of arc");
		Assert.AreEqual(
			"mODbEBMQN0e4fNQkMpQAFXRnJb+m4qJ4Jj/ZD85JEnqkgx0guarutyDDNUC6kDvDCSnIIxW0md2v8fng9jwOgNZxmp46NaJxjoR1jNfDIa8zf6nWNUdypzFTYQwL34Ci/SWcRq78Kzvod+oGZSTvpMuznWPo2nzVY32LPY/CI4E=",
			Convert.ToBase64String(result));
	}

	[TestMethod]
	public void TestHash4096() {
		var result = new Byte[4096/8];
		Hash.OfUTF8String(result, "large-hash");
		Assert.IsTrue(Convert.ToBase64String(result)
                             .StartsWith("/51nef3y/1tdjXK2VFYoV/gSghU2nsOHLRoGnE")); 
	}

}

} // end namespace

