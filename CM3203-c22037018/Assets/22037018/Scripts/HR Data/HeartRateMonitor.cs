using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HeartRateMonitor
{
    string device_name;
    string service_id;
    string read_characteristic;

    public bool want_connect = true;
    Dictionary<string, Dictionary<string, string>> devices = new Dictionary<string, Dictionary<string, string>>();
    string selectedDeviceId = "";
    string selectedServiceId = "";
    string selectedCharacteristicId = "";

    bool isSubscribed = false;

    public string output;
    public int heartRate; public bool has_heartRate = false;
    public bool contact_detected; public bool has_contact = false;
    public bool rr_interval_present; public bool has_rr_interval = false;
    public List<int> rr_intervals = new List<int>(); // RR intervals in ms

    string lastError;

    MonoBehaviour mono;
    HeartRateMonitor_UI ui;
    public HeartRateMonitor(MonoBehaviour _mono, HeartRateMonitor_UI _ui)
    {
        mono = _mono;
        ui = _ui;
    }

    // Start is called before the first frame update
    public IEnumerator connect(string _device_name = "Polar H10 D7E96D26", string _service_id = "{0000180d-0000-1000-8000-00805f9b34fb}", string _read_characteristic = "{00002A37-0000-1000-8000-00805f9b34fb}")
    {
        if (!want_connect) yield break;

        device_name = _device_name;
        service_id = _service_id;
        read_characteristic = _read_characteristic;

        quit();

        yield return mono.StartCoroutine(connect_device());
        if (selectedDeviceId.Length == 0) yield break;

        Debug.Log("connecting device finish");

        yield return mono.StartCoroutine(connect_service());
        if (selectedServiceId.Length == 0) yield break;

        Debug.Log("connecting service finish");

        yield return mono.StartCoroutine(connect_read_characteristic());
        if (selectedCharacteristicId.Length == 0) yield break;

        Debug.Log("connecting read characteristic finish");

        read_subscribe();
        ui.connected = true;
    }

    IEnumerator connect_device()
    {
        Debug.Log("connecting device...");
        BleApi.StartDeviceScan();
        BleApi.ScanStatus status = BleApi.ScanStatus.AVAILABLE;
        BleApi.DeviceUpdate device_res = new BleApi.DeviceUpdate();
        do
        {
            status = BleApi.PollDevice(ref device_res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (!devices.ContainsKey(device_res.id))
                    devices[device_res.id] = new Dictionary<string, string>() {
                            { "name", "" },
                            { "isConnectable", "False" }
                        };
                if (device_res.nameUpdated)
                    devices[device_res.id]["name"] = device_res.name;
                if (device_res.isConnectableUpdated)
                    devices[device_res.id]["isConnectable"] = device_res.isConnectable.ToString();
                // consider only devices which have a name and which are connectable
                if (devices[device_res.id]["name"] == device_name && devices[device_res.id]["isConnectable"] == "True")
                {
                    selectedDeviceId = device_res.id;
                    break;
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                if (selectedDeviceId.Length == 0)
                {
                    Debug.LogError("device " + device_name + " not found!");
                }
            }
            yield return 0;
        } while (status == BleApi.ScanStatus.AVAILABLE || status == BleApi.ScanStatus.PROCESSING);
    }

    IEnumerator connect_service()
    {
        Debug.Log("connecting service...");
        BleApi.ScanServices(selectedDeviceId);
        BleApi.ScanStatus status;
        BleApi.Service service_res = new BleApi.Service();
        do
        {
            status = BleApi.PollService(out service_res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (service_res.uuid == service_id)
                {
                    selectedServiceId = service_res.uuid;
                    break;
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                if (selectedServiceId.Length == 0)
                {
                    Debug.LogError("service " + service_id + " not found!");
                }
            }
            yield return 0;
        } while (status == BleApi.ScanStatus.AVAILABLE || status == BleApi.ScanStatus.PROCESSING);
    }

    IEnumerator connect_read_characteristic()
    {
        Debug.Log("connecting characteristic...");
        BleApi.ScanCharacteristics(selectedDeviceId, selectedServiceId);
        BleApi.ScanStatus status;
        BleApi.Characteristic characteristics_res = new BleApi.Characteristic();

        do
        {
            status = BleApi.PollCharacteristic(out characteristics_res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (characteristics_res.uuid == read_characteristic)
                {
                    selectedCharacteristicId = characteristics_res.uuid;
                    break;
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                if (selectedCharacteristicId.Length == 0)
                {
                    Debug.LogError("characteristic " + read_characteristic + " not found!");
                }
            }
            yield return 0;
        } while (status == BleApi.ScanStatus.AVAILABLE || status == BleApi.ScanStatus.PROCESSING);
    }

    void read_subscribe()
    {
        Debug.Log("Subscribe...");
        BleApi.SubscribeCharacteristic_Read(selectedDeviceId, selectedServiceId, selectedCharacteristicId, false);
        isSubscribed = true;
    }

    public void quit()
    {
        BleApi.Quit();
    }

    // Update is called once per frame
    public void Update()
    {
        if (isSubscribed)
        {
            BleApi.BLEData res = new BleApi.BLEData();
            while (BleApi.PollData(out res, false))
            {
                has_heartRate = false;
                has_contact = false;
                has_rr_interval = false;
                rr_intervals.Clear();

                output = String.Empty;
                int index = 0;

                // Read first byte for flags
                byte flags = res.buf[index++];

                // Heart Rate Value Format bit (0 = UINT8, 1 = UINT16)
                bool isUint16 = (flags & 0x01) != 0;

                // Sensor Contact Status bits
                bool contactSensorPresent = (flags & 0x04) != 0;
                if (contactSensorPresent)
                {
                    has_contact = true;
                    contact_detected = (flags & 0x02) != 0;
                    output += "Contact: " + (contact_detected ? "Detected" : "Not Detected") + "\n";
                }

                // Energy Expended Status bit
                bool energyExpendedPresent = (flags & 0x08) != 0;

                // RR-Interval bit
                rr_interval_present = (flags & 0x10) != 0;
                has_rr_interval = rr_interval_present;

                // Read the Heart Rate Measurement Value
                if (isUint16)
                {
                    heartRate = BitConverter.ToUInt16(res.buf, index);
                    index += 2;
                }
                else
                {
                    heartRate = res.buf[index++];
                }
                has_heartRate = true;
                output += "Heart Rate: " + heartRate + " bpm\n";

                // Skip Energy Expended field if present
                if (energyExpendedPresent)
                {
                    index += 2; // UINT16
                }

                // Read RR-Intervals if present
                if (rr_interval_present)
                {
                    // Calculate how many RR intervals are in the packet
                    // Each RR interval is 2 bytes (UINT16)
                    int remainingBytes = res.size - index;
                    int rrIntervalCount = remainingBytes / 2;

                    output += "RR Intervals: ";
                    for (int i = 0; i < rrIntervalCount; i++)
                    {
                        int rrInterval = BitConverter.ToUInt16(res.buf, index);
                        index += 2;
                        // RR intervals are in 1/1024 second units, convert to milliseconds
                        int rrIntervalMs = (int)Math.Round(rrInterval * 1000.0 / 1024.0);
                        rr_intervals.Add(rrIntervalMs);
                        output += rrIntervalMs + "ms ";
                    }
                    output += "\n";
                }
            }

            // log potential errors
            BleApi.ErrorMessage res_err = new BleApi.ErrorMessage();
            BleApi.GetError(out res_err);
            if (lastError != res_err.msg && !string.IsNullOrEmpty(res_err.msg))
            {
                Debug.LogError(res_err.msg);
                lastError = res_err.msg;
            }
        }
    }
}