using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace PnpDeviceManagement
{
    public class PnpDeviceStatusChecker
    {
        public event EventHandler<PnpDeviceInfoEventArg> PnpDeviceStateChange;

        public CancellationTokenSource CancellationToken { get; }

        private uint _delayTime;

        private class QueryDeviceStatus
        {
            public string Query { get; set; }
            public string Description { get; set; }
            public DeviceStatus DeviceStatus { get; set; }
        }

        private readonly IList<QueryDeviceStatus> _queries;

        public PnpDeviceStatusChecker(IEnumerable<string> descriptions, uint delayTime)
        {
            CheckParameters(descriptions, delayTime);

            _queries = new List<QueryDeviceStatus>();
            CancellationToken = new CancellationTokenSource();

            _delayTime = delayTime;
            InitializeDeviceStatus(descriptions);

            CheckStatusChanges();
        }

        private void InitializeDeviceStatus(IEnumerable<string> descriptions)
        {
            var first = true;
            foreach (var description in descriptions)
            {
                _queries.Add(new QueryDeviceStatus
                {
                    Query = first ? $"SELECT DeviceID, PNPDeviceID, Description FROM Win32_PnPEntity WHERE description LIKE '%{description}%'"
                                 : $" OR description LIKE '%{description}%'",

                    Description = description,
                    DeviceStatus = DeviceStatus.Disconnected
                });
                first = false;
            }
        }

        private void CheckParameters(IEnumerable<string> descriptions, uint delayTime)
        {
            var descriptionError = string.Empty;
            string delayError = string.Empty;

            if (!descriptions.Any())
            {
                descriptionError = "Description collection must contain values";
            }

            if (delayTime < 1000)
            {
                delayError = $"The delay time should be greater than 999, you passed {delayTime}";
            }

            if (!string.IsNullOrEmpty(delayError) || !string.IsNullOrEmpty(descriptionError))
            {
                throw new Exception( $"{delayError}{Environment.NewLine}{descriptionError}");
            }
        }

        private void CheckStatusChanges()
        {
            var ct = CancellationToken.Token;

            var task = Task.Factory.StartNew(async() =>
            {
                ct.ThrowIfCancellationRequested();

                while (true)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(_delayTime));

                    var deviceStatus = GetDeviceStatus();
                    foreach (var device in _queries)
                    {
                        var found = deviceStatus.FirstOrDefault(d => d.Description.Contains(device.Description));
                        var notify = false;

                        if (found == null && device.DeviceStatus == DeviceStatus.Connected)
                        {
                            device.DeviceStatus = DeviceStatus.Disconnected;
                            notify = true;
                        }
                        else
                        if (found != null && device.DeviceStatus == DeviceStatus.Disconnected)
                        {
                            device.DeviceStatus = DeviceStatus.Connected;
                            notify = true;
                        }

                        if (notify)
                        {
                            PnpDeviceStateChange?.Invoke(this, new PnpDeviceInfoEventArg { Description = device.Description, DeviceStatus = device.DeviceStatus });
                        }
                    }

                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                }
            }, CancellationToken.Token); 
        }

        private IEnumerable<PnpDeviceInfo> GetDeviceStatus()
        {
            var queries = string.Join(string.Empty, _queries.Select(q => q.Query));

            using (var searcher = new ManagementObjectSearcher(queries))
            {
                var collection = searcher.Get();

                foreach (var device in collection)
                {
                    yield return new PnpDeviceInfo(
                        device.GetPropertyValue("DeviceID").ToString(),
                        device.GetPropertyValue("PNPDeviceID").ToString(),
                        device.GetPropertyValue("Description").ToString()
                    );
                }
            }
        }
    }
}
