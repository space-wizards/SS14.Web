using System;
using System.Security.Cryptography;
using NUnit.Framework;
using SS14.Auth.Controllers;

namespace SS14.Auth.Tests
{
    [TestOf(typeof(SessionApiController))]
    public class SessionApiControllerTest
    {
        [Test]
        public void TestRoundTripEncrypt()
        {
            var key = RSA.Create(2048);

            var rand = new Random();
            var bytes = new byte[512];
            rand.NextBytes(bytes);

            var encrypted = SessionApiController.Encrypt(bytes, key);
            var decrypted = SessionApiController.Decrypt(encrypted, key);

            Assert.That(decrypted, Is.EqualTo(bytes));
        }
    }
}