using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using NinOS.Domain.Updates;
using NinOS.Infrastructure.Logging;
using Velopack.Sources;

namespace NinOS.UI.Updates
{
    public class PinnedFileDownloader : HttpClientFileDownloader
    {
        private const int LOGGED_PIN_PREFIX_LENGTH = 8;

        private readonly IReadOnlyList<string> _allowed_pins;

        public PinnedFileDownloader() : this(ServerPin.default_pins)
        {
        }

        public PinnedFileDownloader(IReadOnlyList<string> allowed_pins)
        {
            if (allowed_pins == null || allowed_pins.Count == 0)
                throw new ArgumentException("Debe haber al menos una huella permitida.", nameof(allowed_pins));

            ServerPin.is_allowed(allowed_pins[0], allowed_pins);
            _allowed_pins = allowed_pins;
        }

        protected override HttpClientHandler CreateHttpClientHandler()
        {
            HttpClientHandler handler = base.CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = validate_server_certificate;
            return handler;
        }

        private bool validate_server_certificate(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors)
        {
            if (certificate == null)
            {
                AppLog.Warn("Actualizaciones: el servidor no presento certificado; conexion rechazada.");
                return false;
            }

            string received_pin = ServerPin.compute_pin(certificate);
            bool accepted = ServerPin.is_allowed(received_pin, _allowed_pins);

            if (accepted)
                AppLog.Info("Actualizaciones: servidor aceptado, su huella coincide con una de las fijadas.");
            else
                AppLog.Warn($"Actualizaciones: servidor RECHAZADO, su huella no coincide con ninguna de las fijadas (empieza con {received_pin.Substring(0, LOGGED_PIN_PREFIX_LENGTH)}).");

            return accepted;
        }
    }
}
