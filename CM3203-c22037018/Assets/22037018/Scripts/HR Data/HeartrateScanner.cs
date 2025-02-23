using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartrateScanner : MonoBehaviour
{
    // UUIDs for the Polar H10 heart rate monitor service and characteristics
    [SerializeField] private string H10_DEVICE_NAME = "Polar H10 D7E96D26";
    [SerializeField] private string HEART_RATE_SERVICE_UUID = "{0000180d-0000-1000-8000-00805f9b34fb}";  // Heart Rate Service UUID
    [SerializeField] private string HEART_RATE_MEASUREMENT_CHARACTERISTIC_UUID = "{00002A37-0000-1000-8000-00805f9b34fb}";  // Heart Rate Measurement Characteristic UUID

    [SerializeField] private string selectedDeviceId = "";
    [SerializeField] private string selectedServiceId = "";
    [SerializeField] private string selectedCharacteristicId = "";

    [SerializeField] private bool isConnected = false;
    [SerializeField] private bool isSubscribed = false;

    [SerializeField] private float heartRate = 0f;
    [SerializeField] private string heartRateOutput = "";

    [SerializeField] private DataManager dataManager;

    private void Start()
    {
        // Start scanning for devices and connect to the Polar H10 when found
        StartCoroutine(ConnectToH10());
    }

    public void connect()
    {
        StartCoroutine(ConnectToH10());
    }

    private IEnumerator ConnectToH10()
    {
        BleApi.StartDeviceScan();
        BleApi.ScanStatus status = BleApi.ScanStatus.AVAILABLE;
        BleApi.DeviceUpdate deviceRes = new BleApi.DeviceUpdate();

        // Scan for devices
        while (status != BleApi.ScanStatus.FINISHED)
        {
            status = BleApi.PollDevice(ref deviceRes, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (deviceRes.name == H10_DEVICE_NAME)
                {
                    selectedDeviceId = deviceRes.id;
                    Debug.Log("Found Polar H10: " + selectedDeviceId);
                    break;
                }
            }
            yield return null;
        }

        if (selectedDeviceId.Length == 0)
        {
            Debug.LogError("Polar H10 device not found!");
            yield break;
        }

        // Connect to the selected device
        yield return ConnectToService();
    }

    private IEnumerator ConnectToService()
    {
        // add small delay before scanning to ensure device is ready
        yield return new WaitForSeconds(1f);

        // Scan for the heart rate service
        BleApi.ScanServices(selectedDeviceId);
        BleApi.ScanStatus status = BleApi.ScanStatus.AVAILABLE;
        BleApi.Service serviceRes = new BleApi.Service();

        while (status != BleApi.ScanStatus.FINISHED)
        {
            status = BleApi.PollService(out serviceRes, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                Debug.Log("Found Service UUID: " + serviceRes.uuid);
                if (serviceRes.uuid.ToLower() == HEART_RATE_SERVICE_UUID.ToLower())
                {
                    selectedServiceId = serviceRes.uuid;
                    Debug.Log("Found Heart Rate Service: " + selectedServiceId);
                    break;
                }
            }
            yield return null;
        }

        if (selectedServiceId.Length == 0)
        {
            Debug.LogError("Heart Rate Service not found!");
            yield break;
        }

        // Scan for the heart rate measurement characteristic
        BleApi.ScanCharacteristics(selectedDeviceId, selectedServiceId);
        BleApi.Characteristic characteristicRes = new BleApi.Characteristic();
        status = BleApi.ScanStatus.AVAILABLE;

        while (status != BleApi.ScanStatus.FINISHED)
        {
            status = BleApi.PollCharacteristic(out characteristicRes, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (characteristicRes.uuid.ToLower() == HEART_RATE_MEASUREMENT_CHARACTERISTIC_UUID.ToLower())
                {
                    selectedCharacteristicId = characteristicRes.uuid;
                    Debug.Log("Found Heart Rate Measurement Characteristic: " + selectedCharacteristicId);
                    break;
                }
            }
            yield return null;
        }

        if (selectedCharacteristicId.Length == 0)
        {
            Debug.LogError("Heart Rate Measurement Characteristic not found!");
            yield break;
        }

        // Subscribe to the heart rate characteristic to start receiving data
        BleApi.SubscribeCharacteristic_Read(selectedDeviceId, selectedServiceId, selectedCharacteristicId, false);
        isSubscribed = true;

        Debug.Log("Subscribed to Heart Rate Characteristic.");
    }

    private void Update()
    {
        // Poll for data from the heart rate characteristic
        if (isSubscribed)
        {
            BleApi.BLEData data = new BleApi.BLEData();
            while (BleApi.PollData(out data, false))
            {
                int index = 0;
                int flags = BitConverter.ToUInt16(data.buf, index);  // Read the flags (first 2 bytes)
                index += 2;

                //Debug.Log($"Flags: {flags:X4}"); // Log flags to check what they are
                //Debug.Log($"Raw Data: {BitConverter.ToString(data.buf)}"); // Log raw data for inspection

                int heartRateValue = 0;

                // Check if heart rate value is 1 byte or 2 bytes based on flags
                if ((flags & 0x01) != 0) // 1-byte heart rate value
                {
                    heartRateValue = data.buf[index];
                    index += 1;
                }
                else // 2-byte heart rate value
                {
                    heartRateValue = BitConverter.ToUInt16(data.buf, index);
                    index += 2;
                }

                // Output the heart rate value to the debug log 
                // By default the data is transmitted as rr intervals so apply formula to get bpm
                heartRate = Mathf.Round(60000/heartRateValue);
                
                heartRateOutput = "Heart Rate: " + heartRate.ToString() + " bpm";
                dataManager.currentHeartRate = heartRate;
                dataManager.ProcessDataFromHR(heartRate.ToString());
            }

            // Check for any errors
            BleApi.ErrorMessage errorMsg = new BleApi.ErrorMessage();
            BleApi.GetError(out errorMsg);
            if (!string.IsNullOrEmpty(errorMsg.msg))
            {
                //Debug.LogError("Error: " + errorMsg.msg);
            }
        }
    }
}
