using Microsoft.Phone.Info;
using Microsoft.Phone.Net.NetworkInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;

namespace LogEntries
{

    public static class DeviceMetricsUtils
    {
        #region K-V separators constants

        // K-V separators used in serializing.
        const string PAIRS_SEPARATOR = " ";
        const string KV_SEPARATOR = "=";
        const string SERIES_SEPARATOR = ";"; // E.g. KEY1=VALUE1 KEY2=VALUE2; KEY1=VALUE1 KEY2=VALUE2; ...

        #endregion

        #region NetworkStatus class - holds network interfaces info.

        public class NetworkStatus
        {
            #region Network Status constants, used for class serialization to K-V pairs string

            // Network status keys names.
            const string NETWORK_KEY_NAME = "Network";
            const string CELLULAR_KEY_NAME = "Cellular";
            const string ROAMING_KEY_NAME = "Roaming";
            const string WIFI_KEY_NAME = "Wifi";

            // Interface Info keys names.
            const string INTERFACE_KEY_NAME = "IntefaceName";
            const string INTERFACE_TYPE_KEY_NAME = "InterfaceType";
            const string INTERFACE_SUBTYPE_KEY_NAME = "InterfaceSubType";
            const string INTERFACE_BANDWIDTH_KEY_NAME = "InterfaceBandwidth";
            const string INTERFACE_STATE_KEY_NAME = "InterfaceState";

            #endregion

            public NetworkStatus()
            {
                NetworkInterfaces = new NetworkInterfaceList();
                isNetworkEnabled = DeviceNetworkInformation.IsNetworkAvailable;
                isCellularEnabled = DeviceNetworkInformation.IsCellularDataEnabled;
                isRoamingEnabled = DeviceNetworkInformation.IsCellularDataRoamingEnabled;
                isWiFiEnabled = DeviceNetworkInformation.IsWiFiEnabled;
            }
            public string SerializeConnectionStatuses()
            {
                StringBuilder serialized = new StringBuilder();
                serialized.Append(NETWORK_KEY_NAME).Append(KV_SEPARATOR).Append(isNetworkEnabled.ToString()).Append(PAIRS_SEPARATOR);
                serialized.Append(CELLULAR_KEY_NAME).Append(KV_SEPARATOR).Append(isCellularEnabled.ToString()).Append(PAIRS_SEPARATOR);
                serialized.Append(ROAMING_KEY_NAME).Append(KV_SEPARATOR).Append(isRoamingEnabled.ToString()).Append(PAIRS_SEPARATOR);
                serialized.Append(WIFI_KEY_NAME).Append(KV_SEPARATOR).Append(isWiFiEnabled.ToString());

                return serialized.ToString();
            }
            public string SerializeInterfaceInfos()
            {
                StringBuilder serialized = new StringBuilder();
                foreach(NetworkInterfaceInfo NetInterface in  NetworkInterfaces)
                {
                    serialized.Append(INTERFACE_KEY_NAME).Append(KV_SEPARATOR).Append(NetInterface.InterfaceName).Append(PAIRS_SEPARATOR);
                    serialized.Append(INTERFACE_TYPE_KEY_NAME).Append(KV_SEPARATOR).Append(NetInterface.InterfaceType.ToString()).Append(PAIRS_SEPARATOR);
                    serialized.Append(INTERFACE_SUBTYPE_KEY_NAME).Append(KV_SEPARATOR).Append(NetInterface.InterfaceSubtype.ToString()).Append(PAIRS_SEPARATOR);
                    serialized.Append(INTERFACE_BANDWIDTH_KEY_NAME).Append(KV_SEPARATOR).Append(NetInterface.Bandwidth.ToString()).Append(PAIRS_SEPARATOR);
                    serialized.Append(INTERFACE_STATE_KEY_NAME).Append(KV_SEPARATOR).Append(NetInterface.InterfaceState.ToString()).Append(SERIES_SEPARATOR);
                }

                return serialized.ToString();
            }

            public bool isNetworkEnabled { get; private set; }
            public bool isCellularEnabled { get; private set; }
            public bool isRoamingEnabled { get; private set; }
            public bool isWiFiEnabled { get; private set; }

            public readonly NetworkInterfaceList NetworkInterfaces;

        }

        #endregion

        public static string GetDeviceName()
        {
            return DeviceStatus.DeviceName;
        }

        public static string GetDeviceVendor()
        {
            return DeviceStatus.DeviceManufacturer;
        }

        public static string GetOSVersion()
        {
            return Environment.OSVersion.ToString();
        }

        public static long GetDeviceMemoryAmount()
        {
            return DeviceStatus.DeviceTotalMemory;
        }

        public static long GetAppPeakMemoryUsage()
        {
            return DeviceStatus.ApplicationPeakMemoryUsage;
        }

        public static long GetAppCurrentMemoryUsage()
        {
            return DeviceStatus.ApplicationCurrentMemoryUsage;
        }

        /// <summary>
        /// This method returns serialized Device Unique ID.
        /// NOTE: To use it the application must have ID_CAP_IDENTITY_DEVICE capability
        /// checked in WMAppManifest.xml
        ///
        /// If deafult parameter "ThrowOnError" is ommitted or ste to true -
        /// UnauthorizedAccessException will be thrown; empty Device ID will be returned otherwise.
        ///
        /// </summary>
        /// <returns>Device ID string - 20 bytes </returns>
        public static string GetDeviceUniqueID(bool ThrowOnError = true)
        {
            byte[] deviceIdRaw;
            try
            {
                deviceIdRaw = (byte[])DeviceExtendedProperties.GetValue("DeviceUniqueId");
            }
            catch (UnauthorizedAccessException ex)
            {
                // It seems that we do not have enough privileges to access the Device ID;
                // Probably, ID_CAP_IDENTITY_DEVICE property is not checked in the Manifest.
                if (ThrowOnError)
                {
                    throw new UnauthorizedAccessException("Cannot get value of DeviceUniqueId. Make sure ID_CAP_IDENTITY_DEVICE is set to true in the App's Manifest");
                }

                return "";
            }

            return BitConverter.ToString(deviceIdRaw).Replace("-", "");
        }

        /// <summary>
        /// The method gets statuses of current network interfaces.
        /// </summary>
        /// <returns>NetworkStatus object</returns>
        public static NetworkStatus GetNetworksStatuses()
        {
            return new NetworkStatus();
        }

    }
}
