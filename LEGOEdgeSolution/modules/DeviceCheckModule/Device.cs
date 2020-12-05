namespace DeviceCheckModule
{
    using System;
    using System.IO;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Extensions.Logging;
    using Unosquare.RaspberryIO;

    class IoTDevice
    {
        private ILogger _logger;
        public async static Task<IoTDevice> InitDevice(ModuleClient client, ILoggerFactory loggerFactory)
        {

            var logger = loggerFactory.CreateLogger("DeviceLogger");
            var device = new IoTDevice(client, logger);

            logger.LogInformation("IoT Hub module client initialized.");

            await client.SetMethodHandlerAsync("reset", device.ResetMethod, null);
            await client.SetMethodHandlerAsync("takephoto", device.TakePhoto, null);

            await device.SendDeviceData(client);
            return device;
        }
        private IoTDevice(ModuleClient client, ILogger logger)
        {
            _logger = logger;
        }

        private async Task SendDeviceData(ModuleClient deviceClient)
        {
            while (true)
            {
                try
                {
                    _logger.LogInformation("Sending data...");
                    await Task.Delay(TimeSpan.FromSeconds(20));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[ERROR] Unexpected Exception {ex.Message}");
                }
            }

        }
        private Task<MethodResponse> ResetMethod(MethodRequest request, object userContext)
        {
            var response = new MethodResponse((int)HttpStatusCode.OK);
            _logger.LogInformation("Received reset command via direct method invocation");
            return Task.FromResult(response);
        }

        private async Task<MethodResponse> TakePhoto(MethodRequest request, object userContext)
        {
            var response = new MethodResponse((int)HttpStatusCode.OK);
            var hasError = false;
            try
            {
                _logger.LogInformation("Received takephoto command via direct method invocation");
                var pictureBytes = await Pi.Camera.CaptureImageJpegAsync(640, 480);
                var targetPath = "/home/pi/images/picture.jpg";
                if (File.Exists(targetPath))
                    File.Delete(targetPath);

                File.WriteAllBytes(targetPath, pictureBytes);
                _logger.LogInformation($"Took picture -- Byte count: {pictureBytes.Length}");


                return response;
            }
            catch (System.Exception ex)
            {
                hasError = true;
                _logger.LogError(ex, $"[ERROR] Unexpected Exception {ex.Message}");
            }
            finally
            {
                if (hasError)
                    response = new MethodResponse((int)HttpStatusCode.InternalServerError);
                else
                    response = new MethodResponse((int)HttpStatusCode.OK);
            }
            return response;

        }

    }
}