using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using NinOS.Domain.Updates;
using NinOS.Infrastructure.Logging;
using Velopack.Sources;

namespace NinOS.UI.Updates
{
    public class PinnedFileDownloader : HttpClientFileDownloader
    {
        private const int LOGGED_PIN_PREFIX_LENGTH = 8;
        private const int DOWNLOAD_BUFFER_BYTES = 81920;

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

        public override async Task DownloadFile(string url, string targetFile, Action<int> progress, IDictionary<string, string>? headers, double timeout, CancellationToken cancelToken)
        {
            AppLog.Info($"Actualizaciones: descargando {Path.GetFileName(targetFile)}.");
            using HttpClient client = CreateHttpClient(headers, timeout);
            using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancelToken);
            response.EnsureSuccessStatusCode();

            long? total_bytes = response.Content.Headers.ContentLength;
            using Stream source = await response.Content.ReadAsStreamAsync(cancelToken);
            using FileStream target = File.Create(targetFile);
            byte[] buffer = new byte[DOWNLOAD_BUFFER_BYTES];
            long received_bytes = 0;
            int last_percent = -1;
            int read;
            while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancelToken)) > 0)
            {
                await target.WriteAsync(buffer, 0, read, cancelToken);
                received_bytes += read;
                if (total_bytes is > 0)
                {
                    int percent = (int)(received_bytes * 100 / total_bytes.Value);
                    if (percent != last_percent)
                    {
                        last_percent = percent;
                        progress(percent);
                    }
                }
            }
            AppLog.Info($"Actualizaciones: descarga terminada ({received_bytes} de {total_bytes?.ToString() ?? "?"} bytes).");
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
