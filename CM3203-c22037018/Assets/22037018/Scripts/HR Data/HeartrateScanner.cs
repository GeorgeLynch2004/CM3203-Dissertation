using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HeartrateScanner : MonoBehaviour
{
    // UUIDs for the Polar H10 heart rate monitor service and characteristics
    [SerializeField] private string H10_DEVICE_NAME = "Polar H10 D7E96D26";
    [SerializeField] private string HEART_RATE_SERVICE_UUID = "{0000180d-0000-1000-8000-00805f9b34fb}";  // Heart Rate Service UUID
    [SerializeField] private string HEART_RATE_MEASUREMENT_CHARACTERISTIC_UUID = "{00002A37-0000-1000-8000-00805f9b34fb}";  // Heart Rate Measurement Characteristic UUID

    [SerializeField] private string selectedDeviceId = "";
    [SerializeField] private string selectedServiceId = "";
    [SerializeField] private string selectedCharacteristicId = "";

    [SerializeField] public bool isConnected = false;
    [SerializeField] private bool isSubscribed = false;

    [SerializeField] private float heartRate = 0f;
    [SerializeField] private string heartRateOutput = "";

    [SerializeField] private DataManager dataManager;
    [SerializeField] private RawImage colourIndicator;

    

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
                if (serviceRes.uuid.ToLower() == HEART_RATE_SERVICE_UUID.ToLower())
                {
                    selectedServiceId = serviceRes.uuid;
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
        if (isSubscribed)
        {
            // make light in unity green
            colourIndicator.color = Color.green;

            BleApi.BLEData data = new BleApi.BLEData();
            while (BleApi.PollData(out data, false))
            {
                int index = 0;
                int flags = BitConverter.ToUInt16(data.buf, index);  // Read the flags (first 2 bytes)
                index += 2;

                int heartRateValue = 0;
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

                // provided a heartrate is being displayed
                if (heartRateValue > 0)
                {
                    heartRate = Mathf.Round(60000 / heartRateValue);
                }
                
                heartRateOutput = "Heart Rate: " + heartRate.ToString() + " bpm";
                dataManager.currentHeartRate = heartRate;
                dataManager.ProcessDataFromHR(heartRate.ToString());
            }
        }
        else
        {
            colourIndicator.color = Color.red;
        }
    }
}
