using System;
using System.Security.Cryptography;
using System.Text;

namespace MedLinkPortal.Services
{
    /// <summary>
    /// Generates Agora RTC tokens using the AccessToken2 algorithm (pure C# — no NuGet required).
    /// Reference: https://docs.agora.io/en/voice-calling/reference/access-token
    /// </summary>
    public class AgoraTokenService
    {
        private const int TokenExpirationSeconds = 3600; // 1 hour
        private const int PrivilegeExpirationSeconds = 3600;

        // Privilege types
        private const int PrivilegeJoinChannel = 1;
        private const int PrivilegePublishAudioStream = 2;
        private const int PrivilegePublishVideoStream = 3;

        private readonly string _appId;
        private readonly string _appCertificate;

        public AgoraTokenService(IConfiguration configuration)
        {
            _appId = Environment.GetEnvironmentVariable("AGORA_APP_ID")
                  ?? configuration["Agora:AppId"]
                  ?? string.Empty;

            _appCertificate = Environment.GetEnvironmentVariable("AGORA_APP_CERT")
                           ?? configuration["Agora:AppCertificate"]
                           ?? string.Empty;
        }

        /// <summary>
        /// Generates an RTC token for a user joining a channel.
        /// channelName: e.g. "room_42" (appointmentId based)
        /// uid: numeric user ID (0 for any user)
        /// </summary>
        public string GenerateRtcToken(string channelName, uint uid = 0)
        {
            if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appCertificate))
                throw new InvalidOperationException("Agora App ID or Certificate not configured.");

            var expireTs = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + TokenExpirationSeconds);
            var salt = (uint)new Random().Next(1, 99999999);

            // Build message
            var message = new ByteBuffer();
            message.Put((ushort)1);    // version
            message.Put(salt);
            message.Put(expireTs);

            // Privileges
            var privileges = new ByteBuffer();
            privileges.Put((ushort)3); // count

            privileges.Put((ushort)PrivilegeJoinChannel);
            privileges.Put(expireTs);
            privileges.Put((ushort)PrivilegePublishAudioStream);
            privileges.Put(expireTs);
            privileges.Put((ushort)PrivilegePublishVideoStream);
            privileges.Put(expireTs);

            message.Put(privileges.ToArray());

            // Sign
            var toSign = Encoding.UTF8.GetBytes(_appId)
                .Concat(Encoding.UTF8.GetBytes(channelName))
                .Concat(BitConverter.GetBytes(uid))
                .Concat(message.ToArray())
                .ToArray();

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_appCertificate));
            var signature = hmac.ComputeHash(toSign);

            // Build token
            var token = new ByteBuffer();
            token.Put(signature);
            token.Put(message.ToArray());

            var tokenBytes = token.ToArray();
            var base64 = Convert.ToBase64String(tokenBytes);

            return $"007{base64}";
        }

        // ── Minimal ByteBuffer helper ─────────────────────────────────────────
        private class ByteBuffer
        {
            private readonly System.Collections.Generic.List<byte> _buf = new();

            public void Put(ushort v)
            {
                _buf.Add((byte)(v & 0xFF));
                _buf.Add((byte)((v >> 8) & 0xFF));
            }

            public void Put(uint v)
            {
                _buf.Add((byte)(v & 0xFF));
                _buf.Add((byte)((v >> 8) & 0xFF));
                _buf.Add((byte)((v >> 16) & 0xFF));
                _buf.Add((byte)((v >> 24) & 0xFF));
            }

            public void Put(byte[] data)
            {
                Put((ushort)data.Length);
                _buf.AddRange(data);
            }

            public byte[] ToArray() => _buf.ToArray();
        }
    }
}
