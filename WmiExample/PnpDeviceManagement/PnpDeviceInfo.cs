namespace PnpDeviceManagement
{
    public class PnpDeviceInfo
    {
        public PnpDeviceInfo(string deviceId, string pnpDeviceId, string description)
        {
            DeviceId = deviceId;
            PnpDeviceId = pnpDeviceId;
            Description = description;
        }

        public string DeviceId { get; }
        public string PnpDeviceId { get; }
        public string Description { get; }
    }
}
