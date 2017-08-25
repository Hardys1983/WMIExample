using System;

namespace PnpDeviceManagement
{
    public class PnpDeviceStatusException : Exception
    {
        public PnpDeviceStatusException(string message) 
            : base(message)
        {
        }
    }
}
