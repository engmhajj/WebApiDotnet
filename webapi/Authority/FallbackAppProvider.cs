using System;
using System.Collections.Generic;
using System.Linq;
using webapi.Models;
using webapi.Security;

namespace webapi.Authority
{
    public interface IFallbackAppProvider
    {
        Application? GetFallbackApp(string clientId);
    }

    public class FallbackAppProvider : IFallbackAppProvider
    {
        private readonly List<Application> _fallbackApps;

        public FallbackAppProvider()
        {
            const string demoSecret = "0673FC70-0514-4011-CCA3-DF9BC03201BC";
            var (salt, hash) = SecretHasher.HashSecret(demoSecret);

            _fallbackApps = new List<Application>
            {
                new Application
                {
                    ApplicationId = 1,
                    ApplicationName = "MVCWebApp",
                    ClientId = "53D3C1E6-5487-8C6E-A8E4BD59940E",
                    SecretSalt = salt,
                    SecretHash = hash,
                    Scopes = "read,write,delete",
                },
            };
        }

        public Application? GetFallbackApp(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return null;

            return _fallbackApps.FirstOrDefault(app =>
                app.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase)
            );
        }
    }
}
