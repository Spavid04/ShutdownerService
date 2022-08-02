using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Shutdowner
{
    public class MessageReceiver : IDisposable
    {
        private readonly int MaxDelayedSeconds;
        private readonly int MaxAheadSeconds;
        private readonly int HashLengthInBytes;

        private readonly string Passkey;
        private readonly SHA256 Hasher;

        public delegate void MessageAcceptedDelegate();
        public event MessageAcceptedDelegate MessageAccepted;

        public MessageReceiver(string passkey, int maxDelaySeconds, int maxAheadSeconds)
        {
            this.Passkey = passkey ?? "";
            this.MaxDelayedSeconds = Utils.Clamp(maxDelaySeconds, 0, 100);
            this.MaxAheadSeconds = Utils.Clamp(maxAheadSeconds, 0, 100);

            this.Hasher = SHA256.Create();
            this.HashLengthInBytes = this.Hasher.HashSize / 8;
        }

        private byte[] GenerateHashFromDt(DateTime dt)
        {
            string combined = this.Passkey + "-" + dt.ToString("yyyy-MM-dd HH:mm:ss");
            byte[] data = Encoding.ASCII.GetBytes(combined);
            return this.Hasher.ComputeHash(data);
        }

        private IEnumerable<byte[]> GenerateHashes()
        {
            DateTime now = DateTime.UtcNow;

            yield return this.GenerateHashFromDt(now);
            for (int i = 0; i < this.MaxDelayedSeconds; i++)
            {
                yield return this.GenerateHashFromDt(now - TimeSpan.FromSeconds(i));
            }
            for (int i = 0; i < this.MaxAheadSeconds; i++)
            {
                yield return this.GenerateHashFromDt(now + TimeSpan.FromSeconds(i));
            }
        }

        /// <summary>
        /// Expects a SHA256 hash hex string of "{passkey}-{UTC now in `yyyy-MM-dd HH:mm:ss` format}".
        /// Allows for a couple of seconds of delay or ahead-ness.
        /// </summary>
        public void ProcessPacket(UdpListener listener, byte[] data)
        {
            if (data == null || data.Length != HashLengthInBytes * 2) //hex string
            {
                return;
            }
            if (this.MessageAccepted == null)
            {
                return;
            }

            byte[] asBytes;
            try
            {
                asBytes = Utils.StringToByteArrayFastest(Encoding.ASCII.GetString(data));
            }
            catch (Exception)
            {
                return;
            }

            foreach (var hash in this.GenerateHashes())
            {
                if (Utils.UnsafeCompare(asBytes, hash))
                {
                    this.MessageAccepted.Invoke();
                    return;
                }
            }
        }

        public void Dispose()
        {
            this.Hasher?.Dispose();
        }
    }
}
